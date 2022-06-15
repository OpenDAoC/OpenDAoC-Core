using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

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
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158846);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			CaileanBrain sbrain = new CaileanBrain();
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
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnTree = false;
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is WalkingTreeBrain)
						npc.RemoveFromWorld();
				}
			}
			if(HasAggro)
            {
				if(!CanSpawnTree)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CreateTree), Util.Random(25000, 35000));
					CanSpawnTree = true;
                }
            }
			if (Body.HealthPercent <= 30)
				Body.CastSpell(CaileanHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			base.Think();
		}
		private int CreateTree(ECSGameTimer timer)
        {
			if (HasAggro)
				SpawnTree();
			CanSpawnTree = false;
			return 0;
        }
		private void SpawnTree()
        {
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is WalkingTreeBrain)
					return;
			}
			WalkingTree tree = new WalkingTree();
			tree.X = Body.X + Util.Random(-500, 500);
			tree.Y = Body.Y + Util.Random(-500, 500);
			tree.Z = Body.Z;
			tree.Heading = Body.Heading;
			tree.CurrentRegion = Body.CurrentRegion;
			tree.AddToWorld();
		}
		private Spell m_CaileanHeal;
		private Spell CaileanHeal
		{
			get
			{
				if (m_CaileanHeal == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 10;
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
					m_CaileanHeal = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CaileanHeal);
				}
				return m_CaileanHeal;
			}
		}
	}
}
////////////////////////////////////////////////////////adds///////////////////////////////////////
#region Cailean's Trees
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
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int MaxHealth
		{
			get { return 2500; }
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
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		public override bool AddToWorld()
		{
			Model = 1703;
			Name = "walking tree";
			Level = (byte)Util.Random(53, 55);
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
				if(!target.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff))
					Body.CastSpell(TreeRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			base.Think();
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
					spell.SpellID = 11901;
					spell.Target = "Enemy";
					spell.Type = eSpellType.SpeedDecrease.ToString();
					m_TreeRoot = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot);
				}
				return m_TreeRoot;
			}
		}
	}
}
#endregion