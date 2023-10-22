using System;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("StarsProc")]
public class StarsProc : SpellHandler
{
    public override bool CheckBeginCast(GameLiving selectedTarget)
    {
        return base.CheckBeginCast(selectedTarget);
    }

    public override bool StartSpell(GameLiving target)
    {
        foreach (GameLiving targ in SelectTargets(target))
        {
            DealDamage(targ);
        }

        return true;
    }
    
    private void DealDamage(GameLiving target)
    {
        int ticksToTarget = m_caster.GetDistanceTo(target) * 100 / 85; // 85 units per 1/10s
        int delay = 1 + ticksToTarget / 100;
        foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            player.Out.SendSpellEffectAnimation(m_caster, target, m_spell.ClientEffect, (ushort)(delay), false, 1);
        }
        BoltOnTargetAction bolt = new BoltOnTargetAction(Caster, target, this);
        bolt.Start(1 + ticksToTarget);
    }
    
    public override void FinishSpellCast(GameLiving target)
    {
        if (target is Keeps.GameKeepDoor || target is Keeps.GameKeepComponent)
        {
            MessageToCaster("Your spell has no effect on the keep component!", EChatType.CT_SpellResisted);
            return;
        }
        base.FinishSpellCast(target);
    }
    
    protected class BoltOnTargetAction : EcsGameTimerWrapperBase
    {
        protected readonly GameLiving m_boltTarget;
        protected readonly StarsProc m_handler;
        
        public BoltOnTargetAction(GameLiving actionSource, GameLiving boltTarget, StarsProc spellHandler) : base(actionSource)
        {
            if (boltTarget == null)
                throw new ArgumentNullException("boltTarget");
            if (spellHandler == null)
                throw new ArgumentNullException("spellHandler");
            m_boltTarget = boltTarget;
            m_handler = spellHandler;
        }

        protected override int OnTick(EcsGameTimer timer)
        {
            GameLiving target = m_boltTarget;
            GameLiving caster = (GameLiving) timer.Owner;

            if (target == null || !target.IsAlive || target.ObjectState != GameObject.eObjectState.Active || target.CurrentRegionID != caster.CurrentRegionID)
                return 0;

            m_handler.Effectiveness = 1;
            AttackData ad = m_handler.CalculateDamageToTarget(target);
            ad.Damage = (int)m_handler.Spell.Damage;
            m_handler.SendDamageMessages(ad);
            m_handler.DamageTarget(ad, false);

            //if (m_handler.Spell.SubSpellID != 0) Spell subspell = m_handler.SkillBase.GetSpellByID(m_handler.Spell.SubSpellID);
            if (m_handler.Spell.SubSpellID != 0 && SkillBase.GetSpellByID(m_handler.Spell.SubSpellID) != null)
            {
                ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(caster, SkillBase.GetSpellByID(m_handler.Spell.SubSpellID), SkillBase.GetSpellLine("Mob Spells"));
                spellhandler.StartSpell(target);
            }

            target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, caster);
            return 0;
        }
    }

    public StarsProc(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}

[SpellHandler("StarsProc2")]
public class StarsProc2 : SpellHandler
{
    public override int CalculateSpellResistChance(GameLiving target)
    {
        return 0;
    }

    public override void OnEffectStart(GameSpellEffect effect)
    {
        base.OnEffectStart(effect);            
        effect.Owner.DebuffCategory[(int)EProperty.Dexterity] += (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Strength] += (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Constitution] += (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Acuity] += (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Piety] += (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Empathy] += (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Quickness] += (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Intelligence] += (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Charisma] += (int)m_spell.Value;   
        effect.Owner.DebuffCategory[(int)EProperty.ArmorAbsorption] += (int)m_spell.Value; 
        effect.Owner.DebuffCategory[(int)EProperty.MagicAbsorption] += (int)m_spell.Value; 
        
        if(effect.Owner is GamePlayer)
        {
            GamePlayer player = effect.Owner as GamePlayer;  
            if(m_spell.LifeDrainReturn>0) if(player.PlayerClass.ID!=(byte)EPlayerClass.Necromancer) player.Model=(ushort)m_spell.LifeDrainReturn;
            player.Out.SendCharStatsUpdate();
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();             	
        }
    }

    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
        effect.Owner.DebuffCategory[(int)EProperty.Dexterity] -= (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Strength] -= (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Constitution] -= (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Acuity] -= (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Piety] -= (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Empathy] -= (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Quickness] -= (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Intelligence] -= (int)m_spell.Value;
        effect.Owner.DebuffCategory[(int)EProperty.Charisma] -= (int)m_spell.Value;        
        effect.Owner.DebuffCategory[(int)EProperty.ArmorAbsorption] -= (int)m_spell.Value; 
        effect.Owner.DebuffCategory[(int)EProperty.MagicAbsorption] -= (int)m_spell.Value; 

        if(effect.Owner is GamePlayer)
        {
            GamePlayer player = effect.Owner as GamePlayer;  
            if(player.PlayerClass.ID!=(byte)EPlayerClass.Necromancer) player.Model = player.CreationModel;
            player.Out.SendCharStatsUpdate();
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();
        }

        return base.OnEffectExpires(effect, noMessages);
    }

    public override void ApplyEffectOnTarget(GameLiving target)
    {
        base.ApplyEffectOnTarget(target);
        if (target.Realm == 0 || Caster.Realm == 0)
        {
            target.LastAttackedByEnemyTickPvE = GameLoopMgr.GameLoopTime;
            Caster.LastAttackTickPvE = GameLoopMgr.GameLoopTime;
        }
        else
        {
            target.LastAttackedByEnemyTickPvP = GameLoopMgr.GameLoopTime;
            Caster.LastAttackTickPvP = GameLoopMgr.GameLoopTime;
        }
        if(target is GameNpc) 
        {
            IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
            if (aggroBrain != null)
                aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
        }
    }

    public StarsProc2(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}