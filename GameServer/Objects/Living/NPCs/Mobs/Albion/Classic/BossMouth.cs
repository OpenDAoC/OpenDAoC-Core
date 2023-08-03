﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class BossMouth : GameEpicBoss
	{
		public BossMouth() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Mouth Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13025);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(18);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(18));

			MouthBrain sbrain = new MouthBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNpc npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "MouthAdd")
					npc.Die(this);
			}
			base.Die(killer);
        }
        public override void OnAttackEnemy(AttackData ad)
		{
			if (UtilCollection.Chance(30))
			{
				if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				{
					CastSpell(MouthLifeDrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.OnAttackEnemy(ad);
		}
        public override void DealDamage(AttackData ad)
        {
			if (ad != null && ad.AttackType == AttackData.EAttackType.Spell && ad.Damage > 0)
			{
				Health += ad.Damage;
			}
            base.DealDamage(ad);
        }
        private Spell m_MouthLifeDrain;
		private Spell MouthLifeDrain
		{
			get
			{
				if (m_MouthLifeDrain == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 14352;
					spell.Icon = 14352;
					spell.TooltipId = 14352;
					spell.Damage = 500;
					spell.Name = "Lifedrain";
					spell.Range = 350;
					spell.SpellID = 11897;
					spell.Target = ESpellTarget.Enemy.ToString();
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)EDamageType.Body;
					m_MouthLifeDrain = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MouthLifeDrain);
				}
				return m_MouthLifeDrain;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class MouthBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MouthBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool SpawnAdd = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				SpawnAdd = false;
				if (!RemoveAdds)
				{
					foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
					{
						if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "MouthAdd")
							npc.Die(Body);
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if (SpawnAdd==false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(MouthAdds), UtilCollection.Random(20000, 35000));
					SpawnAdd = true;
                }
			}
			base.Think();
		}
		private int MouthAdds(ECSGameTimer timer)
        {
			if (HasAggro && Body.IsAlive)
			{
				GameNpc add = new GameNpc();
				add.Name = Body.Name + "'s minion";
				add.Model = 584;
				add.Size = (byte)UtilCollection.Random(45, 55);
				add.Level = (byte)UtilCollection.Random(55, 59);
				add.Strength = 150;
				add.Quickness = 80;
				add.MeleeDamageType = EDamageType.Thrust;
				add.MaxSpeedBase = 225;
				add.PackageID = "MouthAdd";
				add.RespawnInterval = -1;
				add.X = Body.X + UtilCollection.Random(-100, 100);
				add.Y = Body.Y + UtilCollection.Random(-100, 100);
				add.Z = Body.Z;
				add.CurrentRegion = Body.CurrentRegion;
				add.Heading = Body.Heading;
				add.Faction = FactionMgr.GetFactionByID(18);
				add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(18));
				StandardMobBrain brain = new StandardMobBrain();
				add.SetOwnBrain(brain);
				brain.AggroRange = 600;
				brain.AggroLevel = 100;
				add.AddToWorld();
			}
			SpawnAdd = false;
			return 0;
        }
	}
}
