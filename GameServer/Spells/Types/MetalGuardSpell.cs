using System.Collections.Generic;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.GameUtils;

namespace Core.GS.Spells
{
    [SpellHandler("MetalGuard")]
    public class MetalGuardSpell : ArmorAbsorptionBuff
    {
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            var list = new List<GameLiving>();
            GameLiving target = castTarget as GameLiving;

            if (Caster is GamePlayer)
            {
                GamePlayer casterPlayer = (GamePlayer)Caster;
                GroupUtil group = casterPlayer.Group;
                if(group == null) return list; // Should not appen since it is checked in ability handler
                int spellRange = CalculateSpellRange();
                if (group != null)
                {
                    lock (group)
                    {
                        foreach (GamePlayer groupPlayer in casterPlayer.GetPlayersInRadius((ushort)m_spell.Radius))
                        {
                            if (casterPlayer.Group.IsInTheGroup(groupPlayer))
                            {
                                if (groupPlayer != casterPlayer && groupPlayer.IsAlive)
                                {
                                    list.Add(groupPlayer);
                                    IControlledBrain npc = groupPlayer.ControlledBrain;
                                    if (npc != null)
                                        if (casterPlayer.IsWithinRadius( npc.Body, spellRange ))
                                            list.Add(npc.Body);
                                }
                            }
                        }
                    }
                }
            }
            return list;
        }    	    	
        public MetalGuardSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
