using System.Linq;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS
{
    public abstract class AbstractCrowdControlECSEffect : ECSGameSpellEffect
    {
        public AbstractCrowdControlECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        protected void OnHardCCStart()
        {
            Owner.attackComponent.LivingStopAttack();
            Owner.StopCurrentSpellcast();
            Owner.DisableTurning(true);
            if (Owner is GameNPC npc)
                npc.StopMoving();
            if(Owner.effectListComponent.GetAllEffects().FirstOrDefault(x => x.GetType() == typeof(SpeedOfSoundECSEffect)) == null)
                UpdatePlayerStatus();
            
            //check for conquest activity
            if (Caster is GamePlayer caster)
            {
                if(ConquestService.ConquestManager.IsPlayerInConquestArea(caster))
                    ConquestService.ConquestManager.AddContributor(caster);
            }
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
                npc.TurnTo(npc.TargetObject);
            }
            if(SpellHandler.Caster is GamePlayer)
                Owner.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
            else
                Owner.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
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
            if (initParams.SpellHandler.Caster is GamePet)
                TriggersImmunity = false;
            else
                TriggersImmunity = true;
        }

        public override void OnStartEffect()
        {
            Owner.IsStunned = true;
            OnHardCCStart();
            UpdatePlayerStatus();
            
            // "You are stunned!"
            // "{0} is stunned!"
            OnEffectStartsMsg(Owner, true, true, true);

        }

        public override void OnStopEffect()
        {
            Owner.IsStunned = false;
            OnHardCCStop();
            UpdatePlayerStatus();
            
            // "You recover from the stun.."
            // "{0} recovers from the stun."
            OnEffectExpiresMsg(Owner, true, false, true);

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
            
            // "You are entranced!"
            // "You are mesmerized!"
            OnEffectStartsMsg(Owner, true, true, true);
        }

        public override void OnStopEffect()
        {
            Owner.IsMezzed = false;
            OnHardCCStop();
            UpdatePlayerStatus();
            
            // "You are no longer entranced."
            // "You recover from the mesmerize."
            OnEffectExpiresMsg(Owner, true, false, true);
        }
    }
}