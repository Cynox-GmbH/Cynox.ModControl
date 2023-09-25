using System.Diagnostics;
using Cynox.IO.Connections;
using Cynox.ModControl.Devices;
using Cynox.ModControl.Protocol.Commands;
using NUnit.Framework;

namespace ModControlTests
{
    internal class Examples
    {
        [Test]
        public void QuickStart()
        {
            // Create default device
            var device = new ModControlDevice {
                Address = 1,
            };

            using IConnection connection = new TcpConnection("192.168.6.155", 1470);
            
            // Establish connection
            try
            {
                device.Connect(connection);
            }
            catch (ConnectionException ex)
            {
                Debug.WriteLine($"Failed to connect. {ex}");
                return;
            }

            var setOutputResponse = device.SetOutput(0, true);

            // Check if request was successful
            if (setOutputResponse.Error != ResponseError.None)
            {
                Debug.WriteLine($"Request failed: {setOutputResponse.Error}");
            }

            var getCounterResponse = device.GetCounter(2);

            // Check if request was successful
            if (getCounterResponse.Error != ResponseError.None)
            {
                Debug.WriteLine($"Request failed: {getCounterResponse.Error}");
            }
            else
            {
                // Log counter value
                Debug.WriteLine($"Current counter value for channel {getCounterResponse.Channel} = {getCounterResponse.Value}");
            }

            // Disconnect
            device.Disconnect();
        }
    }
}
