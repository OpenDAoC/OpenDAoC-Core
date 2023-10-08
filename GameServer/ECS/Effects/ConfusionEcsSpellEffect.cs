﻿using System;
using DOL.AI.Brain;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ConfusionEcsSpellEffect : EcsGameSpellEffect
    {
        public ConfusionEcsSpellEffect(EcsGameEffectInitParams initParams)
            : base(initParams) 
        {
            PulseFreq = 5000;
        }

        public override void OnStartEffect()
        {
            if (Owner is GamePlayer)
            {
                /*
                    *Q: What does the confusion spell do against players?
                    *A: According to the magic man, “Confusion against a player interrupts their current action, whether it's a bow shot or spellcast.
                    */
                if (SpellHandler.Spell.Value < 0 || Util.Chance(Convert.ToInt32(Math.Abs(SpellHandler.Spell.Value))))
                {
                    //Spell value below 0 means it's 100% chance to confuse.
                    GamePlayer gPlayer = Owner as GamePlayer;

                    gPlayer.StartInterruptTimer(gPlayer.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
                    
                    // "You can't focus your knight viking badger helmet... meow!"
                    // "{0} is confused!"
                    OnEffectStartsMsg(Owner, true, false, true);

                }
                EffectService.RequestImmediateCancelEffect(this);
            }
            else if (Owner is GameNPC)
            {
                //check if we should do anything at all.

                bool doConfuse = (SpellHandler.Spell.Value < 0 || Util.Chance(Convert.ToInt32(SpellHandler.Spell.Value)));

                if (!doConfuse)
                    return;

                bool doAttackFriend = SpellHandler.Spell.Value < 0 && Util.Chance(Convert.ToInt32(Math.Abs(SpellHandler.Spell.Value)));

                GameNPC npc = Owner as GameNPC;

                npc.IsConfused = true;
                
                // "{0} is confused!"
                OnEffectStartsMsg(Owner, false, false, true);


                //if (log.IsDebugEnabled)
                //    log.Debug("CONFUSION: " + npc.Name + " was confused(true," + doAttackFriend.ToString() + ")");

                if (npc is GameSummonedPet && npc.Brain != null && (npc.Brain as IControlledBrain) != null)
                {
                    //it's a pet.
                    GamePlayer playerowner = (npc.Brain as IControlledBrain).GetPlayerOwner();
                    if (playerowner != null && (playerowner.CharacterClass.ID == (int)ECharacterClass.Theurgist || 
                        playerowner.CharacterClass.ID == (int)ECharacterClass.Animist && npc.Brain is TurretFnfBrain))
                    {
                        //Theurgist pets die.
                        npc.Die(SpellHandler.Caster);
                        EffectService.RequestImmediateCancelEffect(this);
                        return;
                    }
                }

                (SpellHandler as ConfusionSpellHandler).targetList.Clear();
                foreach (GamePlayer target in npc.GetPlayersInRadius(1000))
                {
                    if (doAttackFriend)
                        (SpellHandler as ConfusionSpellHandler).targetList.Add(target);
                    else
                    {
                        //this should prevent mobs from attacking friends.
                        if (GameServer.ServerRules.IsAllowedToAttack(npc, target, true))
                            (SpellHandler as ConfusionSpellHandler).targetList.Add(target);
                    }
                }

                foreach (GameNPC target in npc.GetNPCsInRadius(1000))
                {
                    //don't agro yourself.
                    if (target == npc)
                        continue;

                    if (doAttackFriend)
                        (SpellHandler as ConfusionSpellHandler).targetList.Add(target);
                    else
                    {
                        //this should prevent mobs from attacking friends.
                        if (GameServer.ServerRules.IsAllowedToAttack(npc, target, true) && !GameServer.ServerRules.IsSameRealm(npc, target, true))
                            (SpellHandler as ConfusionSpellHandler).targetList.Add(target);
                    }
                }

                //targetlist should be full, start effect pulse.
                if ((SpellHandler as ConfusionSpellHandler).targetList.Count > 0)
                {
                    npc.StopAttack();
                    npc.StopCurrentSpellcast();

                    GameLiving target = (SpellHandler as ConfusionSpellHandler).targetList[Util.Random((SpellHandler as ConfusionSpellHandler).targetList.Count - 1)] as GameLiving;
                    npc.StartAttack(target);
                }
            }
        }

        public override void OnStopEffect()
        {
            if (Owner != null && Owner is GameNPC)
            {
                GameNPC npc = Owner as GameNPC;
                npc.IsConfused = false;
            }
        }

        public override void OnEffectPulse()
        {
            if ((SpellHandler as ConfusionSpellHandler).targetList.Count > 0)
            {
                GameNPC npc = Owner as GameNPC;
                npc.StopAttack();
                npc.StopCurrentSpellcast();

                GameLiving target = (SpellHandler as ConfusionSpellHandler).targetList[Util.Random((SpellHandler as ConfusionSpellHandler).targetList.Count - 1)] as GameLiving;

                npc.StartAttack(target);
            }
        }
    }
}
