using System;
using Core.Database;
using Core.Database.Tables;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the account events
/// </summary>
public class AccountEventArgs : EventArgs
{
	/// <summary>
	/// Holds the target account for this event
	/// </summary>
	private DbAccount m_account;
	
	/// <summary>
	/// Constructs a new event argument class for the
	/// account events 
	/// </summary>
	/// <param name="account"></param>
	public AccountEventArgs(DbAccount account)
	{
		m_account = account;
	}

	/// <summary>
	/// Gets the target account for this event
	/// </summary>
	public DbAccount Account
	{
		get
		{
			return m_account;
		}
	}
}