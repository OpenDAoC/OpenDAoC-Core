using System.Collections.Generic;

namespace Core.GS.Keeps;

public interface IGameKeepComponent
{
	/// <summary>
	/// Reference to owning Keep
	/// </summary>
	AGameKeep Keep { get; }
	
	/// <summary>
	/// Hook point Collection
	/// </summary>
	Dictionary<int, GameKeepHookPoint> HookPoints { get; }
	
	/// <summary>
	/// Keep Component ID.
	/// </summary>
	int ID { get; }
	
	/// <summary>
	/// GameObject ObjectID.
	/// </summary>
	int ObjectID { get; }
	
	/// <summary>
	/// Keep Component Skin ID.
	/// </summary>
	int Skin { get; }
	
	/// <summary>
	/// Keep Component X.
	/// </summary>
	int ComponentX { get; }
	
	/// <summary>
	/// Keep Component Y.
	/// </summary>
	int ComponentY { get; }
	
	/// <summary>
	/// Keep Component Heading.
	/// </summary>
	int ComponentHeading { get; }
	
	/// <summary>
	/// Keep Component Height
	/// </summary>
	int Height { get; }
	
	/// <summary>
	/// GameLiving Health Percent
	/// </summary>
	byte HealthPercent { get; }
	
	/// <summary>
	/// Status of GameComponent (Flag)
	/// </summary>
	byte Status { get; }
	
	/// <summary>
	/// Is Tower Componetn Raized ?
	/// </summary>
	bool IsRaized { get; }
	
	/// <summary>
	/// Enable component Climbing.
	/// </summary>
	bool Climbing { get; }
	
	/// <summary>
	/// GameObject AddToWorld Method
	/// </summary>
	/// <returns></returns>
	bool AddToWorld();
	
	/// <summary>
	/// GameLiving IsAlive.
	/// </summary>
	bool IsAlive { get; }
}