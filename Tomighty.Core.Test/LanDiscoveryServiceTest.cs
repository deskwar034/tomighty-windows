using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Tomighty
{
    [TestFixture]
    public class LanDiscoveryServiceTest
    {
        [Test]
        public void ShouldParseValidDiscoveryResponse()
        {
            var payload = LanDiscoveryService.BuildResponsePayload(new LanDiscoverySettings { ApiPort = 5055, DiscoveryPort = 5056, HostDisplayName = "Office", DiscoveryTimeoutMs = 100 }, IPAddress.Parse("192.168.1.25"));
            var result = LanDiscoveryService.TryParseResponse(payload);
            Assert.NotNull(result);
            Assert.AreEqual("192.168.1.25", result.Host);
            Assert.AreEqual(5055, result.ApiPort);
            Assert.AreEqual("Office", result.DisplayName);
        }

        [Test] public void ShouldIgnoreInvalidMessage() { Assert.IsNull(LanDiscoveryService.TryParseResponse(Encoding.UTF8.GetBytes("invalid-json"))); }
        [Test] public void ShouldBuildDeduplicationKeyUsingHostAndPort() { var host = new LanDiscoveredHost { Host = "10.0.0.2", ApiPort = 5055 }; Assert.AreEqual("10.0.0.2:5055", host.DeduplicationKey); }
        [Test] public void ShouldCalculateSubnetBroadcast() { var result = LanDiscoveryService.GetSubnetBroadcast(IPAddress.Parse("192.168.1.10"), IPAddress.Parse("255.255.255.0")); Assert.AreEqual(IPAddress.Parse("192.168.1.255"), result); }

        [Test]
        public void ShouldContainGlobalBroadcastEndpoint()
        {
            var endpoints = LanDiscoveryService.GetBroadcastEndpoints(5056).ToList();
            Assert.IsTrue(endpoints.Any(e => e.Address.Equals(IPAddress.Broadcast) && e.Port == 5056));
        }

        [Test]
        public void DiscoverAsyncShouldReturnWithoutInvalidOperationExceptionWhenNoHostsReply()
        {
            var settings = new LanDiscoverySettings { DiscoveryPort = 5056, DiscoveryTimeoutMs = 200, ApiPort = 5055 };
            var service = new LanDiscoveryService();
            var stopwatch = Stopwatch.StartNew();
            var result = service.Client.DiscoverAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
            stopwatch.Stop();
            Assert.IsNotNull(result); Assert.AreEqual(0, result.Count); Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 2000);
        }

        [Test]
        public void DiscoverAsyncShouldReturnPartialResultWhenCancellationTokenIsAlreadyCancelled()
        {
            var settings = new LanDiscoverySettings { DiscoveryPort = 5056, DiscoveryTimeoutMs = 5000, ApiPort = 5055 };
            var service = new LanDiscoveryService();
            var cts = new CancellationTokenSource(); cts.Cancel();
            var result = service.Client.DiscoverAsync(settings, cts.Token).GetAwaiter().GetResult();
            Assert.IsNotNull(result); Assert.AreEqual(0, result.Count);
        }

        [Test] public void InvalidDiscoveryPortShouldThrow() { Assert.Throws<ArgumentOutOfRangeException>(() => LanDiscoveryService.ValidateSettings(new LanDiscoverySettings { DiscoveryPort = 0, ApiPort = 5055, DiscoveryTimeoutMs = 10 })); }
        [Test] public void InvalidApiPortShouldThrow() { Assert.Throws<ArgumentOutOfRangeException>(() => LanDiscoveryService.ValidateSettings(new LanDiscoverySettings { DiscoveryPort = 5056, ApiPort = 70000, DiscoveryTimeoutMs = 10 })); }
        [Test] public void NegativeTimeoutShouldThrow() { Assert.Throws<ArgumentOutOfRangeException>(() => LanDiscoveryService.ValidateSettings(new LanDiscoverySettings { DiscoveryPort = 5056, ApiPort = 5055, DiscoveryTimeoutMs = -1 })); }

        [Test] public void TryParseResponseShouldRejectInvalidApiPort(){ var json="{\"protocol\":\"TOMIGHTY_DISCOVERY_V1\",\"host\":\"192.168.1.2\",\"apiPort\":0,\"discoveryPort\":5056,\"syncMode\":\"LanHost\"}"; Assert.IsNull(LanDiscoveryService.TryParseResponse(Encoding.UTF8.GetBytes(json))); }
        [Test] public void TryParseResponseShouldRejectInvalidDiscoveryPort(){ var json="{\"protocol\":\"TOMIGHTY_DISCOVERY_V1\",\"host\":\"192.168.1.2\",\"apiPort\":5055,\"discoveryPort\":70000,\"syncMode\":\"LanHost\"}"; Assert.IsNull(LanDiscoveryService.TryParseResponse(Encoding.UTF8.GetBytes(json))); }
        [Test] public void TryParseResponseShouldRejectInvalidSyncMode(){ var json="{\"protocol\":\"TOMIGHTY_DISCOVERY_V1\",\"host\":\"192.168.1.2\",\"apiPort\":5055,\"discoveryPort\":5056,\"syncMode\":\"Other\"}"; Assert.IsNull(LanDiscoveryService.TryParseResponse(Encoding.UTF8.GetBytes(json))); }
    }
}
