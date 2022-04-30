using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS;
using System.Collections.Generic;

namespace DOL.GS
{
	public class Orylle : GameEpicBoss
	{
		public Orylle() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Orylle Initializing...");
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
			get { return 50000; }
		}
		public override bool AddToWorld()
		{
			Model = 861;
			Level = 83;
			Name = "Orylle";
			Size = 175;
			ParryChance = 70;

			Strength = 300;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			OrylleBrain sbrain = new OrylleBrain();
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
	public class OrylleBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OrylleBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
        #region Throw Player
        List<GamePlayer> Port_Enemys = new List<GamePlayer>();
		public static bool IsTargetPicked = false;
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		public int ThrowPlayer(ECSGameTimer timer)
		{
			if (Body.IsAlive)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
						{
							if (!Port_Enemys.Contains(player))
							{
								//if (player != Body.TargetObject)
									Port_Enemys.Add(player);
							}
						}
					}
				}
				if (Port_Enemys.Count > 0)
				{
					GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
					RandomTarget = Target;
					if (RandomTarget.IsAlive && RandomTarget != null)
					{
						RandomTarget.MoveTo(61, 31406, 69599, 15605, 2150);
						Port_Enemys.Remove(RandomTarget);
					}
				}
				RandomTarget = null;//reset random target to null
				IsTargetPicked = false;
			}
			return 0;
		}
        #endregion
        public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				RandomTarget = null;//throw
				IsTargetPicked = false;//throw
			}
			if (HasAggro)
			{
				if (IsTargetPicked == false && OrshomFire.FireCount > 0)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ThrowPlayer), Util.Random(15000, 20000));//timer to port and pick player
					IsTargetPicked = true;
				}
				if(IsPulled==false)
                {
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.Brain is OrshomBrain brain)
							{
								if (!brain.HasAggro)
									AddAggroListTo(brain);
								IsPulled = true;
							}
						}
					}
				}
			}
			base.Think();
		}
	}
}
/////////////////////////////////////////////////////////////Orshom////////////////////////////////
namespace DOL.GS
{
	public class Orshom : GameEpicBoss
	{
		public Orshom() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Orshom Brong Initializing...");
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
			get { return 40000; }
		}
		public override bool AddToWorld()
		{
			Model = 919;
			Level = 80;
			Name = "Orshom Brong";
			Size = 175;
			ParryChance = 50;

			Strength = 280;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			OrshomBrain sbrain = new OrshomBrain();
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
	public class OrshomBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OrshomBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool Spawn_Fire = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				Spawn_Fire = false;
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is OrshomFireBrain)
						{
							npc.RemoveFromWorld();
							OrshomFire.FireCount = 0;
						}
					}
				}
			}
			if (HasAggro)
			{
				if (Spawn_Fire == false)
				{
					SpawnFire();
					Spawn_Fire = true;
				}
				if(Body.HealthPercent <=50)
                {
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.Brain is OrylleBrain brain)
							{
								if(!brain.HasAggro)
									AddAggroListTo(brain);
							}
						}
					}
				}
			}
			base.Think();
		}
		public void SpawnFire()
		{
			foreach (GameNPC mob in Body.GetNPCsInRadius(8000))
			{
				if (mob.Brain is OrshomFireBrain)
				{
					return;
				}
			}
			OrshomFire npc = new OrshomFire();
			npc.X = 31406;
			npc.Y = 69599;
			npc.Z = 15605;
			npc.Heading = 2150;
			npc.CurrentRegion = Body.CurrentRegion;
			npc.RespawnInterval = -1;
			npc.AddToWorld();
		}
	}
}
///////////////////////////////////////////////////////////Fire pit mob///////////////////////////////////////////////////
namespace DOL.GS
{
	public class OrshomFire : GameEpicNPC
	{
		public OrshomFire() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Orshom's Fire Initializing...");
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
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (damageType == eDamageType.Cold)//only cold dmg can hit it
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
				else //no dmg
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GamePet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(this.Name + " is immune to your damage!", eChatType.CT_System,
							eChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
			}
		}
		public override void StartAttack(GameObject target)
        {
        }
        public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.15;
		}
		public override int MaxHealth
		{
			get { return 10000; }
		}
		public static int FireCount = 0;
        public override void Die(GameObject killer)
        {
			--FireCount;
            base.Die(killer);
        }
        public override bool AddToWorld()
		{
			Model = 665;
			Level = 80;
			Name = "Orshom's Fire";
			Size = 100;
			Dexterity = 200;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 200;
			++FireCount;

			MaxSpeedBase = 0;
			RespawnInterval = -1;

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			OrshomFireBrain sbrain = new OrshomFireBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
			}
			return success;
		}
		protected int Show_Effect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in this.GetPlayersInRadius(8000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 7025, 0, false, 0x01);
				}
				SetGroundTarget(X, Y, Z);
				CastSpell(Fire_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			return 0;
		}
		protected int DoCast(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			}
			return 0;
		}
		private Spell m_Fire_aoe;
		private Spell Fire_aoe
		{
			get
			{
				if (m_Fire_aoe == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 7025;
					spell.Icon = 7025;
					spell.TooltipId = 7025;
					spell.Damage = 500;
					spell.Name = "Fire Burn";
					spell.Radius = 300; 
					spell.Range = 240;
					spell.SpellID = 11751;
					spell.Target = eSpellTarget.Area.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Fire_aoe = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Fire_aoe);
				}

				return m_Fire_aoe;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class OrshomFireBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OrshomFireBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 2500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}