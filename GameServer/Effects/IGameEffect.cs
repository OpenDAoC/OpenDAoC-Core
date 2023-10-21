using System.Collections.Generic;
using Core.Database.Tables;

namespace Core.GS.Effects;

public interface IGameEffect
{
	/// <summary>
	/// Effect must be canceled
	/// </summary>
	/// <param name="playerCanceled">true if player decided to cancel that effect by shift + rightclick</param>
	void Cancel(bool playerCanceled);

	/// <summary>
	/// Name of the effect
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Remaining Time of the effect in seconds
	/// </summary>
	int RemainingTime { get; }

	/// <summary>
	/// Icon to show on players, can be id
	/// </summary>
	ushort Icon { get; }

	/// <summary>
	/// Unique ID, will be set by effect list on add
	/// </summary>
	ushort InternalID { get; set; }

	/// <summary>
	/// Delve Info
	/// </summary>
	IList<string> DelveInfo { get; }

	/// <summary>
	/// Get the save effect
	/// </summary>
	/// <returns></returns>
	DbPlayerXEffect getSavedEffect();
}