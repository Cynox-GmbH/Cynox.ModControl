using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Timers;
using Cynox.IO.Connections;
using Timer = System.Timers.Timer;
using Cynox.ModControl.Protocol;
using Cynox.ModControl.Protocol.Commands;

namespace Cynox.ModControl.Devices
{
	/// <summary>
	/// Represents the base device for Mod-Control-Protocol based communication.
	/// </summary>
	public class ModControlBase
    {
        private IConnection _Connection;
        private readonly List<byte> _ReceiveBuffer;
        private readonly Timer _ResponseTimeout;
        private readonly Timer _ReceiveTimeout;
        private readonly EventWaitHandle _ReceiveWaitHandle;

        /// <summary>
        /// Returns true if the current <see cref="IConnection"/> is available and connected, otherwise false.
        /// </summary>
        public bool IsConnected => _Connection != null && _Connection.IsConnected;

        /// <summary>
        /// The logical address of the target device.
        /// There may be multiple devices present on the same connection if a bus system is used.
        /// (default = 1)
        /// </summary>
        public ushort Address { get; set; } = 1;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public ModControlBase()
        {
            _ResponseTimeout = new Timer();
            _ResponseTimeout.AutoReset = false;
            _ResponseTimeout.Elapsed += ResponseTimeoutOnElapsed;

            _ReceiveTimeout = new Timer(100);
            _ReceiveTimeout.AutoReset = false;
            _ReceiveTimeout.Elapsed += ReceiveTimeoutOnElapsed;

            _ReceiveBuffer = new List<byte>();

            _ReceiveWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        /// <summary>
        /// Uses the specified <see cref="IConnection"/> to connect to the device.
        /// </summary>
        /// <param name="connection">Desired connection type</param>
        /// <param name="checkResponse">If set to true, the method checks if the device is actually responding after the connection is open.</param>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is null.</exception>
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="TimeoutException">If <paramref name="checkResponse"/> was set to true and the device did not respond.</exception>
        public void Connect(IConnection connection, bool checkResponse = true)
        {
            Debug.WriteLine("Connecting...");

            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            // ggf. alte connection entfernen und neue zuweisen
            if (connection != _Connection)
            {
                Disconnect();

                if (_Connection != null)
                {
                    _Connection.DataReceived -= ConnectionOnDataReceived;
                    _Connection = null;
                }

                _Connection = connection;
                _Connection.DataReceived += ConnectionOnDataReceived;
            }

            if (_Connection.IsConnected)
            {
                return;
            }

            _Connection.Connect();

            Debug.WriteLine("Connected");

            if (checkResponse)
            {
                if (GetProtocolVersion().Version == 0)
                {
                    throw new TimeoutException("Device not responding.");
                }
            }
        }

        /// <summary>
        /// Closes the attached <see cref="IConnection"/>.
        /// </summary>
        /// <exception cref="ConnectionException">if an error occurred while closing the connection.</exception>
        public void Disconnect()
        {
            _Connection?.Disconnect();
        }

        /// <summary>
        /// Sends the specified data using the <see cref="IConnection"/>.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <exception cref="InvalidOperationException">If not connected. Call <see cref="Connect"/> first.</exception>
        /// <exception cref="ConnectionException"></exception>
        protected void Send(IEnumerable<byte> data)
        {
            if (_Connection == null)
            {
                throw new InvalidOperationException("Not connected");
            }

            _Connection.Send(data.ToList());
        }

        /// <summary>
        /// Sends the specified data to the device and waits for a response.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="timeout">Specifies the desired timeout in milliseconds for the device to respond.</param>
        /// <returns>The data that was received.</returns>
        /// <exception cref="ConnectionException"></exception>
        protected List<byte> SendRequest(IEnumerable<byte> data, int timeout)
        {
            Debug.WriteLine($"Sending request (timeout = {timeout})...");

            Send(data);

            #if DEBUG
			var tickStart = Environment.TickCount;
            #endif  

            _ReceiveBuffer.Clear();
            _ReceiveTimeout.Stop();
            _ResponseTimeout.Interval = timeout;
            _ResponseTimeout.Start();

            _ReceiveWaitHandle.Reset();
            _ReceiveWaitHandle.WaitOne(timeout + 1000); // Fallback timeout just in case if handle doesn't get set.

            #if DEBUG
			Debug.WriteLine($"Received {_ReceiveBuffer.Count} byte(s). Completed after {Environment.TickCount - tickStart}ms.");
            #endif

            return _ReceiveBuffer;
        }

        /// <summary>
        /// Sends the specified <see cref="ModControlFrame"/> to the device and waits for a response.
        /// </summary>
        /// <param name="frame">The frame to send.</param>
        /// <param name="timeout">Specifies the desired timeout for the device to respond.</param>
        /// <returns>The <see cref="ModControlFrame"/> response or null if no response was received.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="frame"/> is null.</exception>
        /// <exception cref="ConnectionException"></exception>
        protected ModControlFrame SendRequest(ModControlFrame frame, int timeout)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            var data = SendRequest(frame.GetData(), timeout);

            if (!ModControlFrame.TryParse(data, out ModControlFrame response))
            {
                Debug.WriteLine("ModControlFrame.TryParse() failed");
            }

            return response;
        }

        /// <summary>
        /// Sends the specified <see cref="IModControlCommand{T}"/> to the device and waits for a response.
        /// </summary>
        /// <param name="command">The desired command to send.</param>
        /// <param name="timeout">Specifies the desired timeout for the device to respond.</param>
        /// <returns>The <see cref="ModControlFrame"/> response or null if no response was received.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="command"/> is null.</exception>
        /// <exception cref="ConnectionException"></exception>
        public ModControlFrame SendRequest(IModControlCommand<ModControlResponse> command, int timeout = 500)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            int retryCounter = 3;

            ModControlFrame responseFrame = null;

            while (retryCounter > 0)
            {
                responseFrame = SendRequest(new ModControlFrame(Address, command), timeout);

                if (responseFrame != null)
                {
                    break;
                }

                Debug.WriteLine("Request failed. Retry...");
                retryCounter--;
            }

            return responseFrame;
        }

        private void ConnectionOnDataReceived(object sender, ConnectionDataReceivedEventArgs e)
        {
            Debug.WriteLine("ConnectionOnDataReceived()");

            _ResponseTimeout.Stop();
            _ReceiveTimeout.Stop();
            _ReceiveTimeout.Start();

            _ReceiveBuffer.AddRange(e.Data);
        }

        private void ReceiveTimeoutOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Debug.WriteLine("No more data expected.");
            _ReceiveTimeout.Stop();
            _ReceiveWaitHandle.Set();
        }

        private void ResponseTimeoutOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Debug.WriteLine("Device not responding.");
            _ResponseTimeout.Stop();
            _ReceiveWaitHandle.Set();
        }

        /// <summary>
        /// Requests the supported protocol version.
        /// </summary>
        /// <returns>Return 0, if the device did not respond, otherwise the corresponding protocol version.</returns>
        public GetProtocolVersionResponse GetProtocolVersion()
        {
            var command = new GetProtocolVersionCommand();
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }
    }
}
