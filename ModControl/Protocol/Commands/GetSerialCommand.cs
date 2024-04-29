using System;
using System.Collections.Generic;
using System.Text;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Requests the serial number of the device.
    /// </summary>
    public class GetSerialCommand : IModControlCommand<GetSerialResponse>
    {
        /// <inheritdoc />
        public ModControlCommandCode CommandCode { get; } = ModControlCommandCode.GetSerial;

        /// <inheritdoc />
        public List<byte> GetData()
        {
            return new List<byte>();
        }

        /// <inheritdoc />
        public GetSerialResponse ParseResponse(ModControlFrame frame)
        {
            return new GetSerialResponse(frame);
        }
    }
}
