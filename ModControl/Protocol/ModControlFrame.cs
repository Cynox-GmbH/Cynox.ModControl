using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cynox.ModControl.Protocol.Commands;
using JetBrains.Annotations;

namespace Cynox.ModControl.Protocol
{
    /// <summary>
    /// Represents a data frame as used in the ModControl protocol.
    /// </summary>
	[PublicAPI]
    public class ModControlFrame
    {
        /// <summary>
        /// The maximum payload length <see cref="ModControlFrame.Data"/>.
        /// </summary>
        public const int MaxDataLength = 122;
        
        /// <summary>
        /// The frame overhead length including address, command, length and crc.
        /// </summary>
        protected const int Overhead = 2 + 1 + 1 + 2;  // address + command + length + crc

        /// <summary>
        /// The address of the target client.
        /// </summary>
        public ushort Address { get; }
        
        /// <summary>
        /// The command code, representing the type of command to be executed.
        /// </summary>
        public byte CommandByte { get; }
        
        /// <summary>
        /// The payload data.
        /// </summary>
        public List<byte> Data { get; }
        
        /// <summary>
        /// The <see cref="ModControlCommandCode"/>, representing the type of command to be executed.
        /// Wraps the value from <see cref="CommandByte"/>.
        /// </summary>
        public ModControlCommandCode CommandCode => (ModControlCommandCode)(CommandByte & 0b01111111);

        /// <summary>
        /// Creates a new instance from scratch.
        /// </summary>
        /// <param name="address">Target client address.</param>
        /// <param name="commandByte">Command code</param>
        /// <param name="data">Payload.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ModControlFrame(ushort address, byte commandByte, IList<byte> data)
        {
            /*
            if (address == 0x2B00 || address == 0x002B || address == 0x2F00)
            {
                throw new ArgumentException($"Address {address} is reserved.", nameof(address));
            }
            */

            if (data.Count > MaxDataLength)
            {
                throw new ArgumentOutOfRangeException(nameof(data), data.Count, $"Maximum payload data length is {MaxDataLength} bytes.");
            }

            Address = address;
            CommandByte = commandByte;
            Data = data.ToList();
        }

        /// <summary>
        /// Creates a new instance, based on a <see cref="IModControlCommand{T}"/>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="command"></param>
        public ModControlFrame(ushort address, IModControlCommand<ModControlResponse> command) : this(address, (byte)command.CommandCode, command.GetData()) { }

        /// <summary>
        /// Returns the raw data of the frame.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Data"/> exceeds the maximum range <see cref="MaxDataLength"/>.</exception>
        /// <returns></returns>
        public List<byte> GetData()
        {
            if (Data.Count > MaxDataLength)
            {
                throw new InvalidOperationException($"Maximum payload data length exceeded.");
            }

            var data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(Address).Reverse());
            data.Add(CommandByte);
            data.Add((byte)Data.Count);
            data.AddRange(Data);

            var crc = CalcCrc(data);

            data.AddRange(BitConverter.GetBytes(crc).Reverse());

            return data;
        }

        /// <summary>
        /// Tries to parse a <see cref="ModControlFrame"/> from the specified raw frame data.
        /// </summary>
        /// <param name="frameData">Raw frame data.</param>
        /// <param name="frame">Resulting frame.</param>
        /// <returns></returns>
        public static bool TryParse(List<byte> frameData, out ModControlFrame frame)
        {
            Debug.WriteLine($"TryParse {nameof(ModControlFrame)}");

            frame = null;

            if (frameData == null || frameData.Count < Overhead)
            {
                return false;
            }

            // check CRC
            var crcData = frameData.GetRange(frameData.Count - 2, 2).ToArray().Reverse().ToArray();
            ushort actualCrc = BitConverter.ToUInt16(crcData, 0);
            ushort expectedCrc = CalcCrc(frameData.GetRange(0, frameData.Count - 2));

            if (actualCrc != expectedCrc)
            {
                Debug.WriteLine("CRC mismatch");
                return false;
            }

            ushort address = BitConverter.ToUInt16(frameData.GetRange(0, 2).ToArray().Reverse().ToArray(), 0);
            byte command = frameData[2];
            byte dataLength = frameData[3];

            // check if data length fields matches up with total data length
            if (frameData.Count != Overhead + dataLength)
            {
                Debug.WriteLine("Unexpected data length");
                return false;
            }

            var data = frameData.GetRange(4, dataLength);
            frame = new ModControlFrame(address, command, data);

            return true;
        }

        /// <summary>
        /// Calculates the CRC for the given data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ushort CalcCrc(List<byte> data)
        {
            ushort value = 0xFFFF;

            foreach (var b in data)
            {
                value = CalcCrc(b, value);
            }

            return value;
        }

        /// <summary>
        /// Updates the CRC using the specified start value.
        /// </summary>
        /// <param name="dataByte"></param>
        /// <param name="srcCrc">Start value.</param>
        /// <returns></returns>
        private static ushort CalcCrc(byte dataByte, ushort srcCrc)
        {
            ushort newCrc = srcCrc;
            newCrc = (ushort)(newCrc ^ dataByte);

            for (ushort i = 8; i > 0; i--)
            {
                if ((newCrc & 0x0001) != 0)
                {
                    newCrc = (ushort)((newCrc >> 1) ^ 0xA001);
                }
                else
                {
                    newCrc = (ushort)(newCrc >> 1);
                }
            }
            return newCrc;
        }

    }
}
