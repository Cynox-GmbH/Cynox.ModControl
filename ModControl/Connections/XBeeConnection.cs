using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cynox.ModControl.Connections
{
    class XBeeConnection : IModControlConnection
    {
        /// <inheritdoc />
        public event Action<object, DataReceivedEventArgs> DataReceived;

        /// <inheritdoc />
        public bool IsConnected => _Connection?.IsConnected ?? false;

        public string NodeId { get; set; }

        private readonly IModControlConnection _Connection;

        public XBeeConnection(IModControlConnection connection, string nodeId)
        {
            _Connection = connection;
            _Connection.DataReceived += Connection_OnDataReceived;

            NodeId = nodeId;
        }

        private void Connection_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        /// <inheritdoc />
        public bool Connect()
        {
            if (_Connection == null)
            {
                return false;
            }

            if (!_Connection.IsConnected)
            {
                bool connected = _Connection.Connect();
                return connected;
            }

            return false;
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            _Connection?.Disconnect();
        }

        public bool Send(string text)
        {
            var data = Encoding.ASCII.GetBytes(text).ToList();
            return Send(data);
        }

        /// <inheritdoc />
        public bool Send(List<byte> data)
        {
            if (_Connection == null)
            {
                return false;
            }

            return _Connection.Send(data);

        }
    }
}
