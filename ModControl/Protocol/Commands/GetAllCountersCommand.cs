using Cynox.ModControl.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Used to request the counter values for all channels.
    /// </summary>
	public class GetAllCountersCommand : IModControlCommand<GetAllCountersResponse>
    {
        /// <inheritdoc />
        public ModControlCommandCode CommandCode => ModControlCommandCode.GetAllCounters;

        /// <inheritdoc />
        public List<byte> GetData()
        {
            return new List<byte>();
        }

        /// <inheritdoc />
        public GetAllCountersResponse ParseResponse(ModControlFrame frame)
        {
            return new GetAllCountersResponse(frame);
        }
    }
}
