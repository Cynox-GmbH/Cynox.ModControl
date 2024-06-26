﻿using System;
using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Expected response for <see cref="GetAllOutputsCommand"/>.
    /// </summary>
	public class GetAllOutputsResponse : ModControlResponse
    {
        /// <summary>
        /// Possible states of an output.
        /// </summary>
        public enum OutPutState {
            /// <summary>
            /// The output is currently switched off.
            /// </summary>
            Off,
            /// <summary>
            /// The output is currently switched on.
            /// </summary>
            On,
            /// <summary>
            /// The output is currently switched off, because it exceeded the maximum allowed power consumption.
            /// </summary>
            Overload,
        }

        /// <summary>
        /// List of all output states.
        /// </summary>
        public List<OutPutState> States { get; }

        /// <summary>
        /// Created a new instance with defined output states.
        /// </summary>
        /// <param name="states"></param>
        public GetAllOutputsResponse(List<OutPutState> states)
        {
            States = states;
        }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public GetAllOutputsResponse(ModControlFrame frame) : base(frame)
        {
            States = new List<OutPutState>();

            if (Error != ResponseError.None)
            {
                return;
            }

            if (frame.Data.Count == 1)
            {
                // Altes Format → 1 Byte für 8 Kanäle 
                for (var i = 0; i < 8; i++)
                {
                    States.Add((frame.Data[0] & (1 << i)) > 0 ? OutPutState.On : OutPutState.Off);
                }
            } 
            else if (frame.Data.Count > 1)
            {
                // Neues Format → für jeden Kanal ein Byte
                foreach (byte b in frame.Data) {
                    var state = OutPutState.Off;
                    
                    switch (b) {
                        case 0x00:
                            state = OutPutState.Off;
                            break;
                        case 0x01:
                            state = OutPutState.On;
                            break;
                        case 0x02:
                            state = OutPutState.Overload;
                            break;
                        default:
                            Error = ResponseError.InvalidResponseFormat;
                            break;
                    }

                    States.Add(state);
                }
            }
            else
            {
                Error = ResponseError.InvalidResponseFormat;
            }
        }

        /// <inheritdoc/>
        public override IList<byte> GetData()
        {
            var data = new List<byte>();
            foreach (var state in States)
            {
                byte stateValue;
                
                switch (state)
                {
                    case OutPutState.Off:
                        stateValue = 0;
                        break;
                    case OutPutState.On:
                        stateValue = 1;
                        break;
                    case OutPutState.Overload:
                        stateValue = 2;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                data.Add(stateValue);
            }

            return data;
        }
    }
}
