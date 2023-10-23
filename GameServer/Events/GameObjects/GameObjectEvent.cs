
namespace Core.GS.Events;

/// <summary>
/// This class holds all possible GameObject events.
/// Only constants defined here!
/// </summary>
public class GameObjectEvent : CoreEvent
{
	/// <summary>
	/// Constructs a new GameObjectEvent
	/// </summary>
	/// <param name="name">the name of the event</param>
	protected GameObjectEvent(string name) : base(name)
	{
	}

	/// <summary>
	/// Tests if this event is valid for the specified object
	/// </summary>
	/// <param name="o">The object for which the event wants to be registered</param>
	/// <returns>true if valid, false if not</returns>
	public override bool IsValidFor(object o)
	{
		return o is GameObject;
	}

	/// <summary>
	/// The AddToWorld event is fired whenever the object is added to the world
	/// </summary>
	public static readonly GameObjectEvent AddToWorld = new GameObjectEvent("GameObject.AddToWorld");
	/// <summary>
	/// The RemoveFromWorld event is fired whenever the object is removed from the world
	/// </summary>
	public static readonly GameObjectEvent RemoveFromWorld = new GameObjectEvent("GameObject.RemoveFromWorld");
	/// <summary>
	/// The MoveTo event is fired whenever the object is moved to a new position by the MoveTo method
	/// <seealso cref="MoveToEventArgs"/>
	/// </summary>
	public static readonly GameObjectEvent MoveTo = new GameObjectEvent("GameObject.MoveTo");
	/// <summary>
	/// The Delete event is fired whenever the object is deleted
	/// </summary>
	public static readonly GameObjectEvent Delete = new GameObjectEvent("GameObject.Delete");
	/// <summary>
	/// The Interact event is fired whenever a player interacts with this object
	/// <seealso cref="InteractEventArgs"/>
	/// </summary>
	public static readonly GameObjectEvent Interact = new GameObjectEvent("GameObject.Interact");
	/// <summary>
	/// The Interact Failed event is fired whenever a player interacts with this object but fails
	/// <seealso cref="InteractEventArgs"/>
	/// </summary>
	public static readonly GameObjectEvent InteractFailed = new GameObjectEvent("GameObject.InteractFailed");
	/// <summary>
	/// The Interact event is fired whenever a player interacts with something
	/// <seealso cref="InteractEventArgs"/>
	/// </summary>
	public static readonly GameObjectEvent InteractWith = new GameObjectEvent("GameObject.InteractWith");
	/// <summary>
	/// The ReceiveItem event is fired whenever the object receives an item
	/// <seealso cref="ReceiveItemEventArgs"/>
	/// </summary>
	public static readonly GameObjectEvent ReceiveItem = new GameObjectEvent("GameObjectEvent.ReceiveItem");
	/// <summary>
	/// The ReceiveMoney event is fired whenever the object receives money
	/// <seealso cref="ReceiveMoneyEventArgs"/>
	/// </summary>
	public static readonly GameObjectEvent ReceiveMoney = new GameObjectEvent("GameObjectEvent.ReceiveMoney");
	/// <summary>
	/// The TakeDamage event is fired whenever an object takes damage
	/// <seealso cref="TakeDamageEventArgs"/>
	/// </summary>
	public static readonly GameObjectEvent TakeDamage = new GameObjectEvent("GameObject.TakeDamage");
	
	/// <summary>
	/// The FinishedLosCheck event is fired whenever a LoS Check is finished.
	/// </summary>
	public static readonly GameObjectEvent FinishedLosCheck = new GameObjectEvent("GameObject.FinishLosCheck");
	
}