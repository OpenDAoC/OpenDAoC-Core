using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace DOL.MPK
{
	/// <summary>
	/// Represents a file stored in an MPK archive.
	/// </summary>
	public class MpkFile
	{
		private byte[] _buf;
		private byte[] _compBuf;
		private MpkFileHeader _hdr = new MpkFileHeader();

		/// <summary>
		/// Constructs a new MPK file entry
		/// </summary>
		/// <param name="compData">The compressed data of this file entry</param>
		/// <param name="data">The uncompressed data of this file entry</param>
		/// <param name="hdr">The file entry header</param>
		public MpkFile(byte[] compData, byte[] data, MpkFileHeader hdr)
		{
			_compBuf = compData;
			_buf = data;
			_hdr = hdr;
		}

		/// <summary>
		/// Creates a new MPK file entry
		/// </summary>
		/// <param name="fname">The file name of the MPK file entry</param>
		public MpkFile(string fname)
		{
			Load(fname);
		}

		/// <summary>
		/// Gets the MPK header
		/// </summary>
		public MpkFileHeader Header
		{
			get { return _hdr; }
		}

		/// <summary>
		/// Gets the unencrypted Data in the MPK
		/// </summary>
		public byte[] Data
		{
			get
			{
				var buf = new byte[_buf.Length];
				Buffer.BlockCopy(_buf, 0, buf, 0, buf.Length);

				return buf;
			}
		}

		/// <summary>
		/// Gets the compressed data in the MPK
		/// </summary>
		public byte[] CompressedData
		{
			get
			{
				var buf = new byte[_hdr.CompressedSize];
				Buffer.BlockCopy(_compBuf, 0, buf, 0, buf.Length);

				return buf;
			}
		}

		/// <summary>
		/// Displays header information of this MPK file entry
		/// </summary>
		public void Display()
		{
			_hdr.Display();
		}

		/// <summary>
		/// Loads a MPK file
		/// </summary>
		/// <param name="fname">The filename to load</param>
		public void Load(string fname)
		{
			var fi = new FileInfo(fname);

			if (!fi.Exists)
			{
				throw new FileNotFoundException("File does not exist", fname);
			}

			using (FileStream file = fi.OpenRead())
			{
				_buf = new byte[fi.Length];
				file.Read(_buf, 0, _buf.Length);
			}

			_hdr = new MpkFileHeader { Name = fname, UncompressedSize = (uint)fi.Length, TimeStamp = (uint)DateTime.Now.ToFileTime() };

			var def = new Deflater();

			def.SetInput(_buf, 0, _buf.Length);
			def.Finish();

			// create temporary buffer
			var tempbuffer = new byte[_buf.Length + _buf.Length / 5 + 3];
			_hdr.CompressedSize = (uint)def.Deflate(tempbuffer, 0, tempbuffer.Length);

			_compBuf = new byte[_hdr.CompressedSize];
			Buffer.BlockCopy(tempbuffer, 0, _compBuf, 0, (int)_hdr.CompressedSize);


			var crc = new Crc32();
			crc.Update(new ArraySegment<byte>(_compBuf, 0, (int)_hdr.CompressedSize));

			_hdr.CRCValue = crc.Value;
		}

		/// <summary>
		/// Saves an MPK file
		/// </summary>
		/// <param name="dir">The directory where to save the file</param>
		public void Save(string dir)
		{
			if (!dir.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				dir += Path.DirectorySeparatorChar;
			}

			using (var writer = new BinaryWriter(File.Create(dir + _hdr.Name), Encoding.UTF8))
			{
				writer.Write(_buf);
			}
		}
	}
}