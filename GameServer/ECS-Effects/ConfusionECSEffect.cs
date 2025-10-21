using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ConfusionECSGameEffect : ECSGameSpellEffect
    {
        public ConfusionECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            // Confusion spells don't have a frequency set in the database.
            PulseFreq = 6000;
            NextTick = GameLoop.GameLoopTime;
        }

        public override void OnStartEffect()
        {
            // Spell value below 0 means it's 100% chance to confuse.
            if (SpellHandler.Spell.Value >= 0 && !Util.Chance(Convert.ToInt32(SpellHandler.Spell.Value)))
            {
                Stop();
                return;
            }

            if (Owner is GamePlayer)
            {
                /*
                 * Q: What does the confusion spell do against players?
                 * A: According to the magic man, Confusion against a player interrupts their current action, whether it's a bow shot or spellcast.
                 */
                // "You can't focus your knight viking badger helmet... meow!"
                // "{0} is confused!"
                OnEffectStartsMsg(true, false, true);
                Stop();
            }
            else if (Owner is GameNPC npc && npc.Brain is StandardMobBrain brain)
            {
                // "{0} is confused!"
                OnEffectStartsMsg(false, false, true);

                // Theurgist pets die when confused.
                // This isn't strictly accurate to 1.65 gameplay, where they don't die if there is only one pet and its target is the one casting the spell.
                // But live gameplay is an inconsistent mess in this regard, so this is a reasonable simplification.
                if (npc.Brain is TheurgistPetBrain)
                {
                    npc.Die(SpellHandler.Caster);
                    Stop();
                    return;
                }

                npc.IsConfused = true;
                brain.SetTemporaryAggroList();
            }
        }

        public override void OnStopEffect()
        {
            if (Owner is not GameNPC npc || npc.Brain is not StandardMobBrain brain)
                return;

            npc.IsConfused = false;
            brain.UnsetTemporaryAggroList();
        }

        public override void OnEffectPulse()
        {
            if (Owner is not GameNPC npc || npc.Brain is not StandardMobBrain brain)
                return;

            List<GameLiving> targetList = (SpellHandler as ConfusionSpellHandler).TargetList;
            targetList.Clear();
            bool doAttackFriend = SpellHandler.Spell.Value < 0 && Util.Chance(Convert.ToInt32(Math.Abs(SpellHandler.Spell.Value)));

            foreach (GamePlayer target in npc.GetPlayersInRadius(750))
            {
                if (!GameServer.ServerRules.IsAllowedToAttack(npc, target, true))
                    continue;

                if (doAttackFriend || target.Realm != npc.Realm)
                    targetList.Add(target);
            }

            foreach (GameNPC target in npc.GetNPCsInRadius(750))
            {
                if (!GameServer.ServerRules.IsAllowedToAttack(npc, target, true))
                    continue;

                if (doAttackFriend || target.Realm != npc.Realm)
                    targetList.Add(target);
            }

            if (targetList.Count <= 0)
                return;

            brain.Disengage();
            GameLiving randomTarget = targetList[Util.Random(targetList.Count - 1)];
            brain.ForceAddToAggroList(randomTarget);
        }
    }
}
