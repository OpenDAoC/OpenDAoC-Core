using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace DOL.Network.Tests
{
    [TestFixture]
    public class UT_UdpSource
    {
        [Test]
        public void TryParse_IPv4SocketAddress_RoundTripsAddressAndPort()
        {
            SocketAddress socketAddress = new(AddressFamily.InterNetwork);
            System.Span<byte> buffer = socketAddress.Buffer.Span;
            buffer[2] = 0x1F; // Port 8080, network byte order.
            buffer[3] = 0x90;
            buffer[4] = 192;
            buffer[5] = 168;
            buffer[6] = 1;
            buffer[7] = 42;

            Assert.That(UdpSource.TryParse(socketAddress, out UdpSource source), Is.True);
            Assert.That(source.Port, Is.EqualTo(8080));
            Assert.That(source.Matches(IPAddress.Parse("192.168.1.42")), Is.True);
            Assert.That(source.Matches(IPAddress.Parse("192.168.1.43")), Is.False);

            IPEndPoint endPoint = source.ToIPEndPoint();
            Assert.That(endPoint.Address, Is.EqualTo(IPAddress.Parse("192.168.1.42")));
            Assert.That(endPoint.Port, Is.EqualTo(8080));
        }

        [Test]
        public void Matches_IPEndPoint_RequiresAddressAndPort()
        {
            UdpSource source = new(0xC0A8012A, 8080); // 192.168.1.42

            Assert.That(source.Matches(new IPEndPoint(IPAddress.Parse("192.168.1.42"), 8080)), Is.True);
            Assert.That(source.Matches(new IPEndPoint(IPAddress.Parse("192.168.1.42"), 8081)), Is.False);
            Assert.That(source.Matches(new IPEndPoint(IPAddress.Parse("192.168.1.43"), 8080)), Is.False);
            Assert.That(source.Matches((IPEndPoint) null), Is.False);
        }

        [Test]
        public void TryParse_IPv6SocketAddress_ReturnsFalse()
        {
            SocketAddress socketAddress = new(AddressFamily.InterNetworkV6);

            Assert.That(UdpSource.TryParse(socketAddress, out _), Is.False);
        }

        [Test]
        public void Matches_NonIPv4Address_ReturnsFalse()
        {
            UdpSource source = new(0xC0A8012A, 8080); // 192.168.1.42

            Assert.That(source.Matches(IPAddress.IPv6Loopback), Is.False);
            Assert.That(source.Matches((IPAddress) null), Is.False);
        }
    }
}
