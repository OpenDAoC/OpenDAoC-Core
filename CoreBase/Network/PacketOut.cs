using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace DOL.Network
{
	/// <summary>
	/// Writes primitives data types to an underlying stream.
	/// </summary>
	public abstract class PacketOut : MemoryStream, IPacket
	{
		public byte Code { get; protected set; }
		public bool IsSizeSet { get; private set; }

		public virtual PacketOut Init(byte code)
		{
			Code = code;
			IsSizeSet = false;
			return this;
		}

		#region IPacket Members

		/// <summary>
		/// Generates a human-readable dump of the packet contents.
		/// </summary>
		/// <returns>a string representing the packet contents in hexadecimal</returns>
		public string ToHumanReadable()
		{
			return Marshal.ToHexDump(ToString(), ToArray());
		}

		#endregion

		/// <summary>
		/// Writes a 2 byte (short) value to the stream in network byte order
		/// </summary>
		/// <param name="val">Value to write</param>
		public void WriteShort(ushort val)
		{
			WriteByte((byte) (val >> 8));
			WriteByte((byte) (val & 0xff));
		}

		/// <summary>
		/// Writes a 2 byte (short) value to the stream in host byte order
		/// </summary>
		/// <param name="val">Value to write</param>
		public void WriteShortLowEndian(ushort val)
		{
			WriteByte((byte) (val & 0xff));
			WriteByte((byte) (val >> 8));
		}

		/// <summary>
		/// Writes a 4 byte value to the stream in host byte order
		/// </summary>
		/// <param name="val">Value to write</param>
		public void WriteInt(uint val)
		{
			WriteByte((byte) (val >> 24));
			WriteByte((byte) ((val >> 16) & 0xff));
			WriteByte((byte) ((val & 0xffff) >> 8));
			WriteByte((byte) ((val & 0xffff) & 0xff));
		}

		/// <summary>
		/// Writes a 4 byte value to the stream in host byte order
		/// </summary>
		/// <param name="val">Value to write</param>
		public void WriteIntLowEndian(uint val)
		{
			WriteByte((byte) ((val & 0xffff) & 0xff));
			WriteByte((byte) ((val & 0xffff) >> 8));
			WriteByte((byte) ((val >> 16) & 0xff));
			WriteByte((byte) (val >> 24));
		}

		/// <summary>
		/// Writes a 8 byte value to the stream in host byte order
		/// </summary>
		/// <param name="val">Value to write</param>
		public void WriteLong(ulong val)
		{
			WriteByte((byte) (val >> 56));
			WriteByte((byte) ((val >> 48) & 0xff));
			WriteByte((byte) ((val >> 40) & 0xff));
			WriteByte((byte) ((val >> 32) & 0xff));
			WriteByte((byte) ((val >> 24) & 0xff));
			WriteByte((byte) ((val >> 16) & 0xff));
			WriteByte((byte) ((val >> 8) & 0xff));
			WriteByte((byte) (val & 0xff));
		}

		/// <summary>
		/// Writes a 8 byte value to the stream in host byte order
		/// </summary>
		/// <param name="val">Value to write</param>
		public void WriteLongLowEndian(ulong val)
		{
			WriteByte((byte) (val & 0xff));
			WriteByte((byte) ((val >> 8) & 0xff));
			WriteByte((byte) ((val >> 16) & 0xff));
			WriteByte((byte) ((val >> 24) & 0xff));
			WriteByte((byte) ((val >> 32) & 0xff));
			WriteByte((byte) ((val >> 40) & 0xff));
			WriteByte((byte) ((val >> 48) & 0xff));
			WriteByte((byte) (val >> 56));
		}

		/// <summary>
		/// writes a float value to low endian used in 1.124 packets
		/// </summary>
		public void WriteFloatLowEndian(float val)
		{
			uint intValue = BitConverter.SingleToUInt32Bits(val);
			WriteByte((byte) (intValue & 0xFF));
			WriteByte((byte) ((intValue >> 8) & 0xFF));
			WriteByte((byte) ((intValue >> 16) & 0xFF));
			WriteByte((byte) ((intValue >> 24) & 0xFF));
		}
		
		/// <summary>
		/// Calculates the checksum for the internal buffer
		/// </summary>
		/// <returns>The checksum of the internal buffer</returns>
		public virtual byte GetChecksum()
		{
			byte val = 0;
			byte[] buf = GetBuffer();

			for (int i = 0; i < Position - 6; ++i)
			{
				val += buf[i + 8];
			}

			return val;
		}

		/// <summary>
		/// Writes the supplied value to the stream for a specified number of bytes
		/// </summary>
		/// <param name="val">Value to write</param>
		/// <param name="num">Number of bytes to write</param>
		public void Fill(byte val, int num)
		{
			for (int i = 0; i < num; ++i)
			{
				WriteByte(val);
			}
		}

		/// <summary>
		/// Writes the length of the packet at the beginning of the stream
		/// </summary>
		/// <returns>Length of the packet</returns>
		public virtual void WritePacketLength()
		{
			// Intended to be overridden.
			throw new NotImplementedException();
		}

		protected void OnStartWritePacketLength()
		{
			IsSizeSet = true;
			Position = 0;
		}

		/// <summary>
		/// Writes a Pascal-style string to the stream
		/// </summary>
		public void WritePascalString(ReadOnlySpan<char> chars)
		{
			if (chars.IsEmpty)
			{
				WriteByte(0);
				return;
			}

			int byteCount = BaseServer.DefaultEncoding.GetByteCount(chars);

			if (byteCount > byte.MaxValue)
				throw new ArgumentException($"Pascal string exceeds maximum length of 255 bytes. Actual length: {byteCount} bytes", nameof(chars));

			WriteByte((byte) byteCount);

			// Stack for small buffers, ArrayPool for large buffers.
			if (byteCount <= 1024)
			{
				Span<byte> buffer = stackalloc byte[byteCount];
				BaseServer.DefaultEncoding.GetBytes(chars, buffer);
				Write(buffer);
			}
			else
			{
				byte[] buffer = ArrayPool<byte>.Shared.Rent(byteCount);

				try
				{
					int written = BaseServer.DefaultEncoding.GetBytes(chars, buffer);
					Write(new ReadOnlySpan<byte>(buffer, 0, written));
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(buffer);
				}
			}
		}

		public void WritePascalStringIntLE(ReadOnlySpan<char> chars)
		{
			if (chars.IsEmpty)
			{
				WriteIntLowEndian(0);
				return;
			}

			int byteCount = BaseServer.DefaultEncoding.GetByteCount(chars);
			WriteIntLowEndian((uint) byteCount + 1);
			WriteNonNullTerminatedString(chars);
			WriteByte(0);
		}

		/// <summary>
		/// Writes a C-style string to the stream.
		/// </summary>
		public void WriteString(ReadOnlySpan<char> chars)
		{
			WriteNonNullTerminatedString(chars);
			WriteByte(0x0);
		}

		public void WriteNonNullTerminatedString(ReadOnlySpan<char> chars)
		{
			WriteString(chars, int.MaxValue);
		}

		/// <summary>
		/// Writes up to maxByteLen bytes to the stream from the supplied character span.
		/// </summary>
		public void WriteString(ReadOnlySpan<char> chars, int maxByteLen)
		{
			if (chars.IsEmpty || maxByteLen <= 0)
				return;

			int maxByteCount = BaseServer.DefaultEncoding.GetMaxByteCount(chars.Length);
			int bufferSize = Math.Min(maxByteCount, maxByteLen);

			// Stack for small buffers, ArrayPool for large buffers.
			if (bufferSize <= 1024)
			{
				Span<byte> buffer = stackalloc byte[bufferSize];
				Encoder encoder = BaseServer.GetEncoder();
				encoder.Reset();
				encoder.Convert(chars, buffer, true, out _, out int bytesUsed, out _);
				Write(buffer[..bytesUsed]);
			}
			else
			{
				byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

				try
				{
					Encoder encoder = BaseServer.GetEncoder();
					encoder.Reset();
					encoder.Convert(chars, buffer, true, out _, out int bytesUsed, out _);
					Write(new ReadOnlySpan<byte>(buffer, 0, bytesUsed));
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(buffer);
				}
			}
		}

		/// <summary>
		/// Writes a fixed-length, null-padded string to the stream.
		/// The string is truncated if its byte representation exceeds the specified length.
		/// </summary>
		public void FillString(string str, int len)
		{
			long pos = Position;
			Fill(0x0, len);
			Position = pos;
			WriteString(str, len);
			Position = pos + len;
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
