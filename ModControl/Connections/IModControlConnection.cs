using Cynox.ModControl.Devices;
using System;
using System.Collections.Generic;

namespace Cynox.ModControl.Connections
{
    /// <summary>
    /// Represents a connection interface for use with a <see cref="ModControlDevice"/>.
    /// </summary>
    public interface IModControlConnection
    {
        /// <summary>
        /// Reports that data has been received.
        /// </summary>
        event Action<object, DataReceivedEventArgs> DataReceived;

        /// <summary>
        /// Returns true if currently open/connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Opens the connection.
        /// </summary>
        /// <returns></returns>
        bool Connect();

        /// <summary>
        /// Closes the connection.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sends data.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns></returns>
        bool Send(List<byte> data);
    }
}
