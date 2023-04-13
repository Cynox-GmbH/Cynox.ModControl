using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Used to request the output state for all channels.
    /// </summary>
	public class GetAllOutputsCommand : IModControlCommand<GetAllOutputsResponse>
    {
        /// <inheritdoc />
        public ModControlCommandCode CommandCode => ModControlCommandCode.GetAllOutputs;

        /// <inheritdoc />
        public List<byte> GetData()
        {
            return new List<byte>();
        }

        /// <inheritdoc />
        public GetAllOutputsResponse ParseResponse(ModControlFrame frame)
        {
            return new GetAllOutputsResponse(frame);
        }
    }
}
