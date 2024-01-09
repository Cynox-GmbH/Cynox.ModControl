using Cynox.ModControl.Protocol;
using Cynox.ModControl.Protocol.Commands;
using NUnit.Framework;

namespace ModControlTests
{
    [TestFixture]
    class FormatTests
    {
        [Test]
        public void TestCrc() {
            const ushort expectedCrc1 = 0xE445;
            var data1 = new byte[] { 0x00, 0x01, 0x30, 0x00 };
            var actualCrc1 = ModControlFrame.CalcCRC(data1.ToList());
            Assert.AreEqual(expectedCrc1, actualCrc1, "CRC mismatch");

            const ushort expectedCrc2 = 0x4554;
            var data2 = new byte[] { 0x00, 0x01, 0x83, 0x01, 0x02 };
            var actualCrc2 = ModControlFrame.CalcCRC(data2.ToList());
            Assert.AreEqual(expectedCrc2, actualCrc2, "CRC mismatch");
        }

        [Test]
        public void TestGetAllOutputsResponse() {
            // Alle erwarteten Schaltzustände
            var data = new List<byte> {
                0x00, 0x01, 0x02
            };
            
            var frame = new ModControlFrame(1, (byte)ModControlCommandCode.GetAllOutputs, data);
            var response = new GetAllOutputsResponse(frame);
            
            Assert.That(response.Error, Is.EqualTo(ResponseError.None));
            Assert.That(response.States, Has.Count.EqualTo(data.Count));
            Assert.That(response.States[0], Is.EqualTo(GetAllOutputsResponse.OutPutState.Off));
            Assert.That(response.States[1], Is.EqualTo(GetAllOutputsResponse.OutPutState.On));
            Assert.That(response.States[2], Is.EqualTo(GetAllOutputsResponse.OutPutState.Overload));

            // Unerwartete Werte
            frame.Data[1] = byte.MaxValue;
            response = new GetAllOutputsResponse(frame);
            Assert.That(response.Error, Is.EqualTo(ResponseError.InvalidResponseFormat));
        }
    }
}
