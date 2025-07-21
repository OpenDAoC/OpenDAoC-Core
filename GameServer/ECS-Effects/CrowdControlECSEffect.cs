using System;
using System.Linq;
using DOL.AI;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;

namespace DOL.GS
{
    public abstract class AbstractCrowdControlECSEffect : ECSGameSpellEffect
    {
        public AbstractCrowdControlECSEffect(in ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            if (Properties.IMMUNITY_TIMER_USE_ADAPTIVE)
            {
                ImmunityDuration = Math.Min(60000, (int) (Duration * Properties.IMMUNITY_TIMER_ADAPTIVE_LENGTH)); //cap at 60s
            }
            else
            {
                ImmunityDuration = Properties.IMMUNITY_TIMER_FLAT_LENGTH * 1000; // *1000 to convert to gameloop time
            }
        }

        protected void OnHardCCStart()
        {
            Owner.attackComponent.StopAttack();
            Owner.StopCurrentSpellcast();
            Owner.DisableTurning(true);

            if (Owner is GameNPC npc)
                npc.StopMoving();

            if (Owner.effectListComponent.GetEffects().FirstOrDefault(x => x.GetType() == typeof(SpeedOfSoundECSEffect)) == null)
                UpdatePlayerStatus();
        }

        protected void OnHardCCStop()
        {
            Owner.DisableTurning(false);
            UpdatePlayerStatus();

            // Re-schedule the next think so that the NPC can resume its attack immediately for example.
            if (Owner is GameNPC npc && npc.Brain is ABrain brain)
                brain.NextThinkTick = GameLoop.GameLoopTime;

            if (SpellHandler.Caster.Realm == 0 || Owner.Realm == 0)
                Owner.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
            else
                Owner.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
        }

        protected void UpdatePlayerStatus()
        {
            if (OwnerPlayer != null)
            {
                OwnerPlayer.Client.Out.SendUpdateMaxSpeed();
                if (OwnerPlayer.Group != null)
                    OwnerPlayer.Group.UpdateMember(OwnerPlayer, false, false);
            }
        }

        protected void SendMessages()
        {
            ((SpellHandler)SpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message1, eChatType.CT_Spell);
            ((SpellHandler)SpellHandler).MessageToCaster(Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, true)), eChatType.CT_Spell);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, true)), eChatType.CT_Spell, Owner, SpellHandler.Caster);
        }
    }

    /// <summary>
    /// Stun Effect
    /// </summary>
    public class StunECSGameEffect : AbstractCrowdControlECSEffect
    {
        public StunECSGameEffect(in ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            if (initParams.SpellHandler.Caster is GameSummonedPet)
                TriggersImmunity = false;
            else
                TriggersImmunity = true;
        }

        public override void OnStartEffect()
        {
            Owner.IsStunned = true;
            OnHardCCStart();
            UpdatePlayerStatus();

            // Immediately start the immunity effect for NPCs. This is used for diminishing returns.
            if (TriggersImmunity && Owner is GameNPC npc && !npc.effectListComponent.ContainsEffectForEffectType(eEffect.NPCStunImmunity))
                new NpcStunImmunityEffect(new ECSGameEffectInitParams(Owner, ImmunityDuration, Effectiveness, SpellHandler));

            // "You are stunned!"
            // "{0} is stunned!"
            OnEffectStartsMsg(true, true, true);
        }

        public override void OnStopEffect()
        {
            Owner.IsStunned = false;
            OnHardCCStop();
            UpdatePlayerStatus();

            // "You recover from the stun.."
            // "{0} recovers from the stun."
            OnEffectExpiresMsg(true, false, true);
        }
    }

    /// <summary>
    /// Mesmerize Effect
    /// </summary>
    public class MezECSGameEffect : AbstractCrowdControlECSEffect
    {
        public MezECSGameEffect(in ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            TriggersImmunity = true;
        }

        public override void OnStartEffect()
        {
            Owner.IsMezzed = true;
            OnHardCCStart();
            UpdatePlayerStatus();

            // Immediately start the immunity effect for NPCs. This is used for diminishing returns.
            if (TriggersImmunity && Owner is GameNPC npc && !npc.effectListComponent.ContainsEffectForEffectType(eEffect.NPCMezImmunity))
                new NpcMezImmunityEffect(new ECSGameEffectInitParams(Owner, ImmunityDuration, Effectiveness, SpellHandler));

            // "You are entranced!"
            // "You are mesmerized!"
            OnEffectStartsMsg(true, true, true);
        }

        public override void OnStopEffect()
        {
            Owner.IsMezzed = false;
            OnHardCCStop();
            UpdatePlayerStatus();

            // "You are no longer entranced."
            // "You recover from the mesmerize."
            OnEffectExpiresMsg(true, false, true);
        }
    }
}
