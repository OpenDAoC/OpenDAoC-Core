using System.Reflection;
using DOL.Logging;

namespace DOL.GS.PacketHandler
{
    // How a connection's packets are encrypted, chosen once at the version reply (`GameClient.Encryption`).
    // Implementations are stateless singletons: the per-connection state is the client's `PacketProcessor.Encoding`.
    public interface IClientEncryption
    {
        // Called for each complete inbound TCP packet, before its checksum is checked. Decrypts it in place if needed.
        // Returns false if the packet belonged to the handshake and must not be handled.
        bool DecryptInbound(GameClient client, byte[] buffer, int offset, int length);

        // Encrypts an outbound TCP or UDP frame in place if needed.
        void EncryptOutbound(GameClient client, byte[] buffer, int offset, bool udp);

        // Logs a likely reason after an inbound packet failed its checksum.
        void LogChecksumFailure(GameClient client);
    }

    // Plaintext both ways. The default for every client.
    public sealed class NoClientEncryption : IClientEncryption
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly NoClientEncryption Instance = new();

        private NoClientEncryption() { }

        public bool DecryptInbound(GameClient client, byte[] buffer, int offset, int length) => true;

        public void EncryptOutbound(GameClient client, byte[] buffer, int offset, bool udp) { }

        public void LogChecksumFailure(GameClient client)
        {
            // An unpatched 1.127 or 1.130 client starts encrypting after the version reply. Without RSA encryption, its
            // first encrypted packet fails the checksum. Clients patched to run unencrypted are unaffected.
            if (RsaKeyTransport.AppliesTo(true, client.Version) && log.IsWarnEnabled)
                log.Warn($"{client.TcpEndpointAddress} is a {client.Version} client that encrypts, but RSA encryption is off " +
                    "(client_enable_encryption_rsa, client_rsa_private_key_file; see docs/client-encryption-rsa.md)");
        }
    }
}
