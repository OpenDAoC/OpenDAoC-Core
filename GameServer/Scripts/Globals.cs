using System.Collections;
using System.IO;

namespace DOL.GS.Scripts
{
	/// <summary>
	/// The hand flag for an item
	/// </summary>
	public enum EHandFlag : int
	{
		/// <summary>
		/// Standard or Right hand
		/// </summary>
		Right = 0,
		/// <summary>
		/// Two handed
		/// </summary>
		Two = 1,
		/// <summary>
		/// Left handed
		/// </summary>
		Left = 2,
	}

	/// <summary>
	///This class is used to read commaseperated files!
	///Can be used to parse userdefined tables by index (first value in row).
	///(eg. from Excel or *hint* the tables from the gamedata.mpk file)
	///Values are cached, so it only needs to read the file once! 
	/// </summary>
	public class CSVFileTableReader
	{
		private Hashtable table = null;

		/// <summary>
		/// Creates an instance of the CSVFileTableReader from a csvfile
		/// </summary>
		/// <param name="csvFile"></param>
		public CSVFileTableReader(string csvFile)
		{
			StreamReader reader = null;
			try
			{
				table = new Hashtable();
				reader = File.OpenText(csvFile);
				while (reader.Peek() != -1)
				{
					//read a line from the stream
					string line = reader.ReadLine();
					int firstsep = line.IndexOf(",");
					if (firstsep != -1)
					{
						string key = line.Substring(0, firstsep);
						line.Remove(0, firstsep + 1);
						string[] values = line.Split(',');
						table.Add(key, values);
					}
				}
				reader.Close();
			}
			catch
			{
				table = null;
			}
		}

		/// <summary>
		/// Is the table ready
		/// </summary>
		public bool IsReady
		{
			get { return table != null; }
		}

		/// <summary>
		/// Find a CSV Entry by a firstvalue
		/// </summary>
		/// <param name="firstvalue"></param>
		/// <returns></returns>
		public string[] FindCSVEntry(string firstvalue)
		{
			if (table == null)
				return null;
			return (string[]) table[firstvalue];
		}
	}
}