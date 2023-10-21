using System;

namespace Core.Events;

/// <summary>
/// Objects Able to handle Notifications from DOLEvents.
/// </summary>
public interface ICoreEventHandler
{
	void Notify(CoreEvent e, object sender, EventArgs args);
}