using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Core.Base;

/// <summary>
/// Handles the reading and writing to MPK files.
/// </summary>
public class MpkHandler
{
	/// <summary>
	/// The magic at the top of the file
	/// </summary>
	private const uint Magic = 0x4b41504d; //MPAK

	/// <summary>
	/// Holds all of the files in the MPK
	/// </summary>
	private readonly Dictionary<string, MpkFile> _files = new Dictionary<string, MpkFile>();

	/// <summary>
	/// Name of the archive
	/// </summary>
	private string _name = "";

	/// <summary>
	/// Number of files in the directory
	/// </summary>
	private int _numFiles;

	/// <summary>
	/// Compressed size of the directory section
	/// </summary>
	private int _sizeDir;

	/// <summary>
	/// Compressed size of the name section
	/// </summary>
	private int _sizeName;

	/// <summary>
	/// Creates a new MPK file
	/// </summary>
	/// <param name="fname">The filename</param>
	/// <param name="create">if true, creates the file, else parses an existing file</param>
	public MpkHandler(string fname, bool create)
	{
		if (!create)
		{
			Read(fname);
		}
		else
		{
			_name = fname;
		}
	}

	/// <summary>
	/// Creates a new MPK file
	/// </summary>
	public MpkHandler()
	{
	}

	/// <summary>
	/// The name of this file
	/// </summary>
	public string Name
	{
		get { return _name; }
		set { _name = value; }
	}

	public long CRCValue { get; private set; }

	/// <summary>
	/// The directory size of this MPK
	/// </summary>
	public int DirectorySize
	{
		get { return _sizeDir; }
	}

	/// <summary>
	/// The filecount in this MPK
	/// </summary>
	public int Count
	{
		get { return _files.Count; }
	}

	/// <summary>
	/// Gets a specific MPK file from this MPK
	/// </summary>
	public MpkFile this[string fname]
	{
		get
		{
			if (_files.ContainsKey(fname))
			{
				return _files[fname];
			}

			return null;
		}
	}

	/// <summary>
	/// The event to fire if an invalid file was found
	/// </summary>
	public event EventHandler InvalidFile;

	/// <summary>
	/// Gets a list of all the files inside this MPK
	/// </summary>
	/// <returns>An IDictionaryEnumerator containing entries as filename, MPKFile pairs</returns>
	public IEnumerator<KeyValuePair<string, MpkFile>> GetEnumerator()
	{
		return _files.GetEnumerator();
	}

	/// <summary>
	/// Adds a file to the MPK
	/// </summary>
	/// <param name="file">The file to add</param>
	/// <returns>true if successfull, false if the file is already contained</returns>
	public bool AddFile(MpkFile file)
	{
		if (!_files.ContainsKey(file.Header.Name))
		{
			_files.Add(file.Header.Name, file);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Removes a file from the MPK
	/// </summary>
	/// <param name="fname">The file to remove</param>
	/// <returns>true if the file was successfully removed, false if it wasn't in the MPK</returns>
	public bool RemoveFile(string fname)
	{
		if (_files.ContainsKey(fname.ToLower()))
		{
			_files.Remove(fname.ToLower());
			return true;
		}

		return false;
	}

	/// <summary>
	/// Removes a file from the MPK
	/// </summary>
	/// <param name="file">The file to remove</param>
	/// <returns>true if the file was successfully removed, false if it wasn't in the MPK</returns>
	public bool RemoveFile(MpkFile file)
	{
		return RemoveFile(file.Header.Name);
	}

	/// <summary>
	/// Saves the MPK
	/// </summary>
	public void Save()
	{
		Write(Name);
	}

	/// <summary>
	/// Writes the MPK to a specific filename
	/// </summary>
	/// <param name="fname"></param>
	public void Write(string fname)
	{
		Deflater def;
		byte[] buf, dir, name;
		var files = new MpkFile[_files.Count];

		using (var dirmem = new MemoryStream(_files.Count*MpkFileHeader.MaxSize))
		{
			int index = 0;
			uint offset = 0;
			uint diroffset = 0;

			foreach (MpkFile file in _files.Values)
			{
				file.Header.DirectoryOffset = diroffset;
				file.Header.Offset = offset;
				files[index] = file;

				using (var wrtr = new BinaryWriter(dirmem, Encoding.UTF8, true))
				{
					file.Header.Write(wrtr);
				}

				offset += file.Header.UncompressedSize;
				diroffset += file.Header.CompressedSize;
				index++;
			}

			def = new Deflater();
			def.SetInput(dirmem.GetBuffer(), 0, (int) dirmem.Position);
			def.Finish();

			dir = new byte[dirmem.Position];
			def.Deflate(dir);
			_sizeDir = (int) def.TotalOut;
		}

		def = new Deflater();

		var _crc = new Crc32();
		_crc.Update(new ArraySegment<byte>(dir, 0, _sizeDir));
		CRCValue = _crc.Value;

		def.SetInput(Encoding.UTF8.GetBytes(_name));
		def.Finish();

		name = new byte[_name.Length];
		def.Deflate(name);

		_numFiles = _files.Count;
		_sizeName = (int) def.TotalOut;

		using (var filemem = new MemoryStream())
		{
			using (var wrtr = new BinaryWriter(filemem, Encoding.UTF8))
			{
				wrtr.Write((uint) CRCValue);
				wrtr.Write(_sizeDir);
				wrtr.Write(_sizeName);
				wrtr.Write(_numFiles);

				buf = new byte[16];
				Buffer.BlockCopy(filemem.GetBuffer(), 0, buf, 0, 16);

				for (byte i = 0; i < 16; i++)
				{
					buf[i] ^= i;
				}
			}
		}

		using (FileStream fileStream = File.Open(fname, FileMode.Create, FileAccess.Write))
		{
			using (var wrtr = new BinaryWriter(fileStream, Encoding.UTF8))
			{
				wrtr.Write(Magic);
				wrtr.Write((byte) 2);
				wrtr.Write(buf, 0, 16);
				wrtr.Write(name, 0, _sizeName);
				wrtr.Write(dir, 0, _sizeDir);

				foreach (MpkFile file in files)
				{
					wrtr.Write(file.CompressedData);
				}
			}
		}
	}

	/// <summary>
	/// Extracts all files from this MPK into a directory
	/// </summary>
	/// <param name="dirname">The directory where to put the files</param>
	/// <param name="fname">The MPK file to extract</param>
	public void Extract(string dirname, string fname)
	{
		Read(fname);
		Extract(dirname);
	}

	/// <summary>
	/// Extracts all files from this MPK
	/// </summary>
	/// <param name="dirname">The directory where to put the files</param>
	public void Extract(string dirname)
	{
		if (!Directory.Exists(dirname))
		{
			Directory.CreateDirectory(dirname);
		}

		if (!dirname.EndsWith(Path.DirectorySeparatorChar.ToString()))
		{
			dirname += Path.DirectorySeparatorChar;
		}

		foreach (MpkFile file in _files.Values)
		{
			file.Save(dirname);
		}
	}

	/// <summary>
	/// Reads a MPK file
	/// </summary>
	/// <param name="fname">The MPK filename to read</param>
	public void Read(string fname)
	{
		using (var rdr = new BinaryReader(File.OpenRead(fname), Encoding.UTF8))
		{
			if (rdr.ReadUInt32() != Magic)
			{
				throw new Exception("Invalid MPK file");
			}

			rdr.ReadByte(); //always 2

			ReadArchive(rdr);
		}
	}

	/// <summary>
	/// Reads a MPK from a binary reader
	/// </summary>
	/// <param name="rdr">The binary reader pointing to the MPK</param>
	private void ReadArchive(BinaryReader rdr)
	{
		_files.Clear();

		_sizeDir = 0;
		_sizeName = 0;
		_numFiles = 0;

		var buf = new byte[16];
		rdr.Read(buf, 0, 16);

		for (byte i = 0; i < 16; ++i)
		{
			buf[i] ^= i;
		}

		CRCValue = ((uint)buf[3] << 24) + (buf[2] << 16) + (buf[1] << 8) + buf[0];
		_sizeDir = (buf[7] << 24) + (buf[6] << 16) + (buf[5] << 8) + buf[4];
		_sizeName = (buf[11] << 24) + (buf[10] << 16) + (buf[9] << 8) + buf[8];
		_numFiles = (buf[15] << 24) + (buf[14] << 16) + (buf[13] << 8) + buf[12];

		buf = new byte[_sizeName];
		rdr.Read(buf, 0, _sizeName);

		var inf = new Inflater();
		inf.SetInput(buf);
		buf = new byte[1024];
		inf.Inflate(buf);
		buf[inf.TotalOut] = 0;

		_name = Marshal.ConvertToString(buf);

		long totalin = 0;
		buf = ReadDirectory(rdr, ref totalin);

		using (var directory = new MemoryStream(buf))
		{
			long pos = rdr.BaseStream.Position;
			long len = rdr.BaseStream.Seek(0, SeekOrigin.End);
			rdr.BaseStream.Position = pos;

			buf = new byte[len - pos];
			rdr.Read(buf, 0, buf.Length);

			using (var files = new MemoryStream(buf))
			{
				rdr.BaseStream.Position = pos - totalin;
				buf = new byte[totalin];
				rdr.Read(buf, 0, buf.Length);

				var crc = new Crc32();
				crc.Reset();
				crc.Update(buf);

				if (crc.Value != CRCValue)
				{
					throw new Exception("Invalid or corrupt MPK");
				}

				while (directory.Position < directory.Length && files.Position < files.Length)
				{
					crc.Reset();

					buf = new byte[MpkFileHeader.MaxSize];
					directory.Read(buf, 0, MpkFileHeader.MaxSize);

					MpkFileHeader hdr;

					using (var hdrStream = new MemoryStream(buf))
					{
						using (var hdrRdr = new BinaryReader(hdrStream, Encoding.UTF8))
						{
							hdr = new MpkFileHeader(hdrRdr);
						}
					}

					var compbuf = new byte[hdr.CompressedSize];
					files.Read(compbuf, 0, compbuf.Length);

					crc.Update(compbuf);

					inf.Reset();
					inf.SetInput(compbuf, 0, compbuf.Length);
					buf = new byte[hdr.UncompressedSize];
					inf.Inflate(buf, 0, buf.Length);

					var file = new MpkFile(compbuf, buf, hdr);

					if (crc.Value != hdr.CRCValue)
					{
						OnInvalidFile(file);
						continue;
					}

					_files.Add(hdr.Name.ToLower(), file);
				}
			}
		}
	}

	private static byte[] ReadDirectory(BinaryReader rdr, ref long totalin)
	{
		int totalout = 0;
		var input = new byte[1024];
		var output = new byte[1024];
		var inf = new Inflater();
		totalin = 0;

		while (!inf.IsFinished)
		{
			while (inf.IsNeedingInput)
			{
				int count;
				if ((count = rdr.Read(input, 0, 1024)) <= 0)
				{
					throw new Exception("EOF");
				}

				inf.SetInput(input, 0, count);
				totalin += count;
			}

			if (totalout == output.Length)
			{
				var newOutput = new byte[output.Length*2];
				Buffer.BlockCopy(output, 0, newOutput, 0, output.Length);
				output = newOutput;
			}

			totalout += inf.Inflate(output, totalout, output.Length - totalout);
		}

		var final = new byte[totalout];
		Buffer.BlockCopy(output, 0, final, 0, totalout);
		rdr.BaseStream.Position = rdr.BaseStream.Position - inf.RemainingInput;
		totalin -= inf.RemainingInput;

		return final;
	}

	/// <summary>
	/// Displays debug information about this MPK
	/// </summary>
	public void Display()
	{
		Console.WriteLine("**************************************************************************");
		Console.WriteLine(_name);
		Console.WriteLine("{0} files", _numFiles);
		Console.WriteLine("{0} actual files", _files.Count);
		Console.WriteLine("**************************************************************************");
		foreach (MpkFile file in _files.Values)
		{
			file.Display();
		}
	}

	private void OnInvalidFile(MpkFile file)
	{
		if (InvalidFile != null)
		{
			InvalidFile(file, new EventArgs());
		}
	}

	/// <summary>
	/// Creates a new MPK file
	/// </summary>
	/// <param name="fname">The mpk filename</param>
	public void Create(string fname)
	{
		_files.Clear();
		_name = fname;
	}
}