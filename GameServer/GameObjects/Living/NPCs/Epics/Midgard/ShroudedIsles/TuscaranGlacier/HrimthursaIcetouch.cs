﻿using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
	public class HrimthursaIcetouch : GameEpicNPC
	{
		public HrimthursaIcetouch() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162231);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			HrimthursaIcetouchBrain sbrain = new HrimthursaIcetouchBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class HrimthursaIcetouchBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public HrimthursaIcetouchBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
        public override void AttackMostWanted()
        {
			if (CanHeal)
				return;
			else
				base.AttackMostWanted();
        }
		private bool CanHeal = false;
		private bool LockNpc = false;
		private bool ResetNoTarget;
		private GameNpc healnpc = null;
		private GameNpc HealNpc
		{
			get { return healnpc; }
			set { healnpc = value; }
		}
		public override void Think()
		{
			if(Body.IsAlive)
            {
				#region Heal mobs				
				List<GameNpc> NpcToHeal = new List<GameNpc>();
				GameLiving target = Body.TargetObject as GameLiving;
                foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
                {
					if (npc != null && npc.IsAlive && npc.Faction == Body.Faction )
					{
						if (!NpcToHeal.Contains(npc) && npc.HealthPercent < 80)//add here mobs to heal
							NpcToHeal.Add(npc);
					}
                }
				if (NpcToHeal.Count > 0)
				{
					if (!LockNpc)
					{
						GameNpc mob = NpcToHeal[Util.Random(0, NpcToHeal.Count - 1)];//pick randomly mob that need to be healed
						HealNpc = mob;
						LockNpc = true;
					}
				}
				if(HealNpc != null && HealNpc.IsAlive)//start heal
                {
					if (HealNpc.HealthPercent < 80)
					{
						if (HealNpc.IsWithinRadius(Body, 1000))
						{
							CanHeal = true;
							ClearAggroList();
							Body.attackComponent.StopAttack();
							Body.TargetObject = HealNpc;
							Body.CastSpell(IcetouchHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							ResetNoTarget = false;
						}
					}
					else
                    {
						//HealNpc = null;
						LockNpc = false;
						CanHeal = false;
						if (!HasAggro)
						{
							if (!ResetNoTarget)
							{
								Body.TargetObject = null;
								ResetNoTarget = true;
							}
							FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
						}
					}
				}
				if (Body.InCombatInLast(20 * 1000) == false && this.Body.InCombatInLast(25 * 1000))//reset checks if not in aggro after x sec
                {
					CanHeal = false;
					HealNpc = null;
					LockNpc = false;
					if (NpcToHeal.Count > 0)
						NpcToHeal.Clear();
				}
				#endregion
				if (HasAggro && Body.TargetObject != null)
                {
					ResetNoTarget = false;
					if(!target.effectListComponent.ContainsEffectForEffectType(EEffect.Mez) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.MezImmunity) && !Body.IsCasting && Util.Chance(30))
						Body.CastSpell(IcetouchMezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (!Body.IsCasting && Util.Chance(30))
						Body.CastSpell(IcetouchRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
            }
			base.Think();
		}
		#region Spells
		private Spell m_IcetouchHeal;
		private Spell IcetouchHeal
		{
			get
			{
				if (m_IcetouchHeal == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 4;
					spell.Power = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4659;
					spell.Icon = 4659;
					spell.Value = 500;
					spell.Name = "Glacier Healing";
					spell.Range = 1500;
					spell.SpellID = 11966;
					spell.Target = "Realm";
					spell.Type = ESpellType.Heal.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_IcetouchHeal = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IcetouchHeal);
				}
				return m_IcetouchHeal;
			}
		}
		private protected Spell m_IcetouchMezz;
		private protected Spell IcetouchMezz
		{
			get
			{
				if (m_IcetouchMezz == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4678;
					spell.Icon = 4678;
					spell.TooltipId = 4678;
					spell.Duration = 80;
					spell.Name = "Unmake Mind";
					spell.Message1 = "You are mesmerized!";
					spell.Message2 = "{0} is mesmerized!";
					spell.Message3 = "You recover from the mesmerize.";
					spell.Message4 = "{0} recovers from the mesmerize.";
					spell.Range = 1500;
					spell.SpellID = 11967;
					spell.Target = "Enemy";
					spell.Type = "Mesmerize";
					spell.DamageType = (int)EDamageType.Energy;
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_IcetouchMezz = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IcetouchMezz);
				}
				return m_IcetouchMezz;
			}
		}
		private protected Spell m_IcetouchRoot;
		private protected Spell IcetouchRoot
		{
			get
			{
				if (m_IcetouchRoot == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 30;
					spell.ClientEffect = 177;
					spell.Icon = 177;
					spell.TooltipId = 177;
					spell.Duration = 80;
					spell.Value = 99;
					spell.Name = "Anchor Of Ice";
					spell.Message1 = "Your feet are frozen to the ground!";
					spell.Message2 = "{0}'s feet are frozen to the ground!";
					spell.Range = 1500;
					spell.SpellID = 11968;
					spell.Target = "Enemy";
					spell.Type = "SpeedDecrease";
					spell.DamageType = (int)EDamageType.Cold;
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_IcetouchRoot = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IcetouchRoot);
				}
				return m_IcetouchRoot;
			}
		}
		#endregion
	}
}
