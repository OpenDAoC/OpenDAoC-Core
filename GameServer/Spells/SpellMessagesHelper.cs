using Core.GS.Enums;

namespace Core.GS.Spells;

public static class SpellMessagesHelper
{
	/// <summary>
	/// Sends a message to the caster, if the caster is a controlled
	/// creature, to the player instead (only spell hit and resisted
	/// messages).
	/// </summary>
	/// <param name="handler"></param>
	/// <param name="type"></param>
	/// <param name="format"></param>
	/// <param name="args"></param>
	public static void MessageToCaster(this SpellHandler handler, EChatType type, string format, params object[] args)
	{
		handler.MessageToCaster(string.Format(format, args), type);
	}
	
	/// <summary>
	/// Sends a message to a living
	/// </summary>
	/// <param name="handler"></param>
	/// <param name="living"></param>
	/// <param name="type"></param>
	/// <param name="format"></param>
	/// <param name="args"></param>
	public static void MessageToLiving(this SpellHandler handler, GameLiving living, EChatType type, string format, params object[] args)
	{
		handler.MessageToLiving(living, string.Format(format, args), type);
	}
}