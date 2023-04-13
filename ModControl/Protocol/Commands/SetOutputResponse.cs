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
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public SetOutputResponse(ModControlFrame frame) : base(frame)
        {
            if (Error != ResponseError.None)
            {
                return;
            }

            if (Data.Count >= 2)
            {
                Channel = Data[0];
                State = Data[1] != 0;

                if (Data.Count == 3)
                {
                    Limit = (LoadLimit)Data[2];
                }
            }
            else
            {
                Error = ResponseError.InvalidResponseFormat;
            }
        }
    }
}
