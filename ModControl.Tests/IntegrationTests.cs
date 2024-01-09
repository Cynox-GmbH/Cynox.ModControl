// NUnit 3 tests
// See documentation : https://github.com/nunit/docs/wiki/NUnit-Documentation

using Cynox.IO.Connections;
using NUnit.Framework;
using Cynox.ModControl.Devices;
using Cynox.ModControl.Protocol;
using Cynox.ModControl.Protocol.Commands;

namespace ModControlTests
{
	[TestFixture]
	public class IntegrationTests
    {
        // Test config
        private const int DEVICEADDRESS = 1;
        private const string TCPADDRESS = "192.168.6.155";
        private const int TCPPORT = 1470;
        private const ulong EXPECTEDCARDID = 1314081842;

        private ModControlDevice _Device = new();

        #region Setup

        [OneTimeSetUp]
        public void Setup()
        {
            // Create device and establish connection
            _Device.Address = DEVICEADDRESS;

            Assert.DoesNotThrow(() => _Device.Connect(new TcpConnection(TCPADDRESS, TCPPORT)));
            var response = _Device.GetProtocolVersion();
            Assert.That(response.Error == ResponseError.None, $"Response error: {response.Error}");
            Assert.That(response.Version != 0, $"Unexpected protocol version: {response.Version}");
            TestContext.WriteLine($"Protocol version: {response.Version}");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _Device.Disconnect();
            Assert.IsFalse(_Device.IsConnected);
        }

        #endregion

        #region ResponseErrorTests

        [Test]
        public void ResponseErrorTests() {
            var address = _Device.Address;
            _Device.Address = 99;
            var result = _Device.GetProtocolVersion();
            Assert.That(result.Error, Is.EqualTo(ResponseError.Timeout), "Sending command to invalid address should cause timeout");

            _Device.Address = address;
            var command = new InvalidCommand();
            var test = _Device.SendRequest(command);
            var response = command.ParseResponse(test);
            Assert.That(response.Error, Is.EqualTo(ResponseError.UnknownCommand), "Sending an invalid command should cause UnknownCommand response");
        }

        private class InvalidResponse : ModControlResponse {
            public InvalidResponse(ModControlFrame frame) : base(frame) {
            }

            public override IList<byte> GetData()
            {
                throw new NotImplementedException();
            }
        }

        private class InvalidCommand : IModControlCommand<InvalidResponse> {
            public ModControlCommandCode CommandCode => ModControlCommandCode.Invalid;
            public List<byte> GetData() {
                return new List<byte>();
            }

            public InvalidResponse ParseResponse(ModControlFrame frame) {
                return new InvalidResponse(frame);
            }
        }

        #endregion

        #region Misc

        [Test]
        public void GetSetCredits() {
            var value = _Device.SetCredits(0, 10);
            Assert.That(value.Credits, Is.EqualTo(10));

            value = _Device.GetCredits(0);
            Assert.That(value.Credits, Is.EqualTo(10));

            value = _Device.AddCredits(0, 10);
            Assert.That(value.Credits, Is.EqualTo(20));

            value = _Device.SubtractCredits(0, 15);
            Assert.That(value.Credits, Is.EqualTo(5));

            value = _Device.SetCredits(0, 0);
            Assert.That(value.Credits, Is.EqualTo(0));

            value = _Device.SetCredits(20, 0);
            Assert.That(value.Error, Is.EqualTo(ResponseError.InvalidParameterFormat));
        }

        [Test]
        public void GetCardId() {
            var value = _Device.GetCardId(0);

            if (value.CardId == 0) {
                Assert.Inconclusive($"Test requires to assign a card with the specified id ({EXPECTEDCARDID}) to channel 0");
            }

            Assert.That(value.CardId, Is.EqualTo(EXPECTEDCARDID));
        }

        [Test]
        public void GetSetCounterTests() {
            GetSetCounter(0, 10);
            GetSetCounter(0, 100);
            GetSetCounter(1, 10);
            GetSetCounter(1, 100);

            void GetSetCounter(byte channel, UInt32 value) {
                // set and read back counter value
                var setCounterResult = _Device.SetCounter(channel, value);
                Assert.That(setCounterResult.Error, Is.EqualTo(ResponseError.None), $"Response error: {setCounterResult.Error}");
                Assert.That(setCounterResult.Channel == channel);
                Assert.That(setCounterResult.Value == value);

                var getCounterResult = _Device.GetCounter(channel);
                Assert.That(getCounterResult.Error, Is.EqualTo(ResponseError.None), $"Response error: {getCounterResult.Error}");
                Assert.That(getCounterResult.Channel == channel);
                Assert.That(getCounterResult.Value == value);

                var getAllCountersResult = _Device.GetAllCounters();
                Assert.That(getAllCountersResult.Error, Is.EqualTo(ResponseError.None), $"Response error: {getAllCountersResult.Error}");
                Assert.That(getAllCountersResult.Values, Is.Not.Null);
                Assert.That(getAllCountersResult.Values.Count, Is.AtLeast(channel + 1));
                Assert.That(getAllCountersResult.Values[channel], Is.EqualTo(value));
            }

        }

        [Test]
        public void GetSetOutputTests()
        {
            GetSetOutput(0, true, LoadLimit.DoNotChange);
            GetSetOutput(0, false, LoadLimit.DoNotChange);
            GetSetOutput(1, true, LoadLimit.DoNotChange);
            GetSetOutput(1, false, LoadLimit.DoNotChange);
            GetSetOutput(1, false, LoadLimit.LimitTo4Ampere);
            GetSetOutput(1, false, LoadLimit.LimitTo16Ampere);
            GetSetOutput(1, false, LoadLimit.Disabled);

            void GetSetOutput(byte channel, bool state, LoadLimit limit) {
                var setOutputResult = _Device.SetOutput(channel, state, limit);
                Assert.That(setOutputResult.Error, Is.EqualTo(ResponseError.None), $"Response error: {setOutputResult.Error}");
                Assert.That(setOutputResult.Channel == channel);
                Assert.That(setOutputResult.State == state);
                Assert.That(setOutputResult.Limit == limit);

                var getAllOutputsResult = _Device.GetAllOutputs();
                Assert.That(getAllOutputsResult.Error, Is.EqualTo(ResponseError.None), $"Response error: {getAllOutputsResult.Error}");
                Assert.That(getAllOutputsResult.States, Is.Not.Null);
                Assert.That(getAllOutputsResult.States.Count, Is.AtLeast(channel + 1));
                Assert.That(getAllOutputsResult.States[channel], Is.EqualTo(state ? GetAllOutputsResponse.OutPutState.On : GetAllOutputsResponse.OutPutState.Off));
            }
        }

        #endregion
    }
}
