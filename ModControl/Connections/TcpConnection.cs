using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Cynox.ModControl.Connections
{
    /// <summary>
    /// <see cref="IModControlConnection"/> to be used for TCP connections.
    /// </summary>
    public class TcpConnection : IModControlConnection
    {
        private readonly TcpClientWrapper _Client;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="ipAddress">The target IP-address</param>
        /// <param name="port">The target port</param>
        public TcpConnection(IPAddress ipAddress, int port)
        {
            _Client = new TcpClientWrapper(ipAddress, port);
            _Client.DataReceived += ClientOnDataReceived;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="ipAddress">The target IP-address</param>
        /// <param name="port">The target port</param>
        public TcpConnection(string ipAddress, int port) : this(IPAddress.Parse(ipAddress), port)
        {
        }

        /// <summary>
        /// Gets or sets the current target IP address.
        /// </summary>
        public IPAddress IpAddress
        {
            get => _Client.IpAddress;
            set => _Client.IpAddress = value;
        }

        /// <summary>
        /// Gets or sets the current target port.
        /// </summary>
        public int Port
        {
            get => _Client.Port;
            set => _Client.Port = value;
        }

        private void ClientOnDataReceived(TcpClientWrapperDataReceivedEventArgs args)
        {
            OnDataReceived(args.Data);
        }

        private void OnDataReceived(List<byte> data)
        {
            var handler = DataReceived;
            handler?.Invoke(this, new DataReceivedEventArgs(data));
        }

        #region IModControlConnection

        /// <inheritdoc />
        public event Action<object, DataReceivedEventArgs> DataReceived;

        /// <inheritdoc />
        public bool Connect()
        {
            return _Client != null && _Client.Connect();
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            _Client?.Disconnect();
        }

        /// <inheritdoc />
        public bool IsConnected
        {
            get
            {
                if (_Client != null)
                {
                    return _Client.IsConnected;
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool Send(List<byte> data)
        {
            if (!data.Any())
            {
                return true;
            }

            try
            {
                if (_Client != null)
                {
                    return _Client.Send(data) != 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        #endregion
    }
}
