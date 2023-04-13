using System;
using System.Collections.Generic;

namespace Cynox.ModControl.Connections
{
    /// <summary>
    /// Event arguments for <see cref="IModControlConnection.DataReceived"/> event.
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Received data.
        /// </summary>
        public List<byte> Data { get; }

        /// <summary>
        /// Creates a new instance providing the data that has been received.
        /// </summary>
        /// <param name="data"></param>
        public DataReceivedEventArgs(List<byte> data)
        {
            Data = data;
        }
    }
}
