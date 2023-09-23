using System;
using System.IO;

namespace DOL.Network
{
	/// <summary>
	/// Reads primitive data types from an underlying stream.
	/// </summary>
	public class PacketIn : MemoryStream, IPacket
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="size">Size of the internal buffer</param>
		public PacketIn(int size) : base(size)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buf">Buffer containing packet data to read from</param>
		/// <param name="start">Starting index into buf</param>
		/// <param name="size">Number of bytes to read from buf</param>
		public PacketIn(byte[] buf, int start, int size) : base(buf, start, size)
		{
		}

		/// <summary>
		/// Generates a human-readable dump of the packet contents.
		/// </summary>
		/// <returns>a string representing the packet contents in hexadecimal</returns>
		public string ToHumanReadable()
		{
			return Marshal.ToHexDump(ToString(), ToArray());
		}

		/// <summary>
		/// Reads in 2 bytes and converts it from network to host byte order
		/// </summary>
		/// <returns>A 2 byte (short) value</returns>
		public ushort ReadShort()
		{
			var v1 = (byte) ReadByte();
			var v2 = (byte) ReadByte();

			return Marshal.ConvertToUInt16(v1, v2);
		}

		/// <summary>
		/// Reads in 2 bytes
		/// </summary>
		/// <returns>A 2 byte (short) value in network byte order</returns>
		public ushort ReadShortLowEndian()
		{
			var v1 = (byte) ReadByte();
			var v2 = (byte) ReadByte();

			return Marshal.ConvertToUInt16(v2, v1);
		}

		/// <summary>
		/// Reads in 4 bytes and converts it from network to host byte order
		/// </summary>
		/// <returns>A 4 byte value</returns>
		public uint ReadInt()
		{
			var v1 = (byte) ReadByte();
			var v2 = (byte) ReadByte();
			var v3 = (byte) ReadByte();
			var v4 = (byte) ReadByte();

			return Marshal.ConvertToUInt32(v1, v2, v3, v4);
		}

		/// <summary>
		/// Skips 'num' bytes ahead in the stream
		/// </summary>
		/// <param name="num">Number of bytes to skip ahead</param>
		public void Skip(long num)
		{
			Seek(num, SeekOrigin.Current);
		}

		/// <summary>
		/// Reads a null-terminated string from the stream
		/// </summary>
		/// <param name="maxlen">Maximum number of bytes to read in</param>
		/// <returns>A string of maxlen or less</returns>
		public string ReadString(int maxlen)
		{
			var buf = new byte[maxlen];
			Read(buf, 0, maxlen);

			return Marshal.ConvertToString(buf);
		}

		/// <summary>
		/// Reads in a pascal style string
		/// </summary>
		/// <returns>A string from the stream</returns>
		public string ReadPascalString()
		{
			return ReadString(ReadByte());
		}

		/// <summary>
		/// Reads in a pascal style string, with header count formatted as a Low Endian Short.
		/// </summary>
		/// <returns>A string from the stream</returns>
		public string ReadShortPascalStringLowEndian()
		{
			return ReadString(ReadShortLowEndian());
		}

		public string ReadIntPascalStringLowEndian()
		{
			return ReadString((int)ReadIntLowEndian());
		}

		public uint ReadIntLowEndian()
		{
			var v1 = (byte) ReadByte();
			var v2 = (byte) ReadByte();
			var v3 = (byte) ReadByte();
			var v4 = (byte) ReadByte();

			return Marshal.ConvertToUInt32(v4, v3, v2, v1);
		}
		
		/// <summary>
		/// Reads low endian floats used in 1.124 packets
		/// </summary>
		/// <returns>converts it to a usable value</returns>
		public float ReadFloatLowEndian()
        {
            var v1 = (byte)ReadByte();
            var v2 = (byte)ReadByte();
            var v3 = (byte)ReadByte();
            var v4 = (byte)ReadByte();            
            byte[] bytes = new[] { v1, v2, v3, v4 };
            float data = BitConverter.ToSingle(bytes, 0);
            return data;
        } 
		
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>		
		public override string ToString()
		{
			return GetType().Name;
		}
	}
}
