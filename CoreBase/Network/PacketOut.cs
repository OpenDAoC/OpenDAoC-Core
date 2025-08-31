using System;
using System.Buffers;
using System.IO;

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
		/// Writes a pascal style string to the stream
		/// </summary>
		/// <param name="str">String to write</param>
		public void WritePascalString(string str)
		{
			if (str == null || str.Length <= 0)
			{
				WriteByte(0);
				return;
			}

			byte[] bytes = BaseServer.defaultEncoding.GetBytes(str);
			WriteByte((byte) bytes.Length);
			Write(bytes, 0, bytes.Length);
		}

		public void WritePascalStringIntLE(string str)
		{
			if (str == null || str.Length <= 0)
			{
				WriteIntLowEndian(0);
				return;
			}

			byte[] bytes = BaseServer.defaultEncoding.GetBytes(str);
			WriteIntLowEndian((uint)bytes.Length + 1);
			Write(bytes, 0, bytes.Length);
			WriteByte(0);
		}

		/// <summary>
		/// Writes a C-style string to the stream
		/// </summary>
		/// <param name="str">String to write</param>
		public void WriteString(string str)
		{
			WriteStringBytes(str);
			WriteByte(0x0);
		}

		public void WriteNonNullTerminatedString(string str)
		{
			WriteStringBytes(str);
		}

		/// <summary>
		/// Writes exactly the bytes from the string without any trailing 0
		/// </summary>
		/// <param name="str">the string to write</param>
		public void WriteStringBytes(string str)
		{
			WriteString(str, int.MaxValue);
		}

		/// <summary>
		/// Writes up to maxlen bytes to the stream from the supplied string
		/// </summary>
		/// <param name="str">String to write</param>
		/// <param name="maxByteLen">Maximum number of bytes to be written</param>
		public void WriteString(string str, int maxByteLen)
		{
			if (string.IsNullOrEmpty(str) || maxByteLen <= 0)
				return;

			int maxByteCount = BaseServer.defaultEncoding.GetMaxByteCount(str.Length);
			int bufferSize = Math.Min(maxByteCount, maxByteLen);

			// Stack for small buffers, ArrayPool for large buffers.
			if (bufferSize <= 1024)
			{
				Span<byte> buffer = stackalloc byte[bufferSize];
				int bytesWritten = BaseServer.defaultEncoding.GetBytes(str, buffer);
				Write(buffer[..bytesWritten]);
			}
			else
			{
				byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

				try
				{
					int bytesWritten = BaseServer.defaultEncoding.GetBytes(str, buffer.AsSpan(0, bufferSize));
					Write(buffer, 0, bytesWritten);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(buffer);
				}
			}
		}

		/// <summary>
		/// Writes len number of bytes from str to the stream
		/// </summary>
		/// <param name="str">String to write</param>
		/// <param name="len">Number of bytes to write</param>
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
