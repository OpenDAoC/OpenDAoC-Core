# DAoC 1.130 game client encryption

OpenDAoC can accept the RSA key transport and subsequent RC4 packet encryption used by the
1.130 `game.dll`. Support is disabled by default. It applies to protocol version 1.130; older
client versions keep their existing handshake and packet handling.

## Key and client setup

Generate a dedicated 1024-bit RSA key pair for the game connection. The client key slot used by
this protocol limits the modulus to 128 bytes. The server reads the private key as a PEM file
through .NET `RSA.ImportFromPem`; the command below produces PKCS#8 PEM:

```sh
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:1024 \
  -pkeyopt rsa_keygen_pubexp:65537 -out game-rsa-private.pem
```

Keep the private PEM on the server and outside source control. Configure
`client_rsa_private_key_file` to point to it; the default is
`config/game-rsa-private.pem` relative to the server root. Set
`client_enable_1130_crypto` to `true` through the usual server-property configuration and
restart the server. Startup fails if encryption is enabled and the private key cannot be loaded.

With 1.130 crypto on, the server accepts both kinds of 1.130 client, decided per connection by
the client's first packet after the version exchange: a valid RSA key transport starts an
encrypted session; otherwise the server continues in plaintext, so a client patched to run without
encryption still connects. A packet that is neither (for example from a client whose `game.dll`
embeds a different public key) fails the packet checksum, and the server disconnects and logs why.

With 1.130 crypto off, a 1.130 client that encrypts is disconnected after the version exchange,
and the server logs why. A 1.130 client patched to run without encryption is handled like any
other plaintext client.

The client must have the **matching public key embedded in `game.dll`**. The 1.130 client expects
a LibTomCrypt public-key blob in its key slot; copying a PEM file alongside the client does not
change that key. A stock `game.dll` with its original public key will not work with a newly
generated private key. This server change does not patch or distribute client binaries. If the
login flow uses `login.dll`, its separate key setup is outside the scope of this game-server
transport feature.

The server decrypts the client's RSA key transport using the game's OAEP variant, then uses the
recovered RC4 session key for every packet it sends afterwards: TCP packets, UDP packets sent
over TCP, and UDP datagrams. In a UDP frame the size stays in clear; the counter, code and payload
are encrypted, as the client expects. The client's own packets are encrypted over TCP only; its
UDP datagrams stay unencrypted.
