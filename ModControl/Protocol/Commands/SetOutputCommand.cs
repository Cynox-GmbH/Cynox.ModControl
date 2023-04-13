using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Possible values to set the load limit behaviour of a channel.
    /// </summary>
	public enum LoadLimit : byte
    {
        /// <summary>
        /// Do not change the currently configured load limit.
        /// </summary>
        DoNotChange = 0,
        /// <summary>
        /// Set load limit to 4 amperes.
        /// </summary>
        LimitTo4Ampere = 4,
        /// <summary>
        /// Set load limit to 5 amperes.
        /// </summary>
        LimitTo5Ampere = 5,
        /// <summary>
        /// Set load limit to 6 amperes.
        /// </summary>
        LimitTo6Ampere = 6,
        /// <summary>
        /// Set load limit to 7 amperes.
        /// </summary>
        LimitTo7Ampere = 7,
        /// <summary>
        /// Set load limit to 8 amperes.
        /// </summary>
        LimitTo8Ampere = 8,
        /// <summary>
        /// Set load limit to 9 amperes.
        /// </summary>
        LimitTo9Ampere = 9,
        /// <summary>
        /// Set load limit to 10 amperes.
        /// </summary>
        LimitTo10Ampere = 10,
        /// <summary>
        /// Set load limit to 11 amperes.
        /// </summary>
        LimitTo11Ampere = 11,
        /// <summary>
        /// Set load limit to 12 amperes.
        /// </summary>
        LimitTo12Ampere = 12,
        /// <summary>
        /// Set load limit to 13 amperes.
        /// </summary>
        LimitTo13Ampere = 13,
        /// <summary>
        /// Set load limit to 14 amperes.
        /// </summary>
        LimitTo14Ampere = 14,
        /// <summary>
        /// Set load limit to 15 amperes.
        /// </summary>
        LimitTo15Ampere = 15,
        /// <summary>
        /// Set load limit to 16 amperes.
        /// </summary>
        LimitTo16Ampere = 16,
        /// <summary>
        /// Disables the load limit.
        /// </summary>
        Disabled = 255
    }

    /// <summary>
    /// This commands is used to manipulate the state of a specific output.
    /// </summary>
    public class SetOutputCommand : IModControlCommand<SetOutputResponse>
    {
        /// <summary>
        /// Output/channel index.
        /// </summary>
        public byte Channel { get; }
        
        /// <summary>
        /// Switch state (true: on, false: off).
        /// </summary>
        public bool State { get; }
        
        /// <summary>
        /// Load limit.
        /// </summary>
        public LoadLimit Limit { get; }

        /// <inheritdoc />
        public ModControlCommandCode CommandCode => ModControlCommandCode.SetOutput;

        /// <summary>
        /// Creates a new instance to set the switch state and load limit of the specified output.
        /// </summary>
        /// <param name="channel">Target channel.</param>
        /// <param name="state">Switch state (true: on, false: off).</param>
        /// <param name="limit">Load limit in amperes.</param>
        public SetOutputCommand(byte channel, bool state, LoadLimit limit = LoadLimit.DoNotChange)
        {
            Channel = channel;
            State = state;
            Limit = limit;
        }

        /// <inheritdoc />
        public List<byte> GetData()
        {
            var data = new List<byte>();
            data.Add(Channel);
            data.Add(State ? (byte)1 : (byte)0);

            if (Limit != LoadLimit.DoNotChange)
            {
                data.Add((byte)Limit);
            }

            return data;
        }

        /// <inheritdoc />
        public SetOutputResponse ParseResponse(ModControlFrame frame)
        {
            return new SetOutputResponse(frame);
        }
    }
}
