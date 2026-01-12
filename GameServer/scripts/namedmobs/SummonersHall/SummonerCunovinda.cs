using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
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
			get { return 100000; }
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
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
							truc = ((source as GameSummonedPet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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

		public override void Die(GameObject killer)
        {
			// debug
			if (killer == null)
				log.Error("Summoner Cunovinda Killed: killer is null!");
			else
				log.Debug("Summoner Cunovinda Killed: killer is " + killer.Name + ", attackers:");

			bool canReportNews = true;
			DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>("grimoire_pages");
			int itemCount = 100;
			string message_currency = "Summoner Cunovinda drops " + itemCount + " " + template.Name + ".";
			// due to issues with attackers the following code will send a notify to all in area in order to force quest credit
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				DbInventoryItem item = GameInventoryItem.Create(template);
				item.Count = itemCount;
				if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item))
				{
					player.Out.SendMessage(message_currency, eChatType.CT_Loot, eChatLoc.CL_ChatWindow);
					InventoryLogging.LogInventoryAction(player, player, eInventoryActionType.Other, template, itemCount);
				}
				player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));
				if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
				{
					if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
						canReportNews = false;
				}
			}
			base.Die(killer);
			if (canReportNews)
			{
				ReportNews(killer);
			}
        }

		/// <summary>
		/// Post a message in the server news and award a dragon kill point for
		/// every XP gainer in the raid.
		/// </summary>
		/// <param name="killer">The living that got the killing blow.</param>
		protected void ReportNews(GameObject killer)
		{
			// int numPlayers = GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Count;
			String message = String.Format("{0} has been slain!", Name);
			NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);
		}

		public override int MeleeAttackRange => 350;
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18805);
			LoadTemplate(npcTemplate);
			// Custom Respawn +/- 20% 2h
			int baseRespawnMS = 7200000; 
            int maxOffsetMS = 1440000; 
            Random rnd = new Random();
            int randomOffset = rnd.Next(maxOffsetMS * 2) - maxOffsetMS;
            RespawnInterval = baseRespawnMS + randomOffset;
			SummonerCunovindaBrain.RandomTarget = null;
			SummonerCunovindaBrain.CanCast = false;
			Faction = FactionMgr.GetFactionByID(187);
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

			npcs = WorldMgr.GetNPCsByNameFromRegion("Summoner Cunovinda", 233, (eRealm)0);
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
				OF.CurrentRegionID = 233;//NF summoners hall

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

				OF.X = 37114;
				OF.Y = 35081;
				OF.Z = 16063;
				OF.TetherRange = 1300;
				OF.MaxSpeedBase = 250;
				OF.Heading = 3065;
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
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SummonerCunovindaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
        public override void OnAttackedByEnemy(AttackData ad)
        {
			if(ad.Damage > 0 && ad != null)
            {
				if(Util.Chance(15))//here edit to change teleport chance to happen
					PickRandomTarget();//start teleport here
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				RandomTarget = null;
				CanCast = false;
				if (Enemys_To_DD.Count > 0)
					Enemys_To_DD.Clear();//clear list if it reset
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
							Enemys_To_DD.Add(player);//add player to list
					}
				}
			}
			if (Enemys_To_DD.Count > 0)
			{
				if (CanCast == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastBolt), 1000);
					CanCast = true;
				}
			}
		}
		public int CastBolt(ECSGameTimer timer)
		{
			GameLiving oldTarget = (GameLiving)Body.TargetObject;//old target
			if (RandomTarget != null && RandomTarget.IsAlive)
			{
				Body.TurnTo(RandomTarget);//turn to randomtarget
				Body.StopFollowing();//stop follow
				Body.CastSpell(CunovindaBolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);//cast bolt

				RandomTarget.MoveTo(Body.CurrentRegionID, 24874, 36116, 17060, 3065);//port player to loc

				if(Body.TargetObject != null && Body.TargetObject != RandomTarget)
					Body.TargetObject = RandomTarget;//set target as randomtarget
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetBolt), Util.Random(8000, 12000));//teleport every 8-12s if melee hit got chance to proc teleport
			return 0;
		}
		public int ResetBolt(ECSGameTimer timer)//reset here so boss can start dot again
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
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 2970;
					spell.Icon = 2970;
					spell.TooltipId = 2970;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Summoner Bolt";
					spell.Range = 1800;
					spell.SpellID = 11761;
					spell.Target = eSpellTarget.ENEMY.ToString();
					spell.Uninterruptible = true;
					spell.Type = eSpellType.Bolt.ToString();
					m_CunovindaBolt = new Spell(spell, 50);
				}
				return m_CunovindaBolt;
			}
		}
		#endregion
	}
}
