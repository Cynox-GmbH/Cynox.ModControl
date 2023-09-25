using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Used to request the supported protocol from the device.
    /// </summary>
	public class GetProtocolVersionCommand : IModControlCommand<GetProtocolVersionResponse>
    {
        /// <inheritdoc />
        public ModControlCommandCode CommandCode => ModControlCommandCode.GetVersion;

        /// <inheritdoc />
        public List<byte> GetData()
        {
            return new List<byte>();
        }

        /// <inheritdoc />
        public GetProtocolVersionResponse ParseResponse(ModControlFrame frame)
        {
            return new GetProtocolVersionResponse(frame);
        }
    }
}
