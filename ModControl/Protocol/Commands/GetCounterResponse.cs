using System;
using System.Collections.Generic;
using System.Linq;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Expected response for <see cref="GetCounterCommand"/>.
    /// </summary>
    public class GetCounterResponse : ModControlResponse
    {
        /// <summary>
        /// Channel number.
        /// </summary>
        public byte Channel { get; }
        
        /// <summary>
        /// Current counter value.
        /// </summary>
        public UInt32 Value { get; }

        /// <inheritdoc />
        public GetCounterResponse(byte channel, uint value)
        {
            Channel = channel;
            Value = value;
        }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public GetCounterResponse(ModControlFrame frame) : base(frame)
        {
            if (Error != ResponseError.None)
            {
                return;
            }

            if (frame.Data.Count == 5)
            {
                Channel = frame.Data[0];
                var valueData = frame.Data.GetRange(1, 4).ToArray().Reverse().ToArray();
                Value = BitConverter.ToUInt32(valueData, 0);
            }
            else
            {
                Error = ResponseError.InvalidResponseFormat;
            }
        }

        /// <inheritdoc/>
        public override IList<byte> GetData()
        {
            var data = new List<byte> { Channel };
            data.AddRange(BitConverter.GetBytes(Value).Reverse());
            return data;
        }
    }
}
