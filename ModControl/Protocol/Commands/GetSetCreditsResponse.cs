using System;
using System.Linq;

namespace Cynox.ModControl.Protocol.Commands
{
    /// <summary>
    /// Response to <see cref="GetSetCreditsCommand"/>.
    /// </summary>
    public class GetSetCreditsResponse : ModControlResponse
	{
		/// <summary>
		/// The channel number.
		/// </summary>
		public byte Channel { get; }

		/// <summary>
		/// Specifies what operation should be performed.
		/// </summary>
		public GetSetCreditsCommand.CreditAction Action { get; }
		
		/// <summary>
		/// The current/resulting credit value.
		/// </summary>
		public UInt16 Credits { get; }

		/// <summary>
		/// Creates a new instance and tries to parse the data from the specified <see cref="ModControlFrame"/>.
		/// </summary>
		/// <param name="frame"></param>
		public GetSetCreditsResponse(ModControlFrame frame) : base(frame) {
			if (Error != ResponseError.None) {
				return;
			}

			if (Data.Count == 4) {
				Channel = Data[0];
				Action = (GetSetCreditsCommand.CreditAction)Data[1];
				var valueData = Data.GetRange(2, 2).ToArray().Reverse().ToArray();
				Credits = BitConverter.ToUInt16(valueData, 0);
			} else {
				Error = ResponseError.InvalidResponseFormat;
			}
		}
	}
}
