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
			get { return 30000; }
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
							truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else//take dmg
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
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
			ThinkInterval = 2000;
		}
		public override void Think()
		{			
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				RandomTarget = null;
				CanCast = false;
				if (Enemys_To_DD.Count > 0)
					Enemys_To_DD.Clear();
			}
			if(Body.IsAlive)
            {
				if (!Body.Spells.Contains(RoesiaDot))
					Body.Spells.Add(RoesiaDot);
				if (!Body.Spells.Contains(RoesiaDS))
					Body.Spells.Add(RoesiaDS);
				if (!Body.Spells.Contains(RoesiaHOT))
					Body.Spells.Add(RoesiaHOT);
			}
			if (HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if (Util.Chance(25))
					{
						if (!Body.effectListComponent.ContainsEffectForEffectType(eEffect.DamageReturn) && !Body.IsCasting)
							Body.CastSpell(RoesiaDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//Cast DS
					}
					if(Util.Chance(35))
                    {
						if (Body.HealthPercent < 25)
							Body.CastSpell(RoesiaHOT, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast HOT
					}
					if (Util.Chance(35))
					{ 
						foreach (Spell spells in Body.Spells)
						{
							if (spells != null)
							{
								if (Body.attackComponent.AttackState && Body.IsCasting)
									Body.attackComponent.NPCStopAttack();
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range) && Body.IsCasting)
									Body.StopFollowing();

								PickRandomTarget();
								if (RandomTarget != null && RandomTarget.IsAlive && CanCast)
								{
									GameLiving oldTarget = Body.TargetObject as GameLiving;
									Body.TargetObject = RandomTarget;
									Body.TurnTo(RandomTarget);
									Body.CastSpell(RoesiaDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
								}
							}
						}
					}
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
							Enemys_To_DD.Add(player);
                    }
                }
            }
			if(Enemys_To_DD.Count>0)
            {
				if (CanCast==false)
				{
					GamePlayer Target = Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDot), 15000);
					CanCast = true;
				}				
			}
        }
		public int ResetDot(ECSGameTimer timer)//reset here so boss can start dot again
        {
			RandomTarget = null;
			CanCast = false;
			return 0;
        }
        #region Roesia Spells
        private Spell m_RoesiaDot;
		private Spell RoesiaDot
		{
			get
			{
				if (m_RoesiaDot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
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
		private Spell RoesiaHOT
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
					spell.RecastDelay = 300;
					spell.ClientEffect = 57;
					spell.Icon = 57;
					spell.Damage = 80;
					spell.Duration = 300;
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