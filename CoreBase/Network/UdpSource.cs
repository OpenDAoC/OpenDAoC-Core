using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace DOL.Network
{
    public readonly struct UdpSource
    {
        private const int IPV4_PORT_OFFSET = 2;
        private const int IPV4_ADDRESS_OFFSET = 4;
        private const int IPV4_MIN_SIZE = 8;

        private readonly uint _networkAddress;

        public ushort Port { get; }

        public UdpSource(uint networkAddress, ushort port)
        {
            _networkAddress = networkAddress;
            Port = port;
        }

        public static bool TryParse(SocketAddress address, out UdpSource source)
        {
            source = default;

            if (address == null || address.Family is not AddressFamily.InterNetwork || address.Size < IPV4_MIN_SIZE)
                return false;

            ReadOnlySpan<byte> buffer = address.Buffer.Span;

            if (buffer.Length < IPV4_MIN_SIZE)
                return false;

            ushort port = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(IPV4_PORT_OFFSET, sizeof(ushort)));
            uint networkAddress = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(IPV4_ADDRESS_OFFSET, sizeof(uint)));

            source = new(networkAddress, port);
            return true;
        }

        public IPEndPoint ToIPEndPoint()
        {
            return new(ToIPAddress(), Port);
        }

        public bool Matches(IPAddress address)
        {
            if (address == null || address.AddressFamily is not AddressFamily.InterNetwork)
                return false;

            Span<byte> bytes = stackalloc byte[sizeof(uint)];

            if (!address.TryWriteBytes(bytes, out int bytesWritten) || bytesWritten != bytes.Length)
                return false;

            return BinaryPrimitives.ReadUInt32BigEndian(bytes) == _networkAddress;
        }

        public bool Matches(IPEndPoint endPoint)
        {
            return endPoint != null && endPoint.Port == Port && Matches(endPoint.Address);
        }

        public override string ToString()
        {
            return $"{ToIPAddress()}:{Port}";
        }

        private IPAddress ToIPAddress()
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, _networkAddress);
            return new(bytes);
        }
    }
}
