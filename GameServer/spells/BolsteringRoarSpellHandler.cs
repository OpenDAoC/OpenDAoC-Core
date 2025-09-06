using System.Collections.Generic;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.BolsteringRoar)]
    public class BolsteringRoarSpellHandler : RemoveSpellEffectHandler
    {
        public override List<GameLiving> SelectTargets(GameObject castTarget)
        {
            var list = GameLoop.GetListForTick<GameLiving>();

            if (Caster is GamePlayer)
            {
                GamePlayer casterPlayer = (GamePlayer)Caster;
                Group group = casterPlayer.Group;
                if(group == null) return list; // Should not appen since it is checked in ability handler
                int spellRange = Spell.CalculateEffectiveRange(Caster);
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
                                    {
                                        if (casterPlayer.IsWithinRadius( npc.Body, spellRange ))
                                            list.Add(npc.Body);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return list;
        }

        // constructor
        public BolsteringRoarSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
 			// RR4: now it's a list
			m_spellTypesToRemove = new List<string>();       	
            m_spellTypesToRemove.Add("Mesmerize");
            m_spellTypesToRemove.Add("SpeedDecrease");
            m_spellTypesToRemove.Add("StyleSpeedDecrease");
            m_spellTypesToRemove.Add("DamageSpeedDecrease");
            m_spellTypesToRemove.Add("HereticSpeedDecrease");
            m_spellTypesToRemove.Add("HereticDamageSpeedDecreaseLOP");
            m_spellTypesToRemove.Add("VampiirSpeedDecrease");
            m_spellTypesToRemove.Add("ValkyrieSpeedDecrease");
            m_spellTypesToRemove.Add("WarlockSpeedDecrease");
        }
    }
}
