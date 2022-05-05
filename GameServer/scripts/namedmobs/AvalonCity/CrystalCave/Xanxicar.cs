using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS;
using DOL.GS.ServerProperties;
using System.Collections.Generic;

namespace DOL.GS
{
	public class Xanxicar : GameEpicBoss
	{
		protected String[] m_deathAnnounce;
		public Xanxicar() : base() 
		{
			m_deathAnnounce = new String[] { "The earth lurches beneath your feet as {0} staggers and topples to the ground.",
				"A glowing light begins to form on the mound that served as {0}'s lair." };
		}
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Xanxicar Initializing...");
		}
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
			get { return 200000; }
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(102);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(10);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(10));

			XanxicarBrain sbrain = new XanxicarBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        #region Die/Boradcast/ReportNews/AwardDragonKillPoints
        public override void Die(GameObject killer)
		{
			// debug
			if (killer == null)
				log.Error("Dragon Killed: killer is null!");
			else
				log.Debug("Dragon Killed: killer is " + killer.Name + ", attackers:");

			bool canReportNews = true;

			// due to issues with attackers the following code will send a notify to all in area in order to force quest credit
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

				if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
				{
					if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
						canReportNews = false;
				}

			}

			base.Die(killer);

			foreach (String message in m_deathAnnounce)
			{
				BroadcastMessage(String.Format(message, Name));
			}

			if (canReportNews)
			{
				ReportNews(killer);
			}
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}

		/// <summary>
		/// Post a message in the server news and award a dragon kill point for
		/// every XP gainer in the raid.
		/// </summary>
		/// <param name="killer">The living that got the killing blow.</param>
		protected void ReportNews(GameObject killer)
		{
			int numPlayers = AwardDragonKillPoint();
			String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
			NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);

			if (Properties.GUILD_MERIT_ON_DRAGON_KILL > 0)
			{
				foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					if (player.IsEligibleToGiveMeritPoints)
					{
						GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_DRAGON_KILL);
					}
				}
			}
		}

		/// <summary>
		/// Award dragon kill point for each XP gainer.
		/// </summary>
		/// <returns>The number of people involved in the kill.</returns>
		protected int AwardDragonKillPoint()
		{
			int count = 0;
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.KillsDragon++;
				count++;
			}
			return count;
		}
        #endregion
    }
}
namespace DOL.AI.Brain
{
	public class XanxicarBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public XanxicarBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
        #region Check Flags/Port,DD list/broadcast
        public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		public static GamePlayer randomtarget2 = null;
		public static GamePlayer RandomTarget2
		{
			get { return randomtarget2; }
			set { randomtarget2 = value; }
		}
		public static bool IsTargetPicked = false;
		public static bool IsTargetPicked2 = false;
		public static bool Bomb1 = false;
		public static bool Bomb2 = false;
		public static bool Bomb3 = false;
		public static bool Bomb4 = false;
		List<GamePlayer> Port_Enemys = new List<GamePlayer>();
		List<GamePlayer> DD_Enemys = new List<GamePlayer>();
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
        #endregion

        #region Throw player
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
								if (player != Body.TargetObject)
									Port_Enemys.Add(player);
							}
						}
					}
				}
				if (Port_Enemys.Count > 0)
				{
					GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
					RandomTarget = Target;
					if (RandomTarget.IsAlive && RandomTarget != null && HasAggro)
					{
						BroadcastMessage(RandomTarget.Name + " is hurled into the air!");
						RandomTarget.MoveTo(62, 32338, 32387, 16539, 1830);
						Port_Enemys.Remove(RandomTarget);
					}
				}
				RandomTarget = null;//reset random target to null
				IsTargetPicked = false;
			}
			return 0;
		}
        #endregion

        #region Pick Glare Target
        public int GlarePlayer(ECSGameTimer timer)
		{
			if (Body.IsAlive)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1 && player.CharacterClass.ID != 12)
						{
							if (!DD_Enemys.Contains(player))
									DD_Enemys.Add(player);
						}
					}
				}
				if (DD_Enemys.Count > 0)
				{
					GamePlayer Target = DD_Enemys[Util.Random(0, DD_Enemys.Count - 1)];
					RandomTarget2 = Target;
					if (RandomTarget2.IsAlive && RandomTarget2 != null && HasAggro)
					{
						BroadcastMessage("Xanxicar preparing glare at "+ RandomTarget2.Name +"!");
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DoGlare), 5000);
					}
				}
			}
			return 0;
		}
		public int DoGlare(ECSGameTimer timer)
		{
			GameObject oldTarget = Body.TargetObject;
			Body.TargetObject = RandomTarget2;
			Body.TurnTo(RandomTarget2);
			if (Body.TargetObject != null && RandomTarget2.IsAlive)
				Body.CastSpell(XanxicarGlare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			if (oldTarget != null) Body.TargetObject = oldTarget;
			RandomTarget2 = null;//reset random target to null
			IsTargetPicked2 = false;
			return 0;
		}
        #endregion

        #region PBAOE
        public int BombAnnounce(ECSGameTimer timer)
		{
			BroadcastMessage(String.Format("Xanxicar bellows in rage and prepares massive stomp at all of the creatures attacking him."));
			if (Body.IsAlive && HasAggro)
			{
				Body.StopFollowing();
				Body.CastSpell(XanxicarStomp, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			return 0;
		}
		#endregion

		#region Think()
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				RandomTarget = null;//throw
				RandomTarget2 = null;//glare
				IsTargetPicked = false;//throw
				IsTargetPicked2 = false;//glare
				Bomb1 = false;
				Bomb2 = false;
				Bomb3 = false;
				Bomb4 = false;
				SpawnAddsOnce = false;
				CheckForSingleAdd = false;
				XanxicarianChampion.XanxicarianChampionCount = 0;
				foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
					if(npc != null)
                    {
						if(npc.IsAlive && npc.Brain is XanxicarianChampionBrain)
							npc.RemoveFromWorld();
                    }
                }
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if (IsTargetPicked == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ThrowPlayer), Util.Random(20000, 40000));//timer to port and pick player
					IsTargetPicked = true;
				}
				if (IsTargetPicked2 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(GlarePlayer), Util.Random(35000, 45000));//timer to glare at player
					IsTargetPicked2 = true;
				}
				if(Body.HealthPercent <= 80 && Bomb1 == false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(BombAnnounce), 1000);
					Bomb1 = true;
				}
				if (Body.HealthPercent <= 60 && Bomb2 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(BombAnnounce), 1000);
					Bomb2 = true;
				}
				if (Body.HealthPercent <= 40 && Bomb3 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(BombAnnounce), 1000);
					Bomb3 = true;
				}
				if (Body.HealthPercent <= 20 && Bomb4 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(BombAnnounce), 1000);
					Bomb4 = true;
				}
				if(Body.HealthPercent<=50)
                {
					if(SpawnAddsOnce==false && XanxicarianChampion.XanxicarianChampionCount == 0)
                    {
						SpawnAdds();
						SpawnAddsOnce = true;
                    }
					if(SpawnAddsOnce && CheckForSingleAdd==false && XanxicarianChampion.XanxicarianChampionCount == 0)
                    {
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnMoreAdds), Util.Random(15000, 25000));//spawn 1 add every 25-35s
						CheckForSingleAdd = true;
                    }
                }
			}
			base.Think();
		}
        #endregion

        #region adds
        public static bool SpawnAddsOnce = false;
		public static bool CheckForSingleAdd = false;
		public void SpawnAdds()
        {
			for (int i = 0; i < 5; i++)
			{
				XanxicarianChampion Add = new XanxicarianChampion();
				Add.X = Body.X + Util.Random(-150, 150);
				Add.Y = Body.Y + Util.Random(-150, 150);
				Add.Z = Body.Z;
				Add.Level = 65;
				Add.CurrentRegion = Body.CurrentRegion;
				Add.Heading = Body.Heading;
				Add.AddToWorld();
			}
		}
		public int SpawnMoreAdds(ECSGameTimer timer)
        {
			if (XanxicarianChampion.XanxicarianChampionCount == 0 && HasAggro)
			{
				XanxicarianChampion Add = new XanxicarianChampion();
				Add.X = Body.X + Util.Random(-150, 150);
				Add.Y = Body.Y + Util.Random(-150, 150);
				Add.Z = Body.Z;
				Add.Level = 65;
				Add.CurrentRegion = Body.CurrentRegion;
				Add.Heading = Body.Heading;
				Add.AddToWorld();
				CheckForSingleAdd = false;
			}
			return 0;
        }
        #endregion

        #region spells
        private Spell m_XanxicarStomp;
		private Spell XanxicarStomp
		{
			get
			{
				if (m_XanxicarStomp == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 6;
					spell.RecastDelay = 10;
					spell.ClientEffect = 1695;
					spell.Icon = 1695;
					spell.TooltipId = 1695;
					spell.Damage = 2500;
					spell.Name = "Xanxicar's Stomp";
					spell.Range = 0;
					spell.Radius = 2500;
					spell.SpellID = 11802;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Energy;
					m_XanxicarStomp = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_XanxicarStomp);
				}
				return m_XanxicarStomp;
			}
		}
		private Spell m_XanxicarGlare;
		private Spell XanxicarGlare
		{
			get
			{
				if (m_XanxicarGlare == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 1678;
					spell.Icon = 1678;
					spell.TooltipId = 1678;
					spell.Damage = 1500;
					spell.Name = "Xanxicar's Glare";
					spell.Range = 1500;
					spell.Radius = 450;
					spell.SpellID = 11803;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Energy;
					m_XanxicarGlare = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_XanxicarGlare);
				}
				return m_XanxicarGlare;
			}
		}
        #endregion
    }
}
///////////////////////////////////////////////////////////////////////////Adds will start spawn at 50%/////////////////////////////////////////////////
#region XanxicarianChampion
namespace DOL.GS
{
	public class XanxicarianChampion : GameEpicNPC
	{
		public XanxicarianChampion() : base() { }

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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 8000; }
		}

		public static int XanxicarianChampionCount = 0;
		public override void Die(GameObject killer)
		{
			--XanxicarianChampionCount;
			base.Die(killer);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(91);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			++XanxicarianChampionCount;

			Faction = FactionMgr.GetFactionByID(10);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(10));
			RespawnInterval = -1;

			XanxicarianChampionBrain sbrain = new XanxicarianChampionBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class XanxicarianChampionBrain : StandardMobBrain
	{
		public XanxicarianChampionBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 1500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

