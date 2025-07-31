namespace DOL.GS
{
	/// <summary>
	/// GameNPC Helper Class is a collection of (static) GameNPC methods to avoid clutter in the GameNPC class itself.
	/// </summary>
	public static class GameNPCHelper
	{
		/// <summary>
		/// Cast a spell on player and its pets/subpets if available.
		/// </summary>
		/// <param name="sourceNpc">NPC that is casting the spell</param>
		/// <param name="player">Player is the owner and first target of the spell</param>
		/// <param name="spell">Casted spell</param>
		/// <param name="line">SpellLine the casted spell is derived from</param>
		/// <param name="checkLos">Determines if line of sight is checked</param>
		public static void CastSpellOnOwnerAndPets(this GameNPC sourceNpc, GamePlayer player, Spell spell, SpellLine line, bool checkLos)
		{
			sourceNpc.TargetObject = player;
			int spellRange = spell.CalculateEffectiveRange(sourceNpc);

			if (sourceNpc.IsWithinRadius(player, spellRange))
				sourceNpc.CastSpell(spell, line, checkLos);

			if (player.ControlledBrain != null)
			{
				sourceNpc.TargetObject = player.ControlledBrain.Body;

				if (sourceNpc.IsWithinRadius(player.ControlledBrain.Body, spellRange))
					sourceNpc.CastSpell(spell, line, checkLos);

				if (player.ControlledBrain.Body.ControlledNpcList != null)
				{
					foreach (AI.Brain.IControlledBrain subPet in player.ControlledBrain.Body.ControlledNpcList)
					{
						if (subPet != null && sourceNpc.IsWithinRadius(subPet.Body, spellRange))
						{
							sourceNpc.TargetObject = subPet.Body;
							sourceNpc.CastSpell(spell, line, checkLos);
						}
					}
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
		public static void CastSpellOnOwnerAndPets(this GameNPC sourceNPC, GamePlayer player, Spell spell, SpellLine line)
		{
			CastSpellOnOwnerAndPets(sourceNPC, player, spell, line, true);
		}
	}
}

