using System;
using System.Collections.Generic;
using System.Linq;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Used to set the counter value of a specific channel.
    /// </summary>
	public class SetCounterCommand : IModControlCommand<GetCounterResponse>
    {
        /// <summary>
        /// Channel number.
        /// </summary>
        public byte Channel { get; }
        
        /// <summary>
        /// Desired counter value.
        /// </summary>
        private UInt32 Value { get; }

        /// <inheritdoc />
        public ModControlCommandCode CommandCode => ModControlCommandCode.SetCounter;

        /// <summary>
        /// Creates a new instance to set the counter value for the specified channel to the desired value.
        /// </summary>
        /// <param name="channel">Target channel.</param>
        /// <param name="value">Desired counter value.</param>
        public SetCounterCommand(byte channel, UInt32 value)
        {
            Channel = channel;
            Value = value;
        }

        /// <inheritdoc />
        public List<byte> GetData()
        {
            var data = new List<byte>();
            data.Add(Channel);
            data.AddRange(BitConverter.GetBytes(Value).Reverse());
            return data;
        }

        /// <inheritdoc />
        public GetCounterResponse ParseResponse(ModControlFrame frame)
        {
            return new GetCounterResponse(frame);
        }
    }
}
