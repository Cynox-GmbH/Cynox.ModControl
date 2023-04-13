using System;
using System.Collections.Generic;
using System.Linq;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Used to manipulate the credit value for a specific channel.
    /// </summary>
	public class GetSetCreditsCommand : IModControlCommand<GetSetCreditsResponse>
    {
        /// <summary>
        /// Used to specify how the credit value should be handled.
        /// </summary>
        public enum CreditAction {
            /// <summary>
            /// Return current credit.
            /// </summary>
            Get = 0,
            /// <summary>
            /// Set credit to specified value.
            /// </summary>
            Set = 1,
            /// <summary>
            /// Increase current credit by specified value.
            /// </summary>
            Add = 2,
            /// <summary>
            /// Decrease current credit by specified value.
            /// </summary>
            Subtract = 3
        }

        /// <summary>
        /// The affected 
        /// </summary>
        private byte Channel { get; }
        private CreditAction Action { get; }
        private UInt16 Value { get; }

        /// <inheritdoc />
        public ModControlCommandCode CommandCode => ModControlCommandCode.GetSetCredits;

        /// <summary>
        /// Creates a new instance to set the credit value for a specific channel.
        /// </summary>
        /// <param name="channel">Target channel.</param>
        public GetSetCreditsCommand(byte channel) {
            Channel = channel;
            Action = CreditAction.Get;
            Value = 0;
        }

        /// <summary>
        /// Creates a new instance to perform a credit-action for a specific channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="action"></param>
        /// <param name="value"></param>
        public GetSetCreditsCommand(byte channel, CreditAction action, ushort value) {
            Channel = channel;
            Action = action;
            Value = value;
        }

        /// <inheritdoc />
        public List<byte> GetData() {
            var data = new List<byte>();
            data.Add(Channel);
            data.Add((byte)Action);
            data.AddRange(BitConverter.GetBytes(Value).Reverse());
            return data;
        }

        /// <inheritdoc />
        public GetSetCreditsResponse ParseResponse(ModControlFrame frame) {
            return new GetSetCreditsResponse(frame);
        }
    }
}
