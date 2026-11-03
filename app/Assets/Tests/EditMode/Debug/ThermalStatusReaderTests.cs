using DiyVr.Diagnostics;
using NUnit.Framework;

namespace DiyVr.Tests.Diagnostics
{
    public class ThermalStatusReaderTests
    {
        [TestCase(0, "None")]
        [TestCase(1, "Light")]
        [TestCase(2, "Moderate")]
        [TestCase(3, "Severe")]
        [TestCase(4, "Critical")]
        [TestCase(5, "Emergency")]
        [TestCase(6, "Shutdown")]
        public void ToLabel_MapsKnownCodes(int code, string expected)
        {
            Assert.AreEqual(expected, ThermalStatusReader.ToLabel(code));
        }

        [TestCase(-1)]
        [TestCase(7)]
        [TestCase(100)]
        public void ToLabel_UnknownCodes_AreNotAvailable(int code)
        {
            Assert.AreEqual("n/a", ThermalStatusReader.ToLabel(code));
        }

        [Test]
        public void Read_InEditor_IsUnavailable()
        {
            var r = new ThermalStatusReader();
            Assert.AreEqual(ThermalStatusReader.Unavailable, r.Read());
            Assert.AreEqual("n/a", r.ReadLabel());
        }
    }
}
