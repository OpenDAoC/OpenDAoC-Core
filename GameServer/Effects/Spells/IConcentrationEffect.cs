namespace Core.GS.Effects;

/// <summary>
/// An effect that can be added to concentration list
/// </summary>
public interface IConcentrationEffect
{
	/// <summary>
	/// Name of the effect
	/// </summary>
	string Name { get; }

	/// <summary>
	/// The name of the owner
	/// </summary>
	string OwnerName { get; }

	/// <summary>
	/// Effect icon ID
	/// </summary>
	ushort Icon { get; }

	/// <summary>
	/// Amount of concentration used by effect
	/// </summary>
	byte Concentration { get; }

	/// <summary>
	/// Effect must be canceled
	/// </summary>
	/// <param name="playerCanceled">true if player decided to cancel that effect by shift + rightclick</param>
	// void Cancel(bool playerCanceled);
}