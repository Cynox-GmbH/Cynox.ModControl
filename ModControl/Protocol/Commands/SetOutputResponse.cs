using System.Collections;
using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Expected response for <see cref="SetOutputCommand"/>.
    /// </summary>
    public class SetOutputResponse : ModControlResponse
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

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="channel">Target channel.</param>
        /// <param name="state">Switch state (true: on, false: off).</param>
        /// <param name="limit">Load limit in amperes.</param>
        public SetOutputResponse(byte channel, bool state, LoadLimit limit = LoadLimit.DoNotChange)
        {
            Channel = channel;
            State = state;
            Limit = limit;
        }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public SetOutputResponse(ModControlFrame frame) : base(frame)
        {
            if (Error != ResponseError.None)
            {
                return;
            }

            if (frame.Data.Count >= 2)
            {
                Channel = frame.Data[0];
                State = frame.Data[1] != 0;

                if (frame.Data.Count == 3)
                {
                    Limit = (LoadLimit)frame.Data[2];
                }
            }
            else
            {
                Error = ResponseError.InvalidResponseFormat;
            }
        }

        /// <inheritdoc/>
        public override IList<byte> GetData()
        {
            var data = new List<byte>
            {
                Channel,
                (byte)(State ? 1 : 0),
                (byte)Limit
            };

            return data;
        }
    }
}
