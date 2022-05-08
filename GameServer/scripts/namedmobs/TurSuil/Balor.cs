using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Balor : GameEpicBoss
	{
		public Balor() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40; // dmg reduction for melee dmg
				case eDamageType.Crush: return 40; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
				default: return 70; // dmg reduction for rest resists
			}
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
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GamePet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(this.Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else//take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
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
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158225);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			BalorBrain.spawn_eye = false;
			Faction = FactionMgr.GetFactionByID(93);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
			IsCloakHoodUp = true;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 841, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			BalorBrain sbrain = new BalorBrain();
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
	public class BalorBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BalorBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool spawn_eye = false;
		public void SpawnEyeOfBalor()
		{
			if (BalorEye.EyeCount == 0)
			{
				BalorEye Add1 = new BalorEye();
				Add1.X = Body.X;
				Add1.Y = Body.Y + 80;
				Add1.Z = Body.Z + 347;//at mob head heigh with boss size 105
				Add1.CurrentRegion = Body.CurrentRegion;
				Add1.Heading = Body.Heading;
				Add1.RespawnInterval = -1;
				Add1.AddToWorld();
			}
		}
		private int m_stage = 10;
		/// <summary>
		/// This keeps track of the stage the encounter is in.
		/// </summary>
		private int Stage
		{
			get { return m_stage; }
			set
			{
				if (value >= 0 && value <= 10) m_stage = value;
			}
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.HealthPercent == 100 && Stage < 10 && !HasAggro)
				Stage = 10;

			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				int health = Body.HealthPercent / 10;
				if(health < Stage)
                {
					switch (health)
					{
						case 1:
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:	
							{
								SpawnEyeOfBalor();
							}
							break;
					}
					Stage = health;
				}

			}
			base.Think();
		}
	}
}
/// <summary>
/// //////////////////////////////////////////////////////////////////Balor Eye//////////////////////////////////////////////////////////////////
/// </summary>
namespace DOL.AI.Brain
{
	public class BalorEyeBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BalorEyeBrain()
			: base()
		{
			AggroLevel = 0;
			AggroRange = 1500;
			ThinkInterval = 500;
		}
		public static bool PickTarget = false;
		public static bool Cancast = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override void Think()
		{
			if (Body.IsAlive)
			{
				if (PickTarget == false)
				{
					PickRandomTarget();
					PickTarget = true;
				}
				if (HasAggro && Cancast) //&& RandomTarget != null && RandomTarget.IsAlive && !Body.IsCasting)
				{
					Body.TargetObject = RandomTarget;
					Body.CastSpell(EyeDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.Think();
		}
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
		public void PickRandomTarget()
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_DD.Contains(player) && !AggroTable.ContainsKey(player))
						{
							Enemys_To_DD.Add(player);
							AddToAggroList(player, 10);//make sure it will cast spell
						}
					}
				}
			}
			if (Enemys_To_DD.Count > 0)
			{
				GamePlayer Target = Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
				RandomTarget = Target;//set random target to static RandomTarget
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(StartCast), 3000);
			}
		}
		public int StartCast(ECSGameTimer timer)
        {
			Cancast = true;
			return 0;
        }
		private Spell m_EyeDD;
		private Spell EyeDD
		{
			get
			{
				if (m_EyeDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 4;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4111;
					spell.Icon = 4111;
					spell.TooltipId = 4111;
					spell.Damage = 1000;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Balor's Eye Light";
					spell.Range = 1800;
					spell.SpellID = 11791;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_EyeDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_EyeDD);
				}
				return m_EyeDD;
			}
		}
	}
}
namespace DOL.GS
{
	public class BalorEye : GameNPC
	{
		public override int MaxHealth
		{
			get { return 5000; }
		}
        public override void StartAttack(GameObject target)
        {
        }
		public static int EyeCount = 0;
        public override void Die(GameObject killer)
        {
			--EyeCount;
            base.Die(killer);
        }
        public override bool AddToWorld()
		{
			Model = 665;
			Name = "Eye of Balor";
			Strength = 80;
			Dexterity = 200;
			Quickness = 100;
			Constitution = 100;
			RespawnInterval = -1;
			MaxSpeedBase = 0;
			Flags ^= eFlags.FLYING;
			Flags ^= eFlags.CANTTARGET;
			Flags ^= eFlags.DONTSHOWNAME;
			//Flags ^= eFlags.STATUE;

			BalorEyeBrain.PickTarget = false;
			BalorEyeBrain.RandomTarget = null;
			BalorEyeBrain.Cancast = false;
			++EyeCount;
			Size = 20;
			Level = (byte)Util.Random(65, 70);
			Faction = FactionMgr.GetFactionByID(93);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));//minions of balor
			BalorEyeBrain eye = new BalorEyeBrain();
			SetOwnBrain(eye);
			eye.Start();
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(RemoveEye),18200); //mob will be removed after this time
			}
			return success;
		}
		protected int RemoveEye(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				Die(this);
			}
			return 0;
		}
	}
}