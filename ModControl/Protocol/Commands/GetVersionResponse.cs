namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Response to <see cref="GetVersionCommand"/>.
    /// </summary>
    public class GetVersionResponse : ModControlResponse
    {
        /// <summary>
        /// Protocol version number.
        /// </summary>
        public byte Version { get; }

        /// <summary>
        /// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
        /// </summary>
        /// <param name="frame"></param>
        public GetVersionResponse(ModControlFrame frame) : base(frame)
        {
            if (Error != ResponseError.None)
            {
                return;
            }

            if (Data.Count == 1)
            {
                Version = Data[0];
            }
            else
            {
                Error = ResponseError.InvalidResponseFormat;
            }
        }
    }
}
