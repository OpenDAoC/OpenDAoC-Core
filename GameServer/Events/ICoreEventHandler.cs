using System;

namespace Core.GS.Events;

/// <summary>
/// Objects Able to handle Notifications from CoreEvent.
/// </summary>
public interface ICoreEventHandler
{
	void Notify(CoreEvent e, object sender, EventArgs args);
}