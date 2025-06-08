using System;
using System.Buffers;
using System.IO;

namespace DOL.Network
{
	/// <summary>
	/// Reads primitive data types from an underlying stream.
	/// </summary>
	public abstract class PacketIn : MemoryStream, IPacket
	{
		protected PacketIn() { }

		protected PacketIn(int size) : base(size) { }

		public virtual void Init() { }

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
			// Stack for small strings, ArrayPool for large strings.
			if (maxlen <= 1024)
			{
				Span<byte> buffer = stackalloc byte[maxlen];
				Read(buffer);
				int actualLength = buffer.IndexOf((byte) 0);

				if (actualLength == -1)
					actualLength = maxlen;

				return BaseServer.defaultEncoding.GetString(buffer[..actualLength]);
			}
			else
			{
				byte[] buffer = ArrayPool<byte>.Shared.Rent(maxlen);

				try
				{
					int actualLength = Array.IndexOf(buffer, (byte) 0, 0, maxlen);

					if (actualLength == -1)
						actualLength = maxlen;

					Read(buffer, 0, maxlen);
					return BaseServer.defaultEncoding.GetString(buffer, 0, actualLength);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(buffer);
				}
			}
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
			uint v = (uint) ((byte) ReadByte() | ((byte) ReadByte() << 8) | ((byte) ReadByte() << 16) | ((byte) ReadByte() << 24));
			return BitConverter.UInt32BitsToSingle(v);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>		
		public override string ToString()
		{
			return GetType().Name;
		}

		public override void Close()
		{
			// Called by Dispose and normally invalidates the stream.
			// But this is both pointless (`MemoryStream` doesn't have any unmanaged resource) and undesirable (we always want the buffer to remain accessible)
		}
	}
}
