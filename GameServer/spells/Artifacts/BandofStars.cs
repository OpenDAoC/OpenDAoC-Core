using System;
using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.StarsProc)]
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

        protected class BoltOnTargetAction : ECSGameTimerWrapperBase
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

            protected override int OnTick(ECSGameTimer timer)
            {
                GameLiving target = m_boltTarget;
                GameLiving caster = (GameLiving) timer.Owner;

                if (target == null || !target.IsAlive || target.ObjectState != GameObject.eObjectState.Active || target.CurrentRegionID != caster.CurrentRegionID)
                    return 0;

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

                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);
                return 0;
            }
        }

        public StarsProc(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
    }

    [SpellHandler(eSpellType.StarsProc2)]
    public class StarsProc2 : SpellHandler
    {
        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);            
            effect.Owner.DebuffCategory[(int)eProperty.Dexterity] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Strength] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Constitution] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Acuity] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Piety] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Empathy] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Quickness] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Intelligence] += (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Charisma] += (int)m_spell.Value;   
            effect.Owner.DebuffCategory[(int)eProperty.ArmorAbsorption] += (int)m_spell.Value; 
            effect.Owner.DebuffCategory[(int)eProperty.MagicAbsorption] += (int)m_spell.Value; 
            
            if(effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;  
                if(m_spell.LifeDrainReturn>0) if(player.CharacterClass.ID!=(byte)eCharacterClass.Necromancer) player.Model=(ushort)m_spell.LifeDrainReturn;
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumbrance();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();             	
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.DebuffCategory[(int)eProperty.Dexterity] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Strength] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Constitution] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Acuity] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Piety] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Empathy] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Quickness] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Intelligence] -= (int)m_spell.Value;
            effect.Owner.DebuffCategory[(int)eProperty.Charisma] -= (int)m_spell.Value;        
            effect.Owner.DebuffCategory[(int)eProperty.ArmorAbsorption] -= (int)m_spell.Value; 
            effect.Owner.DebuffCategory[(int)eProperty.MagicAbsorption] -= (int)m_spell.Value; 

            if(effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;  
                if(player.CharacterClass.ID!=(byte)eCharacterClass.Necromancer) player.Model = player.CreationModel;
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumbrance();
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
                target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
            }
            else
            {
                target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
            }
            if(target is GameNPC) 
            {
                IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
            }
        }

        public StarsProc2(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
