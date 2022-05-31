using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class MelancholicFairyQueen : GameEpicBoss
	{
		public MelancholicFairyQueen() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Melancholic Fairy Queen Initializing...");
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
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
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 400; }
		#endregion
		public static bool IsKilled = false;
		public override bool AddToWorld()
		{
			BroadcastMessage("You hear the sound of trumpets in the distance.");
			Name = "Melancholic Fairy Queen";
			Model = 679;
			Level = (byte)Util.Random(64,68);
			Size = 50;
			MaxDistance = 2500;
			TetherRange = 2600;
			Flags = eFlags.FLYING;
			MaxSpeedBase = 250;
			CreateFairyGuards();
			IsKilled = false;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			MelancholicFairyQueenBrain sbrain = new MelancholicFairyQueenBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			IsKilled = true;
			foreach (GameNPC adds in GetNPCsInRadius(8000))
			{
				if (adds != null && adds.IsAlive && adds.Brain is MFQGuardsBrain)
					adds.RemoveFromWorld();
			}
			base.Die(killer);
        }
        private void CreateFairyGuards()
        {
			for (int i = 0; i < 4; i++)
			{
				MFQGuards guards = new MFQGuards();
				guards.X = X + Util.Random(-500, 500);
				guards.Y = Y + Util.Random(-500, 500);
				guards.Z = Z;
				guards.Heading = Heading;
				guards.CurrentRegion = CurrentRegion;
				guards.AddToWorld();
			}
        }
	}
}
namespace DOL.AI.Brain
{
	public class MelancholicFairyQueenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MelancholicFairyQueenBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				Body.MaxSpeedBase = 250;
			}
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is MFQGuardsBrain brain)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && target.IsAlive && target != null)
							brain.AddToAggroList(target, 10);
					}
				}
				if (Util.Chance(50) && !Body.IsCasting)
					Body.CastSpell(MFQDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			base.Think();
		}
        #region Spell
        private Spell m_MFQDD;
		public Spell MFQDD
		{
			get
			{
				if (m_MFQDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 8;
					spell.Power = 0;
					spell.ClientEffect = 4111;
					spell.Icon = 4111;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Heat Beam";
					spell.Range = 1500;
					spell.SpellID = 11896;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_MFQDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MFQDD);
				}
				return m_MFQDD;
			}
		}
        #endregion
    }
}
#region Melancholic Fairy Queen Controller - controlls when queen will spawn/despawn
namespace DOL.GS
{
	public class MFQController : GameNPC
	{
		public MFQController() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "MFQ Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)60;

			MFQControllerBrain sbrain = new MFQControllerBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class MFQControllerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public MFQControllerBrain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public override void Think()
		{
			uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
			uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
			//log.Warn("Current time: " + hour + ":" + minute);
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is MelancholicFairyQueenBrain brain)
				{
					if (hour >= 24)
					{
						npc.RemoveFromWorld();

						foreach (GameNPC adds in Body.GetNPCsInRadius(8000))
						{
							if (adds != null && adds.IsAlive && adds.Brain is MFQGuardsBrain)
								adds.RemoveFromWorld();
						}
					}
				}
			}
			if (hour == 20 && !MelancholicFairyQueen.IsKilled)
				SpawnQueen();

			base.Think();
		}
		public void SpawnQueen()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is MelancholicFairyQueenBrain)
					return;
			}
			MelancholicFairyQueen boss = new MelancholicFairyQueen();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.AddToWorld();
		}
	}
}
#endregion

#region Fairy Queen Guards
namespace DOL.GS
{
	public class MFQGuards : GameNPC
	{
		public MFQGuards() : base()
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
			get { return 2200; }
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
			Model = 679;
			Name = "melancholic fairy guard";
			Level = (byte)Util.Random(51, 55);
			Size = (byte)Util.Random(50, 55);
			RespawnInterval = -1;
			RoamingRange = 200;
			Flags = eFlags.FLYING;

			LoadedFromScript = true;
			MFQGuardsBrain sbrain = new MFQGuardsBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MFQGuardsBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MFQGuardsBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion