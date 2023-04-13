using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Used to request the card-id that is currently assigned to a specific channel.
    /// </summary>
	public class GetCardIdCommand : IModControlCommand<GetCardIdResponse> {
        /// <summary>
        /// The channel number.
        /// </summary>
        public byte Channel { get; }

        /// <inheritdoc />
        public ModControlCommandCode CommandCode => ModControlCommandCode.GetCardId;

        /// <summary>
        /// Creates a new instance to request the card-id for a specific channel.
        /// </summary>
        /// <param name="channel">Target channel.</param>
        public GetCardIdCommand(byte channel) {
            Channel = channel;
        }

        /// <inheritdoc />
        public List<byte> GetData() {
            return new List<byte> { Channel };
        }

        /// <inheritdoc />
        public GetCardIdResponse ParseResponse(ModControlFrame frame) {
            return new GetCardIdResponse(frame);
        }
    }
    }