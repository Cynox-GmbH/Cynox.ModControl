using System.Collections.Generic;
using System.Text;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Response to <see cref="GetSerialCommand"/>.
    /// </summary>
    public class GetSerialResponse : ModControlResponse
    {
        /// <summary>
        /// Serial number of the device.
        /// </summary>
        public string Serial { get; set; }

        /// <inheritdoc />
        public GetSerialResponse(string serial)
        {
            Serial = serial;
        }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public GetSerialResponse(ModControlFrame frame) : base(frame)
        {
            if (Error != ResponseError.None)
            {
                return;
            }

            if (frame.Data.Count != 14)
            {
                Error = ResponseError.InvalidParameterFormat;
            }

            Serial = Encoding.ASCII.GetString(frame.Data.ToArray());
        }

        /// <inheritdoc />
        public override IList<byte> GetData()
        {
            return Encoding.ASCII.GetBytes(Serial);
        }
    }
}