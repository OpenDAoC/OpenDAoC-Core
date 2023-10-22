using Core.GS.AI.Brains;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS;

/// <summary>
/// GameNPC Helper Class is a collection of (static) GameNPC methods to avoid clutter in the GameNPC class itself.
/// </summary>
public static class GameNpcHelper
{
	#region GameNPC cast methods
	/// <summary>
	/// Cast a spell on player and its pets/subpets if available.
	/// </summary>
	/// <param name="sourceNPC">NPC that is casting the spell</param>
	/// <param name="player">Player is the owner and first target of the spell</param>
	/// <param name="spell">Casted spell</param>
	/// <param name="line">SpellLine the casted spell is derived from</param>
	/// <param name="checkLOS">Determines if line of sight is checked</param>
	public static void CastSpellOnOwnerAndPets(this GameNpc sourceNPC, GamePlayer player, Spell spell, SpellLine line, bool checkLOS)
	{
		sourceNPC.TargetObject = player;
		if (sourceNPC.IsWithinRadius(player, spell.Range))
			sourceNPC.CastSpell(spell, line, checkLOS);
		if (player.ControlledBrain != null)
		{
			sourceNPC.TargetObject = player.ControlledBrain.Body;
			if (sourceNPC.IsWithinRadius(player.ControlledBrain.Body, spell.Range))
				sourceNPC.CastSpell(spell, line, checkLOS);
			if (player.ControlledBrain.Body.ControlledNpcList != null)
				foreach (IControlledBrain subpet in player.ControlledBrain.Body.ControlledNpcList)
					if (subpet != null && sourceNPC.IsWithinRadius(subpet.Body, spell.Range))
					{
						sourceNPC.TargetObject = subpet.Body;
						sourceNPC.CastSpell(spell, line, checkLOS);
					}
		}
	}

	/// <summary>
	/// Cast a spell on player and its pets/subpets if available (LOS checked).
	/// </summary>
	/// <param name="sourceNPC">NPC that is casting the spell</param>
	/// <param name="player">Player is the owner and first target of the spell</param>
	/// <param name="spell">Casted spell</param>
	/// <param name="line">SpellLine the casted spell is derived from</param>
	public static void CastSpellOnOwnerAndPets(this GameNpc sourceNPC, GamePlayer player, Spell spell, SpellLine line)
	{
		CastSpellOnOwnerAndPets(sourceNPC, player, spell, line, true);
	}
	#endregion GameNPC cast methods
}