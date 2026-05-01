using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tomighty
{
    public class LanDiscoveryService : ILanDiscoveryService
    {
        public const string DiscoverMessage = "TOMIGHTY_DISCOVER_V1";
        public const string DiscoveryProtocol = "TOMIGHTY_DISCOVERY_V1";

        public ILanDiscoveryHost Host { get; private set; }
        public ILanDiscoveryClient Client { get; private set; }

        public LanDiscoveryService()
        {
            Host = new UdpLanDiscoveryHost();
            Client = new UdpLanDiscoveryClient();
        }

        internal static LanDiscoveredHost TryParseResponse(byte[] payload)
        {
            try
            {
                using (var stream = new MemoryStream(payload))
                {
                    var serializer = new DataContractJsonSerializer(typeof(LanDiscoveryResponse));
                    var response = serializer.ReadObject(stream) as LanDiscoveryResponse;
                    if (response == null || response.Protocol != DiscoveryProtocol || string.IsNullOrWhiteSpace(response.Host) || response.ApiPort <= 0)
                        return null;
                    return response.ToModel();
                }
            }
            catch
            {
                return null;
            }
        }

        internal static byte[] BuildResponsePayload(LanDiscoverySettings settings, IPAddress bestIp)
        {
            var response = new LanDiscoveryResponse
            {
                Protocol = DiscoveryProtocol,
                MachineName = Environment.MachineName,
                DisplayName = settings.HostDisplayName,
                Host = bestIp.ToString(),
                ApiPort = settings.ApiPort,
                DiscoveryPort = settings.DiscoveryPort,
                SyncMode = SyncMode.LanHost.ToString(),
                RequiresSharedKey = settings.RequiresSharedKey
            };

            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(LanDiscoveryResponse));
                serializer.WriteObject(stream, response);
                return stream.ToArray();
            }
        }

        internal static IPAddress ResolveBestLocalIp(IPAddress fallback)
        {
            var ips = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));

            var privateIp = ips.FirstOrDefault(IsPreferredPrivateIpv4);
            return privateIp ?? ips.FirstOrDefault() ?? fallback;
        }

        private static bool IsPreferredPrivateIpv4(IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31);
        }

        private class UdpLanDiscoveryHost : ILanDiscoveryHost
        {
            public async Task StartAsync(LanDiscoverySettings settings, CancellationToken cancellationToken)
            {
                using (var udp = new UdpClient(new IPEndPoint(IPAddress.Any, settings.DiscoveryPort)))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var receiveTask = udp.ReceiveAsync();
                        var completed = await Task.WhenAny(receiveTask, Task.Delay(250, cancellationToken));
                        if (completed != receiveTask) continue;

                        var packet = receiveTask.Result;
                        var message = Encoding.UTF8.GetString(packet.Buffer).Trim();
                        if (!string.Equals(message, DiscoverMessage, StringComparison.Ordinal))
                            continue;

                        var bestIp = ResolveBestLocalIp(packet.RemoteEndPoint.Address);
                        var response = BuildResponsePayload(settings, bestIp);
                        try { await udp.SendAsync(response, response.Length, packet.RemoteEndPoint); }
                        catch (SocketException) { }
                    }
                }
            }
        }

        private class UdpLanDiscoveryClient : ILanDiscoveryClient
        {
            public async Task<IReadOnlyCollection<LanDiscoveredHost>> DiscoverAsync(LanDiscoverySettings settings, CancellationToken cancellationToken)
            {
                var foundHosts = new Dictionary<string, LanDiscoveredHost>(StringComparer.OrdinalIgnoreCase);
                using (var udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;
                    udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

                    var queryBytes = Encoding.UTF8.GetBytes(DiscoverMessage);
                    await udp.SendAsync(queryBytes, queryBytes.Length, new IPEndPoint(IPAddress.Broadcast, settings.DiscoveryPort));

                    var timeout = Task.Delay(settings.DiscoveryTimeoutMs, cancellationToken);
                    while (!timeout.IsCompleted && !cancellationToken.IsCancellationRequested)
                    {
                        var receiveTask = udp.ReceiveAsync();
                        var completed = await Task.WhenAny(receiveTask, Task.Delay(100, cancellationToken), timeout);
                        if (completed != receiveTask) continue;

                        var parsed = TryParseResponse(receiveTask.Result.Buffer);
                        if (parsed == null) continue;
                        if (string.Equals(parsed.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)) continue;

                        foundHosts[parsed.DeduplicationKey] = parsed;
                    }
                }

                return foundHosts.Values.ToList();
            }
        }

        [DataContract]
        private class LanDiscoveryResponse
        {
            [DataMember(Name = "protocol")] public string Protocol { get; set; }
            [DataMember(Name = "machineName")] public string MachineName { get; set; }
            [DataMember(Name = "displayName")] public string DisplayName { get; set; }
            [DataMember(Name = "host")] public string Host { get; set; }
            [DataMember(Name = "apiPort")] public int ApiPort { get; set; }
            [DataMember(Name = "discoveryPort")] public int DiscoveryPort { get; set; }
            [DataMember(Name = "syncMode")] public string SyncMode { get; set; }
            [DataMember(Name = "requiresSharedKey")] public bool RequiresSharedKey { get; set; }

            public LanDiscoveredHost ToModel()
            {
                return new LanDiscoveredHost
                {
                    Protocol = Protocol,
                    MachineName = MachineName,
                    DisplayName = DisplayName,
                    Host = Host,
                    ApiPort = ApiPort,
                    DiscoveryPort = DiscoveryPort,
                    SyncMode = Tomighty.SyncMode.LanHost,
                    RequiresSharedKey = RequiresSharedKey
                };
            }
        }
    }
}
