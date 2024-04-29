using System.Collections.Generic;

namespace Cynox.ModControl.Protocol.Commands
{
	/// <summary>
	/// All valid Mod-Control-Protocol command codes.
	/// </summary>
	public enum ModControlCommandCode : byte
	{
		/// <summary>
		/// Command code for <see cref="SetCounterCommand"/>
		/// </summary>
		SetCounter = 0x00,
		/// <summary>
		/// Command code for <see cref="GetCounterCommand"/>
		/// </summary>
		GetCounter = 0x01,
		/// <summary>
		/// Command code for <see cref="GetAllCountersCommand"/>
		/// </summary>
		GetAllCounters = 0x02,
		/// <summary>
		/// Command code for <see cref="SetOutputCommand"/>
		/// </summary>
		SetOutput = 0x03,
		/// <summary>
		/// Command code for <see cref="GetAllOutputs"/>
		/// </summary>
		GetAllOutputs = 0x04,
		/// Save = 0x20, // obsolete
		/// <summary>
		/// Command code for <see cref="GetProtocolVersionCommand"/>
		/// </summary>
		GetVersion = 0x30,
        /// <summary>
		/// Requests the serial number of the device.
		/// </summary>
        GetSerial = 0x45,
        /// <summary>
		/// Command code for <see cref="GetSetCreditsCommand"/>
		/// </summary>
		GetSetCredits = 0x50,
		/// <summary>
		/// Command code for <see cref="GetCardIdCommand"/>
		/// </summary>
		GetCardId = 0x51,
		/// <summary>
		/// Invalid / uninitialized.
		/// </summary>
		Invalid = byte.MaxValue
	}

	/// <summary>
	/// Specifies the command interface.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IModControlCommand<out T> where T : ModControlResponse
	{
		/// <summary>
		/// The command specifies the type of the command.
		/// </summary>
		ModControlCommandCode CommandCode { get; }

		/// <summary>
		/// Returns the payload data.
		/// </summary>
		/// <returns></returns>
		List<byte> GetData();

		/// <summary>
		/// Parses a <see cref="ModControlFrame"/> into the expected <see cref="ModControlResponse"/>.
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		T ParseResponse(ModControlFrame frame);
	}
}
