using Cynox.IO.Connections;
using Cynox.ModControl.Devices;
using Cynox.ModControl.Protocol;
using Cynox.ModControl.Protocol.Commands;
using Moq;
using NUnit.Framework;

namespace ModControlTests
{
    public class MiscTests
    {
        [Test]
        public void ResponseFromDifferentDeviceAddress_ShouldBeIgnored()
        {
            const int deviceAddress = 1;
            const byte expectedVersion = 100;

            // Setup
            var connMock = new Mock<IConnection>();

            // Report always connected
            connMock.SetupGet(m => m.IsConnected).Returns(true);

            // Respond to first GetVersion request with correct device address and to following request with different address
            ushort responseAddress = deviceAddress;
            connMock.Setup(m => m.Send(It.IsAny<IList<byte>>())).Callback<IList<byte>>(data =>
            {
                ModControlFrame.TryParse(data.ToList(), out var request);
                
                switch (request.CommandCode)
                {
                    case ModControlCommandCode.GetVersion:
                        var versionResponse = new GetProtocolVersionResponse(expectedVersion);
                        // ReSharper disable once AccessToModifiedClosure
                        var getVersionFrame = new ModControlFrame(responseAddress++, ModControlCommandCode.GetVersion, versionResponse.GetData());
                        connMock.Raise(x => x.DataReceived += null, new ConnectionDataReceivedEventArgs(getVersionFrame.GetData()));
                        break;
                }
            });

            // Execute
            ModControlBase.LogFactory = Tools.LogFactory;
            var device = new ModControlDevice
                         {
                             Address = deviceAddress,
                             RetryCount = 1
                         };
            
            device.Connect(connMock.Object, false);

            Assert.That(() => device.GetProtocolVersion()?.Version, Is.EqualTo(expectedVersion));
            Assert.That(() => device.GetProtocolVersion()?.Version, Is.Zero);

            device.Disconnect();

            // Verify
            connMock.Verify(m => m.Send(It.IsAny<IList<byte>>()), Times.Exactly(2));
        }

        [Test]
        public void ResponseFromDifferentCommand_ShouldBeIgnored()
        {
            const int deviceAddress = 1;
            const byte expectedVersion = 100;

            // Setup
            var connMock = new Mock<IConnection>();

            // Report always connected
            connMock.SetupGet(m => m.IsConnected).Returns(true);

            // Respond to first GetVersion request with correct and to following with different command code
            int getVersionCount = 0;
            connMock.Setup(m => m.Send(It.IsAny<IList<byte>>())).Callback<IList<byte>>(data =>
            {
                ModControlFrame.TryParse(data.ToList(), out var request);

                switch (request.CommandCode)
                {
                    case ModControlCommandCode.GetVersion:
                        var versionResponse = new GetProtocolVersionResponse(expectedVersion);

                        if (getVersionCount++ == 0)
                        {
                            var getVersionFrame = new ModControlFrame(deviceAddress, ModControlCommandCode.GetVersion, versionResponse.GetData());
                            connMock.Raise(x => x.DataReceived += null, new ConnectionDataReceivedEventArgs(getVersionFrame.GetData()));
                        }
                        else
                        {
                            var getVersionFrame = new ModControlFrame(deviceAddress, ModControlCommandCode.GetCardId, versionResponse.GetData());
                            connMock.Raise(x => x.DataReceived += null, new ConnectionDataReceivedEventArgs(getVersionFrame.GetData()));
                        }

                        break;
                }
            });

            // Execute
            ModControlBase.LogFactory = Tools.LogFactory;
            var device = new ModControlDevice
            {
                Address = deviceAddress,
                RetryCount = 1
            };

            device.Connect(connMock.Object, false);

            Assert.That(() => device.GetProtocolVersion()?.Version, Is.EqualTo(expectedVersion));
            Assert.That(() => device.GetProtocolVersion()?.Version, Is.Zero);

            device.Disconnect();

            // Verify
            connMock.Verify(m => m.Send(It.IsAny<IList<byte>>()), Times.Exactly(2));
        }

    }
}
