using Cynox.ModControl.Protocol.Commands;
using Microsoft.Extensions.Logging;

namespace Cynox.ModControl.Devices
{
    /// <summary>
    /// Generic Mod-Control device that implements convenience methods for most requests.
    /// </summary>
    public class ModControlDevice : ModControlBase
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public ModControlDevice(ILogger<ModControlBase> logger = null) : base(logger) { }

        /// <summary>
        /// Requests the current counter values for all available channels.
        /// </summary>
        /// <returns></returns>
        public GetAllCountersResponse GetAllCounters()
        {
            var command = new GetAllCountersCommand();
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }

        /// <summary>
        /// Requests the counter value for a specific channel.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <returns></returns>
        public GetCounterResponse GetCounter(byte channel)
        {
            var command = new GetCounterCommand(channel);
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }

        /// <summary>
        /// Sets the counter value for a specific channel.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <param name="value">The new counter value.</param>
        /// <returns></returns>
        public GetCounterResponse SetCounter(byte channel, uint value)
        {
            var command = new SetCounterCommand(channel, value);
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }

        /// <summary>
        /// Requests the output state for all channels.
        /// </summary>
        /// <returns></returns>
        public GetAllOutputsResponse GetAllOutputs()
        {
            var command = new GetAllOutputsCommand();
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }

        /// <summary>
        /// Sets the output state for a specific channel and optionally specifies the load limit.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <param name="state">The desired state.</param>
        /// <param name="limit">The desired load limit. If this limit is exceeded for a prolonged duration, the channel automatically switches off.</param>
        /// <returns></returns>
        public SetOutputResponse SetOutput(byte channel, bool state, LoadLimit limit = LoadLimit.DoNotChange)
        {
            var command = new SetOutputCommand(channel, state, limit);
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }

        /// <summary>
        /// Requests the UID of the card that is currently assigned to the specified channel.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <returns></returns>
        public GetCardIdResponse GetCardId(byte channel)
        {
            var command = new GetCardIdCommand(channel);
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }

        /// <summary>
        /// Requests the current amount of credits for the specified channel.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <returns></returns>
        public GetSetCreditsResponse GetCredits(byte channel)
        {
            var command = new GetSetCreditsCommand(channel);
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }

        /// <summary>
        /// Changes the credit value for the specified channel.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <param name="action">Specifies how the credit value should be handled.</param>
        /// <param name="credits">The credit value.</param>
        /// <returns></returns>
        public GetSetCreditsResponse SetCredits(byte channel, GetSetCreditsCommand.CreditAction action, ushort credits)
        {
            var command = new GetSetCreditsCommand(channel, action, credits);
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }

        /// <summary>
        /// Sets the credit value for the specified channel.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <param name="credits">The credit new value.</param>
        /// <returns></returns>
        public GetSetCreditsResponse SetCredits(byte channel, ushort credits)
        {
            return SetCredits(channel, GetSetCreditsCommand.CreditAction.Set, credits);
        }

        /// <summary>
        /// Adds the credit value to the specified channel.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <param name="credits">The credit value to add.</param>
        /// <returns></returns>
        public GetSetCreditsResponse AddCredits(byte channel, ushort credits)
        {
            return SetCredits(channel, GetSetCreditsCommand.CreditAction.Add, credits);
        }

        /// <summary>
        /// Removes the credit value from the specified channel.
        /// </summary>
        /// <param name="channel">The desired channel number (0...n).</param>
        /// <param name="credits">The credit value to remove.</param>
        /// <returns></returns>
        public GetSetCreditsResponse SubtractCredits(byte channel, ushort credits)
        {
            return SetCredits(channel, GetSetCreditsCommand.CreditAction.Subtract, credits);
        }
    }
}
