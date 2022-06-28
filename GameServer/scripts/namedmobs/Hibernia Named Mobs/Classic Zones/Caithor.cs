using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Caithor : GameEpicBoss
	{
		public Caithor() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Caithor Initializing...");
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
			get { return 20000; }
		}
		public static bool RealCaithorUp = false;
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50023);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RealCaithorUp = true;

			SpawnDorochas();
			CaithorDorocha.DorochaKilled = 0;
			CaithorBrain sbrain = new CaithorBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		private void SpawnDorochas()
		{
			for (int i = 0; i < 4; i++)
			{
				GameNPC npc = new GameNPC();
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160718);
				npc.LoadTemplate(npcTemplate);
				npc.X = X + Util.Random(-100, 100);
				npc.Y = Y + Util.Random(-100, 100);
				npc.Z = Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.PackageID = "RealCaithorDorocha";
				npc.RespawnInterval = -1;
				npc.AddToWorld();
			}
		}
		public override void Die(GameObject killer)
        {
			RealCaithorUp = false;
			foreach(GameNPC npc in GetNPCsInRadius(8000))
            {
				if (npc.IsAlive && npc != null && npc.PackageID == "RealCaithorDorocha")
					npc.Die(this);
            }
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class CaithorBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CaithorBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 450;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}
#region Ghost of Caithor
namespace DOL.GS
{
	public class GhostOfCaithor : GameEpicNPC
	{
		public GhostOfCaithor() : base() { }
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
			get { return 10000; }
		}
		#region Stats
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 400; }
		#endregion
		public static bool GhostCaithorUP = false;
		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is GhostOfCaithorBrain)
					return false;
			}
			Name = "Ghost of Caithor";
			Level = (byte)Util.Random(62, 65);
			Model = 339;
			Size = 80;
			Flags = eFlags.GHOST;
			LoadEquipmentTemplateFromDatabase("65b95161-a813-41cb-be0c-a57d132f8173");
			GhostCaithorUP = true;

			GhostOfCaithorBrain sbrain = new GhostOfCaithorBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			GhostCaithorUP = false;
			SpawnCaithor();
            base.Die(killer);
        }
		private void SpawnCaithor()
		{
			Caithor npc = new Caithor();
			npc.X = 470547;
			npc.Y = 531497;
			npc.Z = 4984;
			npc.Heading = 3319;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class GhostOfCaithorBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GhostOfCaithorBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		public override void Think()
		{
			if (CaithorDorocha.DorochaKilled >= 5 && !Caithor.RealCaithorUp)
			{
				if (changed)
				{
					Body.Flags = oldFlags;
					Body.Model = oldModel;
					changed = false;
				}
			}
			else
			{
				if (changed == false)
				{
					oldFlags = Body.Flags;
					Body.Flags ^= GameNPC.eFlags.CANTTARGET;
					Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
					Body.Flags ^= GameNPC.eFlags.PEACE;

					if (oldModel == 0)
						oldModel = Body.Model;

					Body.Model = 1;
					changed = true;
				}
			}
			base.Think();
		}
	}
}
#endregion

#region Caithor far dorochas
namespace DOL.GS
{
	public class CaithorDorocha : GameNPC
	{
		public CaithorDorocha() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160718);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			EquipmentTemplateID = "65b95161-a813-41cb-be0c-a57d132f8173";

			CaithorDorochaBrain sbrain = new CaithorDorochaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public static int DorochaKilled = 0;
        public override void Die(GameObject killer)
        {
			if(!Caithor.RealCaithorUp)
			++DorochaKilled;
            base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class CaithorDorochaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CaithorDorochaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion