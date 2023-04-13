using System;
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

            if (Data.Count == 5)
            {
                Channel = Data[0];
                var valueData = Data.GetRange(1, 4).ToArray().Reverse().ToArray();
                Value = BitConverter.ToUInt32(valueData, 0);
            }
            else
            {
                Error = ResponseError.InvalidResponseFormat;
            }
        }
    }
}
