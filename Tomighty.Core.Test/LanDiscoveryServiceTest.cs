using System.Net;
using System.Text;
using NUnit.Framework;

namespace Tomighty
{
    [TestFixture]
    public class LanDiscoveryServiceTest
    {
        [Test]
        public void ShouldParseValidDiscoveryResponse()
        {
            var payload = LanDiscoveryService.BuildResponsePayload(new LanDiscoverySettings
            {
                ApiPort = 5055,
                DiscoveryPort = 5056,
                HostDisplayName = "Office"
            }, IPAddress.Parse("192.168.1.25"));

            var result = LanDiscoveryService.TryParseResponse(payload);

            Assert.NotNull(result);
            Assert.AreEqual("192.168.1.25", result.Host);
            Assert.AreEqual(5055, result.ApiPort);
            Assert.AreEqual("Office", result.DisplayName);
        }

        [Test]
        public void ShouldIgnoreInvalidMessage()
        {
            var result = LanDiscoveryService.TryParseResponse(Encoding.UTF8.GetBytes("invalid-json"));

            Assert.IsNull(result);
        }

        [Test]
        public void ShouldBuildDeduplicationKeyUsingHostAndPort()
        {
            var host = new LanDiscoveredHost { Host = "10.0.0.2", ApiPort = 5055 };
            Assert.AreEqual("10.0.0.2:5055", host.DeduplicationKey);
        }
    }
}
