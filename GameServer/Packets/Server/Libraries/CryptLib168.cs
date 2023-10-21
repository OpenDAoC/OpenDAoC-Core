using System;
using System.Runtime.InteropServices;

namespace DOL.GS
{
	public class CryptLib168
	{
		[DllImport("CryptLib168.dll", CharSet=CharSet.Ansi)]
		public static extern bool GenerateRSAKey();

		[DllImport("CryptLib168.dll", CharSet=CharSet.Ansi)]
		public static extern bool ImportRSAKey(Byte[] externalKey, UInt32 keyLen);

		[DllImport("CryptLib168.dll", CharSet=CharSet.Ansi)]
		public static extern UInt32 ExportRSAKey(Byte[] key, UInt32 maxKeySize, bool withPrivateKey);

		[DllImport("CryptLib168.dll", CharSet=CharSet.Ansi)]
		public static extern UInt32 EncodeMythicRSAPacket(Byte[] inMessage, UInt32 inMessageLen, Byte[] outMessage, UInt32 outMessageLen);

		[DllImport("CryptLib168.dll", CharSet=CharSet.Ansi)]
		public static extern UInt32 DecodeMythicRSAPacket(Byte[] inMessage, UInt32 inMessageLen, Byte[] outMessage, UInt32 outMessageLen);
		
		[DllImport("CryptLib168.dll", CharSet=CharSet.Ansi)]
		public static extern void EncodeMythicRC4Packet(Byte[] packet, Byte[] sbox, bool udpPacket);

		[DllImport("CryptLib168.dll", CharSet=CharSet.Ansi)]
		public static extern void DecodeMythicRC4Packet(Byte[] packet, Byte[] sbox);
	}
 
}

