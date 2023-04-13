using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using DataReceivedEventArgs = Cynox.ModControl.Connections.DataReceivedEventArgs;
using Timer = System.Timers.Timer;
using Cynox.ModControl.Protocol;
using Cynox.ModControl.Connections;
using Cynox.ModControl.Protocol.Commands;

namespace Cynox.ModControl.Devices
{
	/// <summary>
	/// Represents the base device for Mod-Control-Protocol based communication.
	/// </summary>
	public class ModControlBase
    {
        private IModControlConnection _Connection;
        private readonly List<byte> _ReceiveBuffer;
        private readonly Timer _ResponseTimeout;
        private readonly Timer _ReceiveTimeout;
        private readonly EventWaitHandle _ReceiveWaitHandle;

        /// <summary>
        /// Returns true if the current <see cref="IModControlConnection"/> is available and connected, otherwise false.
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
        /// Uses the specified <see cref="IModControlConnection"/> to connect to the device.
        /// </summary>
        /// <param name="connection">Desired connection type</param>
        /// <param name="checkResponse">If set to true, the method checks if the device is actually responding after the connection is open.</param>
        /// <returns></returns>
        public bool Connect(IModControlConnection connection, bool checkResponse = true)
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

            bool result = _Connection.IsConnected || _Connection.Connect();

            if (result)
            {
                Debug.WriteLine("Connected");

                if (checkResponse)
                {
                    result = GetVersion().Version != 0;
                }
            }
            else
            {
                Debug.WriteLine("Connect failed");
            }

            return result;
        }

        /// <summary>
        /// Closes the attached <see cref="IModControlConnection"/>.
        /// </summary>
        public void Disconnect()
        {
            _Connection?.Disconnect();
        }

        /// <summary>
        /// Sends the specified data to the device and waits for a response.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <returns>True if the data has been sent.</returns>
        /// <exception cref="InvalidOperationException">If there is no <see cref="IModControlConnection"/> available or currently not connected.</exception>
        protected bool Send(IEnumerable<byte> data)
        {
            if (_Connection == null || !_Connection.IsConnected)
            {
                throw new InvalidOperationException("Not connected");
            }

            return _Connection.Send(data.ToList());
        }

        /// <summary>
        /// Sends the specified data to the device and waits for a response.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="timeout">Specifies the desired timeout for the device to respond.</param>
        /// <returns>The data that was received.</returns>
        protected List<byte> SendRequest(IEnumerable<byte> data, int timeout)
        {
            Debug.WriteLine($"Sending request (timeout = {timeout})...");

            if (!Send(data))
            {
                return null;
            }

#if DEBUG
				var tickStart = Environment.TickCount;
#endif

            _ReceiveBuffer.Clear();
            _ReceiveTimeout.Stop();
            _ResponseTimeout.Interval = timeout;
            _ResponseTimeout.Start();

            _ReceiveWaitHandle.Reset();
            _ReceiveWaitHandle.WaitOne(10000);

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
        protected ModControlFrame SendRequest(ModControlFrame frame, int timeout)
        {
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
        /// <param name="controlCommand">The desired command to send.</param>
        /// <param name="timeout">Specifies the desired timeout for the device to respond.</param>
        /// <returns>The <see cref="ModControlFrame"/> response or null if no response was received.</returns>
        public ModControlFrame SendRequest(IModControlCommand<ModControlResponse> controlCommand, int timeout = 500)
        {
            int retryCounter = 3;

            ModControlFrame responseFrame = null;

            while (retryCounter > 0)
            {
                responseFrame = SendRequest(new ModControlFrame(Address, controlCommand), timeout);

                if (responseFrame != null)
                {
                    break;
                }

                Debug.WriteLine("Request failed. Retry...");
                retryCounter--;
            }

            return responseFrame;
        }

        private void ConnectionOnDataReceived(object sender, DataReceivedEventArgs e)
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
        /// <returns></returns>
        public GetVersionResponse GetVersion()
        {
            var command = new GetVersionCommand();
            var responseFrame = SendRequest(command);
            return command.ParseResponse(responseFrame);
        }
    }
}
