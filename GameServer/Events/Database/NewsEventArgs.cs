using System;
using Core.Database;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the news event
/// </summary>
public class NewsEventArgs : EventArgs
{
	/// <summary>
	/// Holds the target news for this event
	/// </summary>
	private DbNews m_news;
	
	/// <summary>
	/// Constructs a new event argument class for the
	/// news events 
	/// </summary>
	/// <param name="account"></param>
	public NewsEventArgs(DbNews news)
	{
		m_news = news;
	}

	/// <summary>
	/// Gets the target news for this event
	/// </summary>
	public DbNews News
	{
		get
		{
			return m_news;
		}
	}
}