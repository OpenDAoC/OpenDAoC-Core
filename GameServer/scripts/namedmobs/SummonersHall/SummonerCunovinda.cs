using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;


namespace DOL.GS
{
	public class SummonerCunovinda : GameEpicBoss
	{
		public SummonerCunovinda() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 80;// dmg reduction for rest resists
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
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 700;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.55;
		}
		public override int MaxHealth
		{
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18805);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			SummonerCunovindaBrain.RandomTarget = null;
			SummonerCunovindaBrain.CanCast = false;
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));
			IsCloakHoodUp = true;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 305, 43, 0, 0); //Slot,model,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 307, 43);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 306, 43);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 308, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 309, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.Cloak, 57, 54, 0, 0);
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 327, 43, 90, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			SummonerCunovindaBrain sbrain = new SummonerCunovindaBrain();
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

			npcs = WorldMgr.GetNPCsByNameFromRegion("Summoner Cunovinda", 248, (eRealm)0);
			if (npcs.Length == 0)
			{
				log.Warn("Summoner Cunovinda not found, creating it...");

				log.Warn("Initializing Summoner Cunovinda...");
				SummonerCunovinda OF = new SummonerCunovinda();
				OF.Name = "Summoner Cunovinda";
				OF.Model = 162;
				OF.Realm = 0;
				OF.Level = 75;
				OF.Size = 65;
				OF.CurrentRegionID = 248;//OF summoners hall

				OF.Strength = 5;
				OF.Intelligence = 200;
				OF.Piety = 200;
				OF.Dexterity = 200;
				OF.Constitution = 100;
				OF.Quickness = 80;
				OF.Empathy = 300;
				OF.BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;
				OF.MeleeDamageType = eDamageType.Crush;
				OF.Faction = FactionMgr.GetFactionByID(187);
				OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));

				OF.X = 26023;
				OF.Y = 36132;
				OF.Z = 15998;
				OF.MaxDistance = 2000;
				OF.TetherRange = 1300;
				OF.MaxSpeedBase = 250;
				OF.Heading = 19;
				OF.IsCloakHoodUp = true;

				SummonerCunovindaBrain ubrain = new SummonerCunovindaBrain();
				ubrain.AggroLevel = 100;
				ubrain.AggroRange = 600;
				OF.SetOwnBrain(ubrain);
				OF.AddToWorld();
				OF.Brain.Start();
				OF.SaveIntoDatabase();
			}
			else
				log.Warn("Summoner Cunovinda exist ingame, remove it and restart server if you want to add by script code.");
		}
	}
}
namespace DOL.AI.Brain
{
	public class SummonerCunovindaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SummonerCunovindaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
        public override void OnAttackedByEnemy(AttackData ad)
        {
			if(ad.IsMeleeAttack && ad.IsHit && (ad.Attacker is GamePlayer || ad.Attacker is GamePet))
            {
				if(Util.Chance(15))//here edit to change teleport chance to happen
                {
					PickRandomTarget();//start teleport here
                }
            }
            base.OnAttackedByEnemy(ad);
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
				{
					Enemys_To_DD.Clear();//clear list if it reset
				}
			}
			if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
			{
				Body.Health = Body.MaxHealth;
				CanCast = false;
				RandomTarget = null;
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
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_DD.Contains(player))
						{
							Enemys_To_DD.Add(player);//add player to list
						}
					}
				}
			}
			if (Enemys_To_DD.Count > 0)
			{
				if (CanCast == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					new RegionTimer(Body, new RegionTimerCallback(CastBolt), 1000);
					CanCast = true;
				}
			}
		}
		public int CastBolt(RegionTimer timer)
		{
			GameLiving oldTarget = (GameLiving)Body.TargetObject;//old target
			if (RandomTarget != null && RandomTarget.IsAlive)
			{
				RandomTarget.MoveTo(Body.CurrentRegionID, 24874, 36116, 17060, 3065);//port player to loc

				if(Body.TargetObject != null && Body.TargetObject != RandomTarget)
					Body.TargetObject = RandomTarget;//set target as randomtarget

				Body.TurnTo(RandomTarget);//turn to randomtarget
				Body.StopFollowing();//stop follow
				Body.CastSpell(CunovindaBolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast bolt
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			new RegionTimer(Body, new RegionTimerCallback(ResetBolt), Util.Random(8000, 12000));//teleport every 8-12s if melee hit got chance to proc teleport
			return 0;
		}
		public int ResetBolt(RegionTimer timer)//reset here so boss can start dot again
		{
			RandomTarget = null;
			CanCast = false;
			return 0;
		}
		#region Cunovinda Spells
		private Spell m_CunovindaBolt;
		public Spell CunovindaBolt
		{
			get
			{
				if (m_CunovindaBolt == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 2970;
					spell.Icon = 2970;
					spell.TooltipId = 2970;
					spell.Damage = 250;
					spell.Frequency = 30;
					spell.Duration = 36;
					spell.DamageType = (int)eDamageType.Spirit;
					spell.Name = "Summoner Bolt";
					spell.Range = 1800;
					spell.SpellID = 11761;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.Type = eSpellType.Bolt.ToString();
					m_CunovindaBolt = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CunovindaBolt);
				}
				return m_CunovindaBolt;
			}
		}
		#endregion
	}
}