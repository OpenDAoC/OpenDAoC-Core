using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;

namespace DOL.GS
{
    public abstract class AbstractCrowdControlECSEffect : ECSGameEffect
    {
        public AbstractCrowdControlECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        protected void OnHardCCStart()
        {
            Owner.attackComponent.LivingStopAttack();
            Owner.StopCurrentSpellcast();
            Owner.DisableTurning(true);
            UpdatePlayerStatus();
        }

        protected void OnHardCCStop()
        {
            Owner.DisableTurning(false);
            UpdatePlayerStatus();

            GameNPC npc = Owner as GameNPC;
            if (npc != null)
            {
                IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(SpellHandler.Caster, 1);
                npc.attackComponent.AttackState = true;
            }
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
        public StunECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            TriggersImmunity = true;
        }

        public override void OnStartEffect()
        {
            Owner.IsStunned = true;
            OnHardCCStart();
            UpdatePlayerStatus();
            SendMessages();
        }

        public override void OnStopEffect()
        {
            Owner.IsStunned = false;
            OnHardCCStop();
            UpdatePlayerStatus();
        }
    }

    /// <summary>
    /// Mesmerize Effect
    /// </summary>
    public class MezECSGameEffect : AbstractCrowdControlECSEffect
    {
        public MezECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            TriggersImmunity = true;
        }

        public override void OnStartEffect()
        {
            Owner.IsMezzed = true;
            OnHardCCStart();
            UpdatePlayerStatus();
            SendMessages();
        }

        public override void OnStopEffect()
        {
            Owner.IsMezzed = false;
            OnHardCCStop();
            UpdatePlayerStatus();
        }
    }
}