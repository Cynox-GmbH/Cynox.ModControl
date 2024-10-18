using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Cynox.IO.Connections;
using Timer = System.Timers.Timer;
using Cynox.ModControl.Protocol;
using Cynox.ModControl.Protocol.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cynox.ModControl.Devices
{
    /// <summary>
    /// Represents the base device for Mod-Control-Protocol based communication.
    /// </summary>
    [PublicAPI]
    public class ModControlBase
    {
        /// <summary>
        /// Optional <see cref="ILoggerFactory"/>, used to provide additional logging from internal classes and static members.
        /// </summary>
        public static ILoggerFactory LogFactory { get; set; }

        private static readonly object ConnectionLock = new object();
        private IConnection _connection;
        private readonly List<byte> _receiveBuffer;
        private readonly Timer _responseTimeout;
        private readonly Timer _receiveTimeout;
        private readonly EventWaitHandle _receiveWaitHandle;
        private int _retryCount = 3;

        /// <summary>
        /// Logger.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Returns true if the current <see cref="IConnection"/> is available and connected, otherwise false.
        /// </summary>
        public bool IsConnected {
            get {
                lock (ConnectionLock)
                {
                    return _connection != null && _connection.IsConnected;
                }
            }
        }

        /// <summary>
        /// The logical address of the target device.
        /// There may be multiple devices present on the same connection if a bus system is used.
        /// (default = 1)
        /// </summary>
        public ushort Address { get; set; } = 1;

        /// <summary>
        /// The duration in milliseconds that is waited for a response, before a timeout occurs.
        /// </summary>
        public int ResponseTimeout { get; set; } = 500;

        /// <summary>
        /// Number of automatic retries in case no valid response was received.
        /// </summary>
        public int RetryCount {
            get => _retryCount;
            set {
                if (value > 0 && value <= 10)
                {
                    _retryCount = value;
                }
            }
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public ModControlBase(ILogger<ModControlBase> logger = null)
        {
            Logger = logger ?? LogFactory?.CreateLogger<ModControlBase>() ?? NullLogger<ModControlBase>.Instance;

            _responseTimeout = new Timer();
            _responseTimeout.AutoReset = false;
            _responseTimeout.Elapsed += ResponseTimeoutOnElapsed;

            _receiveTimeout = new Timer(100);
            _receiveTimeout.AutoReset = false;
            _receiveTimeout.Elapsed += ReceiveTimeoutOnElapsed;

            _receiveBuffer = new List<byte>();

            _receiveWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
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
            Logger.LogInformation("Connecting...");

            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            lock (ConnectionLock)
            {
                // ggf. alte connection entfernen und neue zuweisen
                if (connection != _connection)
                {
                    Logger.LogDebug("Discarding previous connection.");
                    Disconnect();

                    if (_connection != null)
                    {
                        _connection.DataReceived -= ConnectionOnDataReceived;
                        _connection = null;
                    }

                    _connection = connection;
                    _connection.DataReceived += ConnectionOnDataReceived;
                }

                if (!_connection.IsConnected)
                {
                    _connection.Connect();
                }
            }

            Logger.LogInformation("Connected");

            if (checkResponse)
            {
                Logger.LogInformation("Check if device responding");

                if (GetProtocolVersion().Version == 0)
                {
                    Logger.LogWarning("Device not responding");
                    throw new TimeoutException("Device not responding.");
                }
                else
                {
                    Logger.LogInformation("Response received");
                }
            }
        }

        /// <summary>
        /// Closes the attached <see cref="IConnection"/>.
        /// </summary>
        /// <exception cref="ConnectionException">if an error occurred while closing the connection.</exception>
        public void Disconnect()
        {
            lock (ConnectionLock)
            {
                Logger.LogInformation("Disconnecting...");
                _connection?.Disconnect();
                Logger.LogInformation("Disconnected");
            }
        }

        /// <summary>
        /// Sends the specified data using the <see cref="IConnection"/>.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <exception cref="InvalidOperationException">If not connected. Call <see cref="Connect"/> first.</exception>
        /// <exception cref="ConnectionException"></exception>
        protected void Send(IEnumerable<byte> data)
        {
            lock (ConnectionLock)
            {
                if (_connection == null)
                {
                    throw new InvalidOperationException("No connection available");
                }

                if (!_connection.IsConnected)
                {
                    throw new InvalidOperationException("Not connected");
                }

                _connection.Send(data.ToList());
            }
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
            Logger.LogDebug($"Sending request (timeout = {timeout})...");

            _receiveBuffer.Clear();
            _receiveTimeout.Stop();
            _responseTimeout.Interval = timeout;
            _responseTimeout.Start();
            _receiveWaitHandle.Reset();

            Send(data);

            int tickStart = Environment.TickCount;

            _receiveWaitHandle.WaitOne(timeout + 1000); // Fallback timeout just in case if handle doesn't get set.

            Logger.LogTrace($"Received {_receiveBuffer.Count} byte(s). Completed after {Environment.TickCount - tickStart}ms.");
            return _receiveBuffer;
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
                Logger.LogError("ModControlFrame.TryParse() failed");
            }

            return response;
        }

        /// <summary>
        /// Sends the specified <see cref="IModControlCommand{T}"/> to the device and waits for a response.
        /// </summary>
        /// <param name="command">The desired command to send.</param>
        /// <param name="timeout">Specifies the desired timeout for the device to respond.</param>
        /// <returns>The <see cref="ModControlFrame"/> response or null if no response was received.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="command"/> is null.</exception>
        /// <exception cref="ConnectionException"></exception>
        public ModControlFrame SendRequest(IModControlCommand<ModControlResponse> command, int timeout = 0)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            int retryCounter = RetryCount;

            ModControlFrame responseFrame = null;

            while (retryCounter > 0)
            {
                responseFrame = SendRequest(new ModControlFrame(Address, command), timeout == 0 ? ResponseTimeout : timeout);

                if (responseFrame != null)
                {
                    break;
                }

                Logger.LogWarning("Request failed. Retry...");
                retryCounter--;
            }

            return responseFrame;
        }

        /// <summary>
        /// Sends the specified <see cref="IModControlCommand{T}"/> to the device and waits for a response.
        /// </summary>
        /// <typeparam name="T">The expected response type for the specified <see cref="IModControlCommand{T}"/></typeparam>
        /// <param name="command">The desired command to send.</param>
        /// <param name="timeout">Specifies the desired timeout for the device to respond.</param>
        /// <returns>The <see cref="ModControlResponse"/> or null if no response was received.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="command"/> is null.</exception>
        /// <exception cref="ConnectionException"></exception>
        public T SendRequestGeneric<T>(IModControlCommand<T> command, int timeout = 0) where T : ModControlResponse
        {
            var frame = SendRequest(command, timeout == 0 ? ResponseTimeout : timeout);
            return command.ParseResponse(frame);
        }

        private void ConnectionOnDataReceived(object sender, ConnectionDataReceivedEventArgs e)
        {
            Logger.LogTrace("Data received: {data}", e.Data.Aggregate("", (s, b) => s + b.ToString("X2")));
            
            _responseTimeout.Stop();
            _receiveTimeout.Stop();
            _receiveTimeout.Start();

            _receiveBuffer.AddRange(e.Data);
        }

        private void ReceiveTimeoutOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Logger.LogTrace("No more data expected.");
            _receiveTimeout.Stop();
            _receiveWaitHandle.Set();
        }

        private void ResponseTimeoutOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Logger.LogTrace("Device not responding.");
            _responseTimeout.Stop();
            _receiveWaitHandle.Set();
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
