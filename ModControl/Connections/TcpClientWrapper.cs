using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace Cynox.ModControl.Connections
{
    internal delegate void TcpClientWrapperDataReceivedDelegate(TcpClientWrapperDataReceivedEventArgs args);

    internal class TcpClientWrapperDataReceivedEventArgs
    {
        public List<byte> Data { get; }

        public TcpClientWrapperDataReceivedEventArgs(List<byte> data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Wrapper around the TcpClient class that simplifies usage when sending and receiving data from a connected socket.
    /// </summary>
    internal class TcpClientWrapper : IDisposable
    {
        public event TcpClientWrapperDataReceivedDelegate DataReceived;

        private const int DEFAULT_CHECK_CONNECTION_INTERVAL = 5000;
        private const int DEFAULT_TRY_RECONNECT_INTERVAL = 30000;

        #region Public Properties

        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }
        public TimeSpan TryReconnectInterval { get; set; } = TimeSpan.FromMilliseconds(DEFAULT_TRY_RECONNECT_INTERVAL);
        public TimeSpan CheckConnectionInterval { get; set; } = TimeSpan.FromMilliseconds(DEFAULT_CHECK_CONNECTION_INTERVAL);

        /// <summary>
        /// Checks if the underlying Socket is still connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (_Client?.Client is Socket socket)
                {
                    return socket.IsConnectedMsdn();
                }

                return false;
            }
        }

        #endregion

        #region private Fields

        private TcpClient _Client;
        private readonly byte[] _Buffer;
        private readonly Timer _CheckConnectionTimer;

        #endregion

        public TcpClientWrapper(IPAddress address, int port, int bufferSize = 2048)
        {
            IpAddress = address ?? throw new ArgumentNullException(nameof(address));
            Port = port;
            _Buffer = new byte[bufferSize];

            _CheckConnectionTimer = new Timer(DEFAULT_CHECK_CONNECTION_INTERVAL);
            _CheckConnectionTimer.Elapsed += CheckConnectionTimer_OnElapsed;
        }

        public TcpClientWrapper(string ipAddress, int port) : this(IPAddress.Parse(ipAddress), port)
        {
        }

        /// <summary>
        /// Sends data to a connected Socket using the specified SocketFlags.
        /// </summary>
        /// <param name="data">Data to be sent.</param>
        /// <param name="blocking">Specifies if the Socket should be set to blocking mode.</param>
        /// <param name="flags">A bitwise combination of the SocketFlags values.</param>
        /// <returns>The number of bytes sent to the Socket.</returns>
        /// <exception cref="SocketException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public int Send(List<byte> data, bool blocking = true, SocketFlags flags = SocketFlags.None)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_Client?.Client != null)
            {
                _Client.Client.Blocking = blocking;
                return _Client.Client.Send(data.ToArray(), flags);
            }

            return 0;
        }

        /// <summary>
        /// Closes the socket connection and allows reuse of the socket.
        /// Disposes this TcpClient instance and requests that the underlying TCP connection be closed.
        /// </summary>
        public void Disconnect()
        {
            _CheckConnectionTimer.Stop();

            if (_Client?.Client == null)
            {
                return;
            }

            try
            {
                var socket = _Client.Client;
                socket.Disconnect(false);
            }
            catch (SocketException)
            {
                // client nicht verbunden
            }

            _Client?.Close();
        }

        /// <summary>
        /// Performs a request for a remote host connection.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Connect(int timeout = 1000)
        {
            try
            {
                _Client = new TcpClient();

                // async Connect verwenden, da hier ein Timeout angegeben werden kann
                var asyncResult = _Client.BeginConnect(IpAddress.ToString(), Port, null, null);
                asyncResult.AsyncWaitHandle.WaitOne(timeout);

                if (!_Client.Connected)
                {
                    return false;
                }

                // we have connected
                _Client.EndConnect(asyncResult);
                _Client.Client?.SetKeepAlive(2000, 500);

                _Client?.Client?.BeginReceive(_Buffer, 0, _Buffer.Length, 0, ReceiveCallback, null);
                _CheckConnectionTimer.Start();
                return true;
            }
            catch (SocketException)
            {
                if (_Client != null)
                {
                    _Client.Close();
                    _Client = null;
                }

                return false;
            }
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            try
            {
                Socket client = _Client.Client;

                if (client == null)
                {
                    return;
                }

                Debug.WriteLine("EndReceive");
                int rcvCount = client.EndReceive(asyncResult);

                if (rcvCount > 0)
                {
                    byte[] buf = new byte[rcvCount];
                    Array.Copy(_Buffer, buf, rcvCount);

                    TcpClientWrapperDataReceivedEventArgs args = new TcpClientWrapperDataReceivedEventArgs(new List<byte>(buf));
                    OnDataReceived(args);
                }

                // empfang forsetzen
                Debug.WriteLine("BeginReceive");
                client.BeginReceive(_Buffer, 0, _Buffer.Length, 0, ReceiveCallback, null);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        private void OnDataReceived(TcpClientWrapperDataReceivedEventArgs args)
        {
            var handler = DataReceived;
            handler?.Invoke(args);
        }

        private void CheckConnectionTimer_OnElapsed(object sender, ElapsedEventArgs e)
        {
            _CheckConnectionTimer.Stop();

            try
            {
                if (IsConnected)
                {
                    Debug.WriteLine("Connection OK");
                    _CheckConnectionTimer.Interval = CheckConnectionInterval.TotalMilliseconds;
                }
                else
                {
                    Debug.WriteLine("Connection not ready");
                    Disconnect();

                    bool success = Connect();

                    if (success)
                    {
                        Debug.WriteLine("Re-connect successful");
                        _CheckConnectionTimer.Interval = CheckConnectionInterval.TotalMilliseconds;
                    }
                    else
                    {
                        Debug.WriteLine("Re-connect failed");
                        _CheckConnectionTimer.Interval = TryReconnectInterval.TotalMilliseconds;
                        Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                _CheckConnectionTimer.Start();
            }
        }

        #region IDisposable

        // Flag: Has Dispose already been called?
        private bool _Disposed;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposing)
            {
                Disconnect();
                _Client?.Dispose();
                _CheckConnectionTimer?.Stop();
                _CheckConnectionTimer?.Dispose();
            }

            // Free any unmanaged objects here.
            _Disposed = true;
        }

        ~TcpClientWrapper()
        {
            Dispose(false);
        }

        #endregion

    }

    internal static class SocketExtensions
    {
        /// <summary>
        /// Checks if the Socket is still connected by performing a non-blocking, zero-byte Send() call.
        /// </summary>
        /// <param name="client"></param>
        /// <returns>true if connected, otherwise false</returns>
        public static bool IsConnectedMsdn(this Socket client)
        {
            // From MSDN:
            // The Connected property gets the connection state of the Socket as of the last I/O operation.
            // When it returns false, the Socket was either never connected, or is no longer connected.
            // The value of the Connected property reflects the state of the connection as of the most recent operation.

            // If you need to determine the current state of the connection, make a nonblocking, zero - byte Send call.
            // If the call returns successfully or throws a WAEWOULDBLOCK error code(10035), then the socket is still connected; otherwise, the socket is no longer connected.

            // If you call Connect on a User Datagram Protocol(UDP) socket, the Connected property always returns true;
            // however, this action does not change the inherent connectionless nature of UDP.

            bool blockingState = client.Blocking;
            bool result = false;

            try
            {
                byte[] tmp = new byte[1];
                client.Blocking = false;
                client.Send(tmp, 0, 0);
                result = true;
            }
            catch (SocketException e)
            {
                // 10035 == WSAEWOULDBLOCK
                if (e.NativeErrorCode.Equals(10035))
                {
                    result = true;
                }
            }

            try
            {
                client.Blocking = blockingState;
            }
            catch (SocketException)
            {
                // setting the Blocking property seems to fail if a SocketException was thrown on Send()
            }

            return result;
        }

        /// <summary>
        /// Sets the keep-alive interval for the socket.
        /// 
        /// The socket connection is considered to be dead after (timeout + 10 * interval) milliseconds.
        /// After that, any read/write or poll operations should fail on the socket.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="timeout">Time between two keep alive "pings".</param>
        /// <param name="interval">Time between two keep alive "pings" when first one fails. Repeated 10 times (hardcoded since Vista?!).</param>
        /// <returns>true if the keep alive infos were succefully modified.</returns>
        public static bool SetKeepAlive(this Socket socket, ulong timeout, ulong interval)
        {
            const int BytesPerLong = 4; // 32 / 8
            const int BitsPerByte = 8;

            try
            {
                // Array to hold input values.
                var input = new[] {
                    timeout == 0 || interval == 0 ? 0UL : 1UL, // on or off
					timeout,
                    interval
                };

                // Pack input into byte struct.
                byte[] inValue = new byte[3 * BytesPerLong];
                for (int i = 0; i < input.Length; i++)
                {
                    inValue[i * BytesPerLong + 3] = (byte)((input[i] >> ((BytesPerLong - 1) * BitsPerByte)) & 0xff);
                    inValue[i * BytesPerLong + 2] = (byte)((input[i] >> ((BytesPerLong - 2) * BitsPerByte)) & 0xff);
                    inValue[i * BytesPerLong + 1] = (byte)((input[i] >> ((BytesPerLong - 3) * BitsPerByte)) & 0xff);
                    inValue[i * BytesPerLong + 0] = (byte)((input[i] >> ((BytesPerLong - 4) * BitsPerByte)) & 0xff);
                }

                // Create bytestruct for result (bytes pending on server socket).
                byte[] outValue = BitConverter.GetBytes(0);

                // Write SIO_VALS to Socket IOControl.
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socket.IOControl(IOControlCode.KeepAliveValues, inValue, outValue);
            }
            catch (SocketException)
            {
                return false;
            }

            return true;
        }
    }


}
