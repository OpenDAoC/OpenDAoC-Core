using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class SummonerRoesia : GameEpicBoss
	{
		public SummonerRoesia() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 70;// dmg reduction for melee dmg
				case eDamageType.Crush: return 70;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 70;// dmg reduction for melee dmg
				default: return 50;// dmg reduction for rest resists
			}
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (this.IsOutOfTetherRange)
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
			if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 1000;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.85;
		}
		public override int MaxHealth
		{
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18804);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			SummonerRoesiaBrain.RandomTarget = null;
			SummonerRoesiaBrain.CanCast = false;
			SummonerRoesiaBrain.spawn_bows = false;
			SummonerRoesiaBrain.IsPulled = false;
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));
			IsCloakHoodUp = true;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 139, 43, 0, 0); //Slot,model,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 43);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 43);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.Cloak, 57, 66, 0, 0);
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 43, 94, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			SummonerRoesiaBrain sbrain = new SummonerRoesiaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			GameNPC[] npcs;

			npcs = WorldMgr.GetNPCsByNameFromRegion("Summoner Roesia", 248, (eRealm)0);
			if (npcs.Length == 0)
			{
				log.Warn("Summoner Roesia not found, creating it...");

				log.Warn("Initializing Summoner Roesia...");
				SummonerRoesia OF = new SummonerRoesia();
				OF.Name = "Summoner Roesia";
				OF.Model = 6;
				OF.Realm = 0;
				OF.Level = 75;
				OF.Size = 65;
				OF.CurrentRegionID = 248;//OF summoners hall

				OF.Strength = 5;
				OF.Intelligence = 200;
				OF.Piety = 200;
				OF.Dexterity = 200;
				OF.Constitution = 100;
				OF.Quickness = 125;
				OF.Empathy = 300;
				OF.BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;
				OF.MeleeDamageType = eDamageType.Crush;
				OF.Faction = FactionMgr.GetFactionByID(187);
				OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));

				OF.X = 34577;
				OF.Y = 31371;
				OF.Z = 15998;
				OF.MaxDistance = 2000;
				OF.TetherRange = 1300;
				OF.MaxSpeedBase = 250;
				OF.Heading = 19;
				OF.IsCloakHoodUp = true;

				SummonerRoesiaBrain ubrain = new SummonerRoesiaBrain();
				ubrain.AggroLevel = 100;
				ubrain.AggroRange = 600;
				OF.SetOwnBrain(ubrain);
				OF.AddToWorld();
				OF.Brain.Start();
				OF.SaveIntoDatabase();
			}
			else
				log.Warn("Summoner Roesia exist ingame, remove it and restart server if you want to add by script code.");
		}
	}
}
namespace DOL.AI.Brain
{
	public class SummonerRoesiaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SummonerRoesiaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
		public static bool spawn_bows = false;
		public override void OnAttackedByEnemy(AttackData ad)
		{
			if(spawn_bows == false)
            {
				SpawnBows();
				spawn_bows = true;
            }
			if (IsPulled == false)
			{
				foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is RoesiaBowsBrain)
						{
							AddAggroListTo(npc.Brain as RoesiaBowsBrain);
							IsPulled = true;
						}
					}
				}
			}
			base.OnAttackedByEnemy(ad);
		}
		public void SpawnBows()
		{
			for (int i = 0; i < Util.Random(4,6); i++)
			{
				RoesiaBows Add1 = new RoesiaBows();
				Add1.X = Body.X + Util.Random(-300, 300);
				Add1.Y = Body.Y + Util.Random(-300, 300);
				Add1.Z = Body.Z;
				Add1.CurrentRegion = Body.CurrentRegion;
				Add1.Heading = 22;
				Add1.RespawnInterval = -1;
				Add1.AddToWorld();
			}
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				this.Body.Health = this.Body.MaxHealth;
				RandomTarget = null;
				CanCast = false;
				IsPulled = false;
				spawn_bows = false;
				if (Enemys_To_DD.Count > 0)
				{
					Enemys_To_DD.Clear();
				}
				foreach(GameNPC bows in Body.GetNPCsInRadius(5000))
                {
					if (bows != null)
                    {
						if(bows.IsAlive && bows.Brain is RoesiaBowsBrain)
                        {
							bows.RemoveFromWorld();
                        }
                    }
                }
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if (Body.HealthPercent < 25)
				{
					Body.CastSpell(RoesiaHOT, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast HOT
				}
				if (Body.TargetObject != null)
				{
					if(!Body.effectListComponent.ContainsEffectForEffectType(eEffect.DamageReturn))
                    {
						Body.CastSpell(RoesiaDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//Cast DS
					}
					PickRandomTarget();
				}
			}
			if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
			{
				this.Body.Health = this.Body.MaxHealth;
			}
			base.Think();
		}
		public int HotTimer(RegionTimer timer)
        {
			
			return 0;
        }
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		public static bool CanCast = false;
		List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
		public void PickRandomTarget()
        {
			foreach(GamePlayer player in Body.GetPlayersInRadius(2000))
            {
				if(player != null)
                {
					if(player.IsAlive && player.Client.Account.PrivLevel==1)
                    {
						if(!Enemys_To_DD.Contains(player))
                        {
							Enemys_To_DD.Add(player);
                        }
                    }
                }
            }
			if(Enemys_To_DD.Count>0)
            {
				if (CanCast == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					new RegionTimer(Body, new RegionTimerCallback(CastDot), 1000);
					CanCast = true;
				}				
			}
        }
		public int CastDot(RegionTimer timer)
        {
			GamePlayer oldTarget = (GamePlayer)Body.TargetObject;//old target
			if (RandomTarget != null && RandomTarget.IsAlive)
			{
				Body.TargetObject = RandomTarget;
				Body.TurnTo(RandomTarget);
				Body.StopFollowing();
				Body.CastSpell(RoesiaDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			Body.StartAttack(oldTarget);//start attack old target
			new RegionTimer(Body, new RegionTimerCallback(ResetDot), Util.Random(25000,35000));
			return 0;
        }
		public int ResetDot(RegionTimer timer)//reset here so boss can start dot again
        {
			RandomTarget = null;
			CanCast = false;
			return 0;
        }
        #region Roesia Spells
        private Spell m_RoesiaDot;
		public Spell RoesiaDot
		{
			get
			{
				if (m_RoesiaDot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 10;
					spell.ClientEffect = 585;
					spell.Icon = 585;
					spell.TooltipId = 585;
					spell.Damage = 150;
					spell.Frequency = 30;
					spell.Duration = 36;
					spell.DamageType = (int)eDamageType.Spirit;
					spell.Name = "Summoner Pain";
					spell.Description = "Inflicts 150 damage to the target every 3 sec for 36 seconds.";
					spell.Message1 = "Your body is covered with painful sores!";
					spell.Message2 = "{0}'s skin erupts in open wounds!";
					spell.Message3 = "The destructive energy wounding you fades.";
					spell.Message4 = "The destructive energy around {0} fades.";
					spell.Range = 1800;
					spell.Radius = 1000;
					spell.SpellID = 11756;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.Type = eSpellType.DamageOverTime.ToString();
					m_RoesiaDot = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RoesiaDot);
				}
				return m_RoesiaDot;
			}
		}
		private Spell m_RoesiaHOT;
		public Spell RoesiaHOT
		{
			get
			{
				if (m_RoesiaHOT == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 45;
					spell.ClientEffect = 4414;
					spell.Icon = 4414;
					spell.TooltipId = 4414;
					spell.Value = 125;
					spell.Frequency = 20;
					spell.Duration = 10;
					spell.Name = "Summoner Heal";
					spell.Description = "Causes the target to regain 2% health during the spell's duration.";
					spell.Message1 = "You start healing faster.";
					spell.Message2 = "{0} starts healing faster.";
					spell.Range = 1800;
					spell.SpellID = 11757;
					spell.Target = "Self";
					spell.Uninterruptible = true;
					spell.Type = eSpellType.HealOverTime.ToString();
					m_RoesiaHOT = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RoesiaHOT);
				}
				return m_RoesiaHOT;
			}
		}
		private Spell m_RoesiaDS;
		private Spell RoesiaDS
		{
			get
			{
				if (m_RoesiaDS == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 70;
					spell.ClientEffect = 57;
					spell.Icon = 57;
					spell.Damage = 80;
					spell.Duration = 60;
					spell.Name = "Roesia Damage Shield";
					spell.TooltipId = 57;
					spell.SpellID = 11758;
					spell.Target = "Self";
					spell.Type = "DamageShield";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_RoesiaDS = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RoesiaDS);
				}
				return m_RoesiaDS;
			}
		}
        #endregion
    }
}
/// <summary>
/// //////////////////////////////////////////////////////////////////Summoned Bows//////////////////////////////////////////////////////////////////
/// </summary>
namespace DOL.AI.Brain
{
	public class RoesiaBowsBrain : StandardMobBrain
	{
		public RoesiaBowsBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
			}
			base.Think();
		}
	}
}
namespace DOL.GS
{
	public class RoesiaBows : GameNPC
	{
		public override int MaxHealth
		{
			get { return 3000 * Constitution / 100; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 500;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.35;
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 35;// dmg reduction for melee dmg
				case eDamageType.Crush: return 35;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 35;// dmg reduction for melee dmg
				default: return 25;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 70;
		}
		public override bool AddToWorld()
		{
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.DistanceWeapon, 132, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Distance);
			MeleeDamageType = eDamageType.Thrust;
			VisibleActiveWeaponSlots = 51;//distance

			Model = 665;
			Name = "Summoned Bow";
			Strength = 80;
			Dexterity = 200;
			Quickness = 100;
			Constitution = 100;
			RespawnInterval = -1;
			MaxDistance = 2200;
			TetherRange = 1300;
			Size = 60;
			Level = (byte)Util.Random(65, 70);
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			RoesiaBowsBrain bows = new RoesiaBowsBrain();
			SetOwnBrain(bows);
			base.AddToWorld();
			return true;
		}

	}
}