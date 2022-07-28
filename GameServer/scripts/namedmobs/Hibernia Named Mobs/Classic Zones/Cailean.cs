using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Cailean : GameEpicBoss
	{
		public Cailean() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Cailean Initializing...");
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
						damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit ||
						damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GamePet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(Name + " is too far away from it's habbitat and is immune to your damage!", eChatType.CT_System,
								eChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
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
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is CaileanBrain)
					return false;
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158846);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			CaileanBrain sbrain = new CaileanBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is WalkingTreeBrain)
					npc.RemoveFromWorld();
			}
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is WalkingTree2Brain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class CaileanBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CaileanBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		private bool CanSpawnTree = false;
		private bool RemoveTrees = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnTree = false;
				if (!RemoveTrees)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is WalkingTreeBrain)
							npc.RemoveFromWorld();
					}
					foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is WalkingTree2Brain)
							npc.RemoveFromWorld();
					}
					RemoveTrees = true;
				}
			}
			if(HasAggro && Body.TargetObject != null)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				RemoveTrees = false;
				if (!CanSpawnTree)
                {
					SpawnTree();
					SpawnTree2();
					CanSpawnTree = true;
                }
				if(Body.TargetObject != null)
                {					
					if(Util.Chance(20) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff))
						Body.CastSpell(TreeRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (Util.Chance(20) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.MezImmunity) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.Mez))
						Body.CastSpell(BossMezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive)
					{
						if (npc.Brain is WalkingTreeBrain brain)
						{
							if (brain != null && target != null && !brain.HasAggro && target.IsAlive)
								brain.AddToAggroList(target, 10);
						}
						if (npc.Brain is WalkingTree2Brain brain2)
						{
							if (brain2 != null && target != null && !brain2.HasAggro && target.IsAlive)
								brain2.AddToAggroList(target, 10);
						}
					}
				}
			}
			if (Body.HealthPercent <= 30)
				Body.CastSpell(CaileanHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			base.Think();
		}
        #region Spawn Trees
        private void SpawnTree()
        {
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is WalkingTreeBrain)
					return;
			}
			for (int i = 0; i < Util.Random(4, 5); i++)
			{
				WalkingTree tree = new WalkingTree();
				tree.X = Body.X + Util.Random(-500, 500);
				tree.Y = Body.Y + Util.Random(-500, 500);
				tree.Z = Body.Z;
				tree.Heading = Body.Heading;
				tree.CurrentRegion = Body.CurrentRegion;
				tree.AddToWorld();
			}
		}
		private void SpawnTree2()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is WalkingTree2Brain)
					return;
			}
			Point3D point1 = new Point3D(479778, 508293, 4534);
			Point3D point2 = new Point3D(478647, 508450, 4639);
			Point3D point3 = new Point3D(479444, 508548, 4532);
			for (int i = 0; i < Util.Random(8, 10); i++)
			{
				WalkingTree2 tree = new WalkingTree2();
				switch(Util.Random(1,3))
                {
					case 1:
						tree.X = point1.X + Util.Random(-200, 200);
						tree.Y = point1.Y + Util.Random(-200, 200);
						tree.Z = point1.Z;
						break;
					case 2:
						tree.X = point2.X + Util.Random(-200, 200);
						tree.Y = point2.Y + Util.Random(-200, 200);
						tree.Z = point2.Z;
						break;
					case 3:
						tree.X = point3.X + Util.Random(-200, 200);
						tree.Y = point3.Y + Util.Random(-200, 200);
						tree.Z = point3.Z;
						break;
				}
				tree.Heading = Body.Heading;
				tree.CurrentRegion = Body.CurrentRegion;
				tree.AddToWorld();
			}
		}
        #endregion
        #region Spells
        private Spell m_CaileanHeal;
		private Spell CaileanHeal
		{
			get
			{
				if (m_CaileanHeal == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 6;
					spell.ClientEffect = 1340;
					spell.Icon = 1340;
					spell.TooltipId = 1340;
					spell.Value = 400;
					spell.Name = "Cailean's Heal";
					spell.Range = 1500;
					spell.SpellID = 11902;
					spell.Target = "Self";
					spell.Type = eSpellType.Heal.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_CaileanHeal = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CaileanHeal);
				}
				return m_CaileanHeal;
			}
        }
		private Spell m_TreeRoot;
		private Spell TreeRoot
		{
			get
			{
				if (m_TreeRoot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5208;
					spell.Icon = 5208;
					spell.TooltipId = 5208;
					spell.Value = 99;
					spell.Duration = 70;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Root";
					spell.Range = 1500;
					spell.SpellID = 11979;
					spell.Target = "Enemy";
					spell.Type = eSpellType.SpeedDecrease.ToString();
					m_TreeRoot = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot);
				}
				return m_TreeRoot;
			}
		}
		private Spell m_BossmezSpell;
		private Spell BossMezz
		{
			get
			{
				if (m_BossmezSpell == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5318;
					spell.Icon = 5318;
					spell.TooltipId = 5318;
					spell.Name = "Mesmerized";
					spell.Range = 1500;
					spell.SpellID = 11980;
					spell.Duration = 60;
					spell.Target = "Enemy";
					spell.Type = "Mesmerize";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Spirit; //Spirit DMG Type
					m_BossmezSpell = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BossmezSpell);
				}
				return m_BossmezSpell;
			}
		}
		#endregion
	}
}
////////////////////////////////////////////////////////adds///////////////////////////////////////
#region Cailean's Trees 4-5 yellows
namespace DOL.GS
{
	public class WalkingTree : GameNPC
	{
		public WalkingTree() : base()
		{
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15;// dmg reduction for melee dmg
				case eDamageType.Crush: return 15;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
				default: return 15;// dmg reduction for rest resists
			}
		}
		public override int MaxHealth
		{
			get { return 2000; }
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
		public override bool AddToWorld()
		{
			Model = 1703;
			Name = "walking tree";
			Level = (byte)Util.Random(48, 50);
			Size = (byte)Util.Random(50, 55);
			RespawnInterval = -1;
			RoamingRange = 200;

			LoadedFromScript = true;
			WalkingTreeBrain sbrain = new WalkingTreeBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class WalkingTreeBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WalkingTreeBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if(HasAggro && Body.TargetObject != null)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				if(Util.Chance(20) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff))
					Body.CastSpell(TreeRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				if (Util.Chance(20) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
					Body.CastSpell(CaileanTree_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			base.Think();
		}
        #region Spells
        private Spell m_TreeRoot;
		private Spell TreeRoot
		{
			get
			{
				if (m_TreeRoot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5208;
					spell.Icon = 5208;
					spell.TooltipId = 5208;
					spell.Value = 99;
					spell.Duration = 70;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Root";
					spell.Range = 1500;
					spell.SpellID = 11901;
					spell.Target = "Enemy";
					spell.Type = eSpellType.SpeedDecrease.ToString();
					m_TreeRoot = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot);
				}
				return m_TreeRoot;
			}
		}
		private Spell m_CaileanTree_Dot;
		private Spell CaileanTree_Dot
		{
			get
			{
				if (m_CaileanTree_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3411;
					spell.Icon = 3411;
					spell.Name = "Poison";
					spell.Description = "Inflicts 60 damage to the target every 4 sec for 20 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 3411;
					spell.Range = 1500;
					spell.Damage = 60;
					spell.Duration = 20;
					spell.Frequency = 40;
					spell.SpellID = 11978;
					spell.Target = "Enemy";
					spell.SpellGroup = 1800;
					spell.EffectGroup = 1500;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_CaileanTree_Dot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CaileanTree_Dot);
				}
				return m_CaileanTree_Dot;
			}
		}
        #endregion
    }
}
#endregion

#region Cailean's Trees 8-10 blue
namespace DOL.GS
{
	public class WalkingTree2 : GameNPC
	{
		public WalkingTree2() : base()
		{
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15;// dmg reduction for melee dmg
				case eDamageType.Crush: return 15;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
				default: return 15;// dmg reduction for rest resists
			}
		}
		public override int MaxHealth
		{
			get { return 1500; }
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
		public override bool AddToWorld()
		{
			Model = 1703;
			Name = "rotted tree";
			Level = (byte)Util.Random(40, 44);
			Size = (byte)Util.Random(40, 50);
			RespawnInterval = -1;
			MaxSpeedBase = 0;

			LoadedFromScript = true;
			WalkingTree2Brain sbrain = new WalkingTree2Brain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class WalkingTree2Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WalkingTree2Brain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				GameLiving target = Body.TargetObject as GameLiving;
				if (Util.Chance(20))
				{
					if(target.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity) && target != null && target.IsAlive)
                    {
						var effect = EffectListService.GetEffectOnTarget(target, eEffect.SnareImmunity);
						if(effect != null)
							EffectService.RequestImmediateCancelEffect(effect);//remove snare immunity here
					}
					if(!target.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff) && target != null && target.IsAlive)
						Body.CastSpell(TreeRoot2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				}
			}
			base.Think();
		}
		#region Spells
		private Spell m_TreeRoot;
		private Spell TreeRoot
		{
			get
			{
				if (m_TreeRoot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 30;
					spell.ClientEffect = 5208;
					spell.Icon = 5208;
					spell.TooltipId = 5208;
					spell.Value = 99;
					spell.Duration = 70;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Root";
					spell.Range = 4500;
					spell.SpellID = 11981;
					spell.Target = "Enemy";
					spell.Type = eSpellType.SpeedDecrease.ToString();
					m_TreeRoot = new Spell(spell, 40);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot);
				}
				return m_TreeRoot;
			}
		}
		private Spell m_TreeRoot2;
		private Spell TreeRoot2
		{
			get
			{
				if (m_TreeRoot2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 10;
					spell.ClientEffect = 5208;
					spell.Icon = 5208;
					spell.TooltipId = 5208;
					spell.Value = 40;
					spell.Duration = 20;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Snare";
					spell.Range = 4500;
					spell.SpellID = 11982;
					spell.Target = "Enemy";
					spell.Type = eSpellType.SpeedDecrease.ToString();
					m_TreeRoot2 = new Spell(spell, 40);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot2);
				}
				return m_TreeRoot2;
			}
		}
		#endregion
	}
}
#endregion