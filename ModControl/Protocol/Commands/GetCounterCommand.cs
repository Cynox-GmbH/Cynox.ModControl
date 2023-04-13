using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// 
    /// </summary>
	public class GetCounterCommand : IModControlCommand<GetCounterResponse>
    {
        /// <summary>
        /// The channel number.
        /// </summary>
        public byte Channel { get; }

        /// <inheritdoc />
        public ModControlCommandCode CommandCode => ModControlCommandCode.GetCounter;

        /// <summary>
        /// Creates a new instance to request the counter value for a specific channel.
        /// </summary>
        /// <param name="channel">Target channel.</param>
        public GetCounterCommand(byte channel)
        {
            Channel = channel;
        }

        /// <inheritdoc />
        public List<byte> GetData()
        {
            return new List<byte> {Channel};
        }

        /// <inheritdoc />
        public GetCounterResponse ParseResponse(ModControlFrame frame)
        {
            return new GetCounterResponse(frame);
        }
    }
}
