using System;
using System.Linq;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Expected response for <see cref="GetCardIdCommand"/>.
    /// </summary>
    public class GetCardIdResponse : ModControlResponse
    {
        /// <summary>
        /// The channel number.
        /// </summary>
        public byte Channel { get; }
        
        /// <summary>
        /// The UID of the assigned card or 0 if no card is assigned.
        /// </summary>
        public ulong CardId { get; }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public GetCardIdResponse(ModControlFrame frame) : base(frame) {
            if (Error != ResponseError.None) {
                return;
            }

            if (Data.Count == 9) {
                Channel = Data[0];
                var valueData = Data.GetRange(1, 8).ToArray().Reverse().ToArray();
                CardId = BitConverter.ToUInt64(valueData, 0);
            } else {
                Error = ResponseError.InvalidResponseFormat;
            }
        }
    }
}
