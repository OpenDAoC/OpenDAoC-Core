using System;
using System.Linq;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;
using Core.GS.Spells;

namespace Core.GS.ECS;

public abstract class ACrowdControlEcsSpellEffect : EcsGameSpellEffect
{
    public ACrowdControlEcsSpellEffect(EcsGameEffectInitParams initParams)
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
        if (Owner is GameNpc npc)
            npc.StopMoving();
        if(Owner.effectListComponent.GetAllEffects().FirstOrDefault(x => x.GetType() == typeof(OfRaSpeedOfSoundEcsEffect)) == null)
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

        if (SpellHandler.Caster is GamePlayer)
            Owner.LastAttackedByEnemyTickPvP = GameLoopMgr.GameLoopTime;
        else
            Owner.LastAttackedByEnemyTickPvE = GameLoopMgr.GameLoopTime;
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
        ((SpellHandler)SpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message1, EChatType.CT_Spell);
        ((SpellHandler)SpellHandler).MessageToCaster(Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, true)), EChatType.CT_Spell);
        MessageUtil.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, true)), EChatType.CT_Spell, Owner, SpellHandler.Caster);
    }
}

/// <summary>
/// Stun Effect
/// </summary>
public class StunEcsSpellEffect : ACrowdControlEcsSpellEffect
{
    public StunEcsSpellEffect(EcsGameEffectInitParams initParams)
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
public class MezEcsSpellEffect : ACrowdControlEcsSpellEffect
{
    public MezEcsSpellEffect(EcsGameEffectInitParams initParams)
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