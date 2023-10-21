using System;
using DOL.Events;

namespace DOL.GS.PlayerTitles
{
	/// <summary>
	/// Base abstract class for typical player titles based on events.
	/// </summary>
	public abstract class EventPlayerTitle : APlayerTitle
	{
		/// <summary>
		/// Constructs a new EventPlayerTitle instance.
		/// </summary>
		protected EventPlayerTitle()
		{
			GameEventMgr.AddHandler(Event, new CoreEventHandler(EventCallback));
		}
		
		/// <summary>
		/// The event to hook.
		/// </summary>
		public abstract CoreEvent Event { get; }
		
		/// <summary>
		/// The event callback.
		/// </summary>
		/// <param name="e">The event fired.</param>
		/// <param name="sender">The event sender.</param>
		/// <param name="arguments">The event arguments.</param>
		protected virtual void EventCallback(CoreEvent e, object sender, EventArgs arguments)
		{
			GamePlayer p = sender as GamePlayer;
			if (p != null)
			{
				if (IsSuitable(p))
				{
					p.AddTitle(this);
				}
				else
				{
					p.RemoveTitle(this);
				}
			}
		}
	}
}