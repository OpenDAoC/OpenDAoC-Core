using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.SkillHandler;

using DOL.Events;
using DOL.GS;


namespace DOL.GS.Spells
{
	[SpellHandlerAttribute("StarsProc")]
	public class BandOfStarsProc : SpellHandler
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
		
		protected class BoltOnTargetAction : RegionAction
		{
			protected readonly GameLiving m_boltTarget;
			protected readonly BandOfStarsProc m_handler;
			
			public BoltOnTargetAction(GameLiving actionSource, GameLiving boltTarget, BandOfStarsProc spellHandler) : base(actionSource)
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
				GameLiving caster = (GameLiving)m_actionSource;
				if (target == null) return 0;
				if (target.CurrentRegion.ID != caster.CurrentRegion.ID) return 0;
				if (target.ObjectState != GameObject.eObjectState.Active) return 0;
				if (!target.IsAlive) return 0;
				if (target == null) return 0;
				if (!target.IsAlive || target.ObjectState!=GameLiving.eObjectState.Active) return 0;
				
				AttackData ad = m_handler.CalculateDamageToTarget(target, 1);
				ad.Damage = (int)m_handler.Spell.Damage;
				m_handler.SendDamageMessages(ad);
				m_handler.DamageTarget(ad, false);
				
				//if (m_handler.Spell.SubSpellID != 0) Spell subspell = m_handler.SkillBase.GetSpellByID(m_handler.Spell.SubSpellID);
                if (m_handler.Spell.SubSpellID != 0 && SkillBase.GetSpellByID(m_handler.Spell.SubSpellID) != null)
				{
					ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(caster, SkillBase.GetSpellByID(m_handler.Spell.SubSpellID), SkillBase.GetSpellLine("Mob Spells"));
					spellhandler.StartSpell(target);
				}
					
				target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, caster);
				return 0;
			}
		}

		public BandOfStarsProc(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	
	[SpellHandlerAttribute("StarsProc2")]
    public class BandOfStarsProc2 : SpellHandler
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
				if(m_spell.LifeDrainReturn>0) if(player.CharacterClass.ID!=(byte)ECharacterClass.Necromancer) player.Model=(ushort)m_spell.LifeDrainReturn;
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
				if(player.CharacterClass.ID!=(byte)ECharacterClass.Necromancer) player.Model = player.CreationModel;
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumberance();
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();  
            }                       
            return base.OnEffectExpires(effect, noMessages);
        }

		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
		{
			base.ApplyEffectOnTarget(target, effectiveness);
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
			if(target is GameNpc) 
			{
				IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
				if (aggroBrain != null)
					aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
			}
		}		
        public BandOfStarsProc2(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
 
}