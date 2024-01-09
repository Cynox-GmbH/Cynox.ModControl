using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Response to <see cref="GetProtocolVersionCommand"/>.
    /// </summary>
    public class GetProtocolVersionResponse : ModControlResponse
    {
        /// <summary>
        /// Protocol version number.
        /// </summary>
        public byte Version { get; }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public GetProtocolVersionResponse(ModControlFrame frame) : base(frame)
        {
            if (Error != ResponseError.None)
            {
                return;
            }

            if (frame.Data.Count == 1)
            {
                Version = frame.Data[0];
            }
            else
            {
                Error = ResponseError.InvalidResponseFormat;
            }
        }

        /// <inheritdoc/>
        public override IList<byte> GetData()
        {
            throw new System.NotImplementedException();
        }
    }
}
