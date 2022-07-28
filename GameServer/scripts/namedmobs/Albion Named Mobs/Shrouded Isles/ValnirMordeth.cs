using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class ValnirMordeth : GameEpicBoss
	{
		public ValnirMordeth() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Valnir Mordeth Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000014);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
			canSpawnAdds = false;
			if(canSpawnAdds==false)
            {
				SpawnAdds();
				canSpawnAdds = true;
            }
			ValnirMordethBrain sbrain = new ValnirMordethBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is ValnirMordethAddBrain)
					npc.Die(this);
			}
			base.Die(killer);
        }
        private bool canSpawnAdds = false;
		private void SpawnAdds()
        {
			for (int i = 0; i < Util.Random(2, 3); i++)
			{
				ValnirMordethAdd add = new ValnirMordethAdd();
				add.X = X + Util.Random(-200, 200);
				add.Y = Y + Util.Random(-200, 200);
				add.Z = Z;
				add.Heading = Heading;
				add.CurrentRegion = CurrentRegion;
				add.PackageID = "MordethBaf";
				add.AddToWorld();
			}
		}
		public override void OnAttackEnemy(AttackData ad)
		{
			if (Util.Chance(30))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(ValnirLifeDrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		public override void DealDamage(AttackData ad)
		{
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0)
				Health += ad.Damage;
			base.DealDamage(ad);
		}
		private Spell m_ValnirLifeDrain;
		private Spell ValnirLifeDrain
		{
			get
			{
				if (m_ValnirLifeDrain == null)
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
					spell.Range = 400;
					spell.SpellID = 11903;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Body;
					m_ValnirLifeDrain = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ValnirLifeDrain);
				}
				return m_ValnirLifeDrain;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class ValnirMordethBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ValnirMordethBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool CanSpawnMoreGhouls = false;
		private bool spawnAddsAfterCombat = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnMoreGhouls = false;
				if(spawnAddsAfterCombat == false)
                {
					SpawnAddsAfterCombat();
					spawnAddsAfterCombat = true;
				}
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is ValnirMordethAddBrain && npc.PackageID == "MordethAdds")
							npc.Die(Body);
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				spawnAddsAfterCombat = false;
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "MordethBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
				GameLiving target = Body.TargetObject as GameLiving;
				if (target != null)
				{
					if (Body.IsCasting)
					{
						if (Body.attackComponent.AttackState)
							Body.attackComponent.NPCStopAttack();
						if (Body.IsMoving)
							Body.StopFollowing();
					}
					Body.TurnTo(Body.TargetObject);
					if (Util.Chance(25) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
						Body.CastSpell(Valnir_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
					if (Util.Chance(25) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(eEffect.Disease))
						Body.CastSpell(ValnirDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				}
				if(CanSpawnMoreGhouls == false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnAdds), Util.Random(25000, 35000));
					CanSpawnMoreGhouls = true;
                }
			}
			base.Think();
		}
		private int SpawnAdds(ECSGameTimer timer)
		{
			if (Body.IsAlive && HasAggro && ValnirMordethAdd.EssenceGhoulCount == 0)
			{
				ValnirMordethAdd add = new ValnirMordethAdd();
				add.X = Body.X + Util.Random(-100, 100);
				add.Y = Body.Y + Util.Random(-100, 100);
				add.Z = Body.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.PackageID = "MordethAdds";
				add.AddToWorld();
			}
			CanSpawnMoreGhouls = false;
			return 0;
		}
		private void SpawnAddsAfterCombat()
        {
			if (!HasAggro && ValnirMordethAdd.EssenceGhoulCount == 0)
			{
				for (int i = 0; i < Util.Random(2, 3); i++)
				{
					ValnirMordethAdd add = new ValnirMordethAdd();
					add.X = Body.SpawnPoint.X + Util.Random(-100, 100);
					add.Y = Body.SpawnPoint.Y + Util.Random(-100, 100);
					add.Z = Body.SpawnPoint.Z;
					add.Heading = Body.Heading;
					add.CurrentRegion = Body.CurrentRegion;
					add.PackageID = "MordethBaf";
					add.AddToWorld();
				}
			}
		}
		#region Spells
		private Spell m_Valnir_Dot;
		private Spell Valnir_Dot
		{
			get
			{
				if (m_Valnir_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 562;
					spell.Icon = 562;
					spell.Name = "Valnir Modreth's Breath";
					spell.Description = "Inflicts 80 damage to the target every 4 sec for 40 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 562;
					spell.Range = 1500;
					spell.Damage = 80;
					spell.Duration = 40;
					spell.Frequency = 40;
					spell.SpellID = 11903;
					spell.Target = "Enemy";
					spell.SpellGroup = 1800;
					spell.EffectGroup = 1500;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Valnir_Dot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Valnir_Dot);
				}
				return m_Valnir_Dot;
			}
		}
		private Spell m_ValnirDisease;
		private Spell ValnirDisease
		{
			get
			{
				if (m_ValnirDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 40;
					spell.ClientEffect = 731;
					spell.Icon = 731;
					spell.Name = "Valnir Mordeth's Plague";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.TooltipId = 731;
					spell.Range = 1500;
					spell.Duration = 120;
					spell.SpellID = 11904;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
					m_ValnirDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ValnirDisease);
				}
				return m_ValnirDisease;
			}
		}
        #endregion
    }
}
/////////////////////////////////////////////////////////////////////Adds//////////////////////////////////////////////////
namespace DOL.GS
{
	public class ValnirMordethAdd : GameNPC
	{
		public ValnirMordethAdd() : base() { }

		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override int MaxHealth
		{
			get { return 5000; }
		}
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 120; }
		public static int EssenceGhoulCount = 0;
        public override bool AddToWorld()
		{
			Model = 921;
			Name = "Essence Ghoul";
			Level = (byte)Util.Random(62, 66);
			Size = (byte)Util.Random(55, 65);
			MaxSpeedBase = 225;
			RespawnInterval = -1;
			++EssenceGhoulCount;

			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			ValnirMordethAddBrain sbrain = new ValnirMordethAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
        public override long ExperienceValue => 0;
        public override void DropLoot(GameObject killer)
        {
        }
        public override void Die(GameObject killer)
        {
			--EssenceGhoulCount;
            base.Die(killer);
        }
        public override void DealDamage(AttackData ad)
		{
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0)
			{
				foreach(GameNPC boss in GetNPCsInRadius(5000))
                {
					if (boss != null && boss.IsAlive && boss.Brain is ValnirMordethBrain)
						boss.Health += ad.Damage;//heal boss
                }
				Health += ad.Damage;//heal self
			}
			base.DealDamage(ad);
		}
	}
}
namespace DOL.AI.Brain
{
	public class ValnirMordethAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ValnirMordethAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				if (Body.IsCasting)
				{
					if (Body.attackComponent.AttackState)
						Body.attackComponent.NPCStopAttack();
					if (Body.IsMoving)
						Body.StopFollowing();
				}
					Body.TurnTo(Body.TargetObject);
					Body.CastSpell(ValnirAddLifeDrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				
			}
			base.Think();
		}
		private Spell m_ValnirAddLifeDrain;
		private Spell ValnirAddLifeDrain
		{
			get
			{
				if (m_ValnirAddLifeDrain == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = 0;
					spell.ClientEffect = 14352;
					spell.Icon = 14352;
					spell.TooltipId = 14352;
					spell.Damage = 350;
					spell.Name = "Lifedrain";
					spell.Range = 1800;
					spell.SpellID = 11902;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Body;
					m_ValnirAddLifeDrain = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ValnirAddLifeDrain);
				}
				return m_ValnirAddLifeDrain;
			}
		}
	}
}
