﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Expected response for <see cref="GetAllCountersCommand"/>.
    /// </summary>
    public class GetAllCountersResponse : ModControlResponse
    {
        /// <summary>
        /// A list of counter values for each available channel.
        /// </summary>
        public List<UInt32> Values { get; }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public GetAllCountersResponse(ModControlFrame frame) : base(frame)
        {
            Values = new List<UInt32>();

            if (Error != ResponseError.None)
            {
                return;
            }

            if (Data.Count % 4 == 0)
            {
                var counterData = new List<byte>(Data);

                while (counterData.Any())
                {
                    var valueData = counterData.GetRange(0, 4).ToArray().Reverse().ToArray();
                    counterData.RemoveRange(0, 4);
                    Values.Add(BitConverter.ToUInt32(valueData, 0));
                }
            }
            else
            {
                Error = ResponseError.InvalidResponseFormat;
            }
        }
    }
}
