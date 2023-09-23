using System;

namespace DOL.Database
{
	/// <summary>
	/// Any exception that occurs in the O-R persistence layer.
	/// </summary>
	public class DatabaseException : ApplicationException
	{
		/// <summary>
		/// Constructor for an DatabaseException
		/// </summary>
		/// <param name="e">Baseexeption for this error</param>
		public DatabaseException(Exception e) : base(string.Empty, e)
		{
		}

		/// <summary>
		/// Constructor for an DatabaseException
		/// </summary>
		/// <param name="str">Reason that describes the Problem</param>
		/// <param name="e">Baseexeption for this error</param>
		public DatabaseException(string str, Exception e) : base(str, e)
		{
		}

		/// <summary>
		/// Constructor for an DatabaseException
		/// </summary>
		/// <param name="str">Reason that describes the Problem</param>
		public DatabaseException(string str) : base(str)
		{
		}
	}
}