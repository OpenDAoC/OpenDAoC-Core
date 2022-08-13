using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
	public class Myrddraxis : GameEpicBoss
	{
		protected String[] m_deathAnnounce;
		public Myrddraxis() : base() 
		{
			m_deathAnnounce = new String[] { "The earth lurches beneath your feet as {0} staggers and topples to the ground.",
				"A glowing light begins to form on the mound that served as {0}'s lair." };
		}
        #region Custom methods
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
			int numPlayers = GetPlayersInRadiusCount(WorldMgr.VISIBILITY_DISTANCE);
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
		public override void Die(GameObject killer)
		{
			foreach (GameNPC heads in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (heads != null)
				{
					if (heads.IsAlive && (heads.Brain is MyrddraxisSecondHeadBrain || heads.Brain is MyrddraxisThirdHeadBrain || heads.Brain is MyrddraxisFourthHeadBrain || heads.Brain is MyrddraxisFifthHeadBrain))
						heads.Die(heads);
				}
			}
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

			AwardDragonKillPoint();

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
		#endregion
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Myrddraxis Initializing...");
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
		public override int AttackRange
		{
			get { return 550; }
			set { }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
        public override void OnAttackEnemy(AttackData ad)
        {
			if(ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
            {
				if(Util.Chance(25))
                {
					switch(Util.Random(1,2))
                    {
						case 1: CastSpell(HydraDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break;
						case 2: CastSpell(Hydra_Haste_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break;
					}					
				}
            }
            base.OnAttackEnemy(ad);
        }
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164337);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			MaxSpeedBase = 0;
			X = 32302;
			Y = 32221;
			Z = 15635;
			Heading = 492;

			Faction = FactionMgr.GetFactionByID(105);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

			CanSpawnHeads = false;
			if(CanSpawnHeads == false)
            {
				SpawnHeads();
				CanSpawnHeads = true;
            }

			MyrddraxisBrain sbrain = new MyrddraxisBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        #region Spawn Heads
        public static bool CanSpawnHeads = false;
		public void SpawnHeads()
        {
			//Second Head
			MyrddraxisSecondHead Add1 = new MyrddraxisSecondHead();
			Add1.X = 32384;
			Add1.Y = 31942;
			Add1.Z = 15931;
			Add1.CurrentRegion = CurrentRegion;
			Add1.Heading = 455;
			Add1.Flags = eFlags.FLYING;
			Add1.RespawnInterval = -1;
			Add1.AddToWorld();

			//Third Head
			MyrddraxisThirdHead Add2 = new MyrddraxisThirdHead();
			Add2.X = 32187;
			Add2.Y = 32205;
			Add2.Z = 15961;
			Add2.CurrentRegion = CurrentRegion;
			Add2.Heading = 4095;
			Add2.Flags = eFlags.FLYING;
			Add2.RespawnInterval = -1;
			Add2.AddToWorld();

			//Fourth Head
			MyrddraxisFourthHead Add3 = new MyrddraxisFourthHead();
			Add3.X = 32371;
			Add3.Y = 32351;
			Add3.Z = 15936;
			Add3.CurrentRegion = CurrentRegion;
			Add3.Heading = 971;
			Add3.Flags = eFlags.FLYING;
			Add3.RespawnInterval = -1;
			Add3.AddToWorld();

			//Fifth Head
			MyrddraxisFifthHead Add4 = new MyrddraxisFifthHead();
			Add4.X = 32576;
			Add4.Y = 32133;
			Add4.Z = 15936;
			Add4.CurrentRegion = CurrentRegion;
			Add4.Heading = 4028;
			Add4.Flags = eFlags.FLYING;
			Add4.RespawnInterval = -1;
			Add4.AddToWorld();
		}
		#endregion
		#region spells
		private Spell m_HydraDisease;
		private Spell HydraDisease
		{
			get
			{
				if (m_HydraDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(25, 35);
					spell.ClientEffect = 4375;
					spell.Icon = 4375;
					spell.Name = "Disease";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.TooltipId = 4375;
					spell.Range = 0;
					spell.Radius = 800;
					spell.Duration = 120;
					spell.SpellID = 11843;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
					m_HydraDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HydraDisease);
				}
				return m_HydraDisease;
			}
		}
		private Spell m_Hydra_Haste_Debuff;
		private Spell Hydra_Haste_Debuff
		{
			get
			{
				if (m_Hydra_Haste_Debuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 60;
					spell.Duration = 60;
					spell.ClientEffect = 5427;
					spell.Icon = 5427;
					spell.Name = "Combat Speed Debuff";
					spell.TooltipId = 5427;
					spell.Range = 0;
					spell.Value = 24;
					spell.Radius = 800;
					spell.SpellID = 11844;
					spell.Target = "Enemy";
					spell.Type = eSpellType.CombatSpeedDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Hydra_Haste_Debuff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_Haste_Debuff);
				}
				return m_Hydra_Haste_Debuff;
			}
		}
		#endregion
	}
}
namespace DOL.AI.Brain
{
	public class MyrddraxisBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MyrddraxisBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
		public static bool CanCast = false;
		public static bool CanCast2 = false;
		public static bool CanCastStun1 = false;
		public static bool CanCastStun2 = false;
		public static bool CanCastStun3 = false;
		public static bool CanCastStun4 = false;
		public static bool CanCastPBAOE1 = false;
		public static bool CanCastPBAOE2 = false;
		public static bool CanCastPBAOE3 = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		#region Hydra DOT
		public static GamePlayer randomtarget2 = null;
		public static GamePlayer RandomTarget2
		{
			get { return randomtarget2; }
			set { randomtarget2 = value; }
		}
		List<GamePlayer> Enemys_To_DOT = new List<GamePlayer>();
		public int PickRandomTarget2(ECSGameTimer timer)
		{
			if (HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
						{
							if (!Enemys_To_DOT.Contains(player))
								Enemys_To_DOT.Add(player);
						}
					}
				}
				if (Enemys_To_DOT.Count > 0)
				{
					if (CanCast2 == false)
					{
						GamePlayer Target = (GamePlayer)Enemys_To_DOT[Util.Random(0, Enemys_To_DOT.Count - 1)];//pick random target from list
						RandomTarget2 = Target;//set random target to static RandomTarget
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDOT), 2000);
						CanCast2 = true;
					}
				}
			}
			return 0;
		}
		public int CastDOT(ECSGameTimer timer)
		{
			if (HasAggro && RandomTarget2 != null)
			{
				GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
				if (RandomTarget2 != null && RandomTarget2.IsAlive)
				{
					Body.TargetObject = RandomTarget2;
					Body.TurnTo(RandomTarget2);
					Body.CastSpell(Hydra_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDOT), 5000);
			}
			return 0;
		}
		public int ResetDOT(ECSGameTimer timer)
		{
			RandomTarget2 = null;
			CanCast2 = false;
			StartCastDOT = false;
			return 0;
		}
		#endregion
		#region Hydra DD
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
		public int PickRandomTarget(ECSGameTimer timer)
		{
			if (HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
						{
							if (!Enemys_To_DD.Contains(player))
								Enemys_To_DD.Add(player);
						}
					}
				}
				if (Enemys_To_DD.Count > 0)
				{
					if (CanCast == false)
					{
						GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
						RandomTarget = Target;//set random target to static RandomTarget
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDD), 5000);
						BroadcastMessage(String.Format(Body.Name + " taking a big flame breath at " + RandomTarget.Name + "."));
						CanCast = true;
					}
				}
			}
			return 0;
		}
		public int CastDD(ECSGameTimer timer)
		{
			if (HasAggro && RandomTarget != null)
			{
				GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
				if (RandomTarget != null && RandomTarget.IsAlive)
				{
					Body.TargetObject = RandomTarget;
					Body.TurnTo(RandomTarget);
					Body.CastSpell(Hydra_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDD), 5000);
			}
			return 0;
		}
		public int ResetDD(ECSGameTimer timer)
		{
			RandomTarget = null;
			CanCast = false;
			StartCastDD = false;
			return 0;
		}
        #endregion
        #region Hydra Stun
		public int HydraStun(ECSGameTimer timer)
        {
			if(HasAggro && Body.IsAlive)
				Body.CastSpell(Hydra_Stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			return 0;
        }
		#endregion
		#region Hydra PBAOE
		public int HydraPBAOE(ECSGameTimer timer)
		{
			if (HasAggro && Body.IsAlive)
				Body.CastSpell(Hydra_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			return 0;
		}
		#endregion
		public static bool StartCastDD = false;
		public static bool StartCastDOT = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled = false;
				StartCastDD = false;
				StartCastDOT = false;
				CanCast = false;
				CanCast2 = false;
				RandomTarget = null;
				RandomTarget2 = null;
				CanCastStun1 = false;
				CanCastStun2 = false;
				CanCastStun3 = false;
				CanCastStun4 = false;
				CanCastPBAOE1 = false;
				CanCastPBAOE2 = false;
				CanCastPBAOE3 = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
					{
						if (npc != null)
						{
							if (MyrddraxisSecondHead.SecondHeadCount == 0)
							{
								MyrddraxisSecondHead Add1 = new MyrddraxisSecondHead();
								Add1.X = 32384;
								Add1.Y = 31942;
								Add1.Z = 15931;
								Add1.CurrentRegion = Body.CurrentRegion;
								Add1.Heading = 455;
								Add1.Flags = GameNPC.eFlags.FLYING;
								Add1.RespawnInterval = -1;
								Add1.AddToWorld();
							}
							if (MyrddraxisThirdHead.ThirdHeadCount == 0)
							{
								MyrddraxisThirdHead Add2 = new MyrddraxisThirdHead();
								Add2.X = 32187;
								Add2.Y = 32205;
								Add2.Z = 15961;
								Add2.CurrentRegion = Body.CurrentRegion;
								Add2.Heading = 4095;
								Add2.Flags = GameNPC.eFlags.FLYING;
								Add2.RespawnInterval = -1;
								Add2.AddToWorld();
							}
							if (MyrddraxisFourthHead.FourthHeadCount == 0)
							{
								MyrddraxisFourthHead Add3 = new MyrddraxisFourthHead();
								Add3.X = 32371;
								Add3.Y = 32351;
								Add3.Z = 15936;
								Add3.CurrentRegion = Body.CurrentRegion;
								Add3.Heading = 971;
								Add3.Flags = GameNPC.eFlags.FLYING;
								Add3.RespawnInterval = -1;
								Add3.AddToWorld();
							}
							if (MyrddraxisFifthHead.FifthHeadCount == 0)
							{
								MyrddraxisFifthHead Add4 = new MyrddraxisFifthHead();
								Add4.X = 32576;
								Add4.Y = 32133;
								Add4.Z = 15936;
								Add4.CurrentRegion = Body.CurrentRegion;
								Add4.Heading = 4028;
								Add4.Flags = GameNPC.eFlags.FLYING;
								Add4.RespawnInterval = -1;
								Add4.AddToWorld();
							}
						}
					}
					RemoveAdds = true;
				}
			}
			if (Body.IsAlive && HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if(StartCastDD==false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickRandomTarget), Util.Random(35000, 45000));
					StartCastDD = true;
				}
				if (StartCastDOT == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickRandomTarget2), Util.Random(35000, 45000));
					StartCastDOT = true;
				}
				if (IsPulled == false)
				{
					GameLiving ptarget = Body.TargetObject as GameLiving;
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisSecondHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisThirdHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisFourthHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisFifthHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					IsPulled = true;
				}
				#region Hydra Stun
				if (Body.HealthPercent <= 80 && CanCastStun1==false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(HydraStun), 5000);
					BroadcastMessage(String.Format(Body.Name + " prepares stunning breath."));
					CanCastStun1 = true;
                }
				else if (Body.HealthPercent <= 60 && CanCastStun2 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(HydraStun), 5000);
					BroadcastMessage(String.Format(Body.Name + " prepares stunning breath."));
					CanCastStun2 = true;
				}
				else if (Body.HealthPercent <= 40 && CanCastStun3 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(HydraStun), 5000);
					BroadcastMessage(String.Format(Body.Name + " prepares stunning breath."));
					CanCastStun3 = true;
				}
				else if (Body.HealthPercent <= 20 && CanCastStun4 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(HydraStun), 5000);
					BroadcastMessage(String.Format(Body.Name + " prepares stunning breath."));
					CanCastStun4 = true;
				}
				#endregion
				#region Hydra PBAOE
				if (Body.HealthPercent <= 75 && CanCastPBAOE1 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(HydraPBAOE), 6000);
					BroadcastMessage(String.Format(Body.Name + " taking a massive breath of flames to annihilate enemys."));
					CanCastPBAOE1 = true;
				}
				else if (Body.HealthPercent <= 50 && CanCastPBAOE2 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(HydraPBAOE), 6000);
					BroadcastMessage(String.Format(Body.Name + " taking a massive breath of flames to annihilate enemys."));
					CanCastPBAOE2 = true;
				}
				else if (Body.HealthPercent <= 25 && CanCastPBAOE3 == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(HydraPBAOE), 6000);
					BroadcastMessage(String.Format(Body.Name + " taking a massive breath of flames to annihilate enemys."));
					CanCastPBAOE3 = true;
				}
				#endregion
				GameLiving target = Body.TargetObject as GameLiving;
				if(target != null && !target.IsWithinRadius(Body,Body.AttackRange))
                {
					Body.SetGroundTarget(target.X, target.Y, target.Z);
					Body.CastSpell(Hydra_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast dmg if main target is not in attack range
				}
			}
			base.Think();
		}
		#region Spells
		private Spell m_Hydra_Dot;
		private Spell Hydra_Dot
		{
			get
			{
				if (m_Hydra_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4445;
					spell.Icon = 4445;
					spell.TooltipId = 4445;
					spell.Damage = 90;
					spell.Duration = 40;
					spell.Frequency = 40;
					spell.Name = "Myrddraxis Poison";
					spell.Range = 2000;
					spell.Radius = 800;
					spell.SpellID = 11849;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Body;
					m_Hydra_Dot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_Dot);
				}
				return m_Hydra_Dot;
			}
		}
		private Spell m_Hydra_DD;
		private Spell Hydra_DD
		{
			get
			{
				if (m_Hydra_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5700;
					spell.Icon = 5700;
					spell.TooltipId = 5700;
					spell.Damage = 1100;
					spell.Name = "Myrddraxis Breath of Flame";
					spell.Range = 2000;
					spell.Radius = 450;
					spell.SpellID = 11840;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Hydra_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_DD);
				}
				return m_Hydra_DD;
			}
		}
		private Spell m_Hydra_DD2;
		private Spell Hydra_DD2
		{
			get
			{
				if (m_Hydra_DD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 3;
					spell.ClientEffect = 5700;
					spell.Icon = 5700;
					spell.TooltipId = 5700;
					spell.Damage = 450;
					spell.Name = "Myrddraxis Breath of Flame";
					spell.Range = 2000;
					spell.Radius = 200;
					spell.SpellID = 11850;
					spell.Target = "Area";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Hydra_DD2 = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_DD2);
				}
				return m_Hydra_DD2;
			}
		}
		private Spell m_Hydra_PBAOE;
		private Spell Hydra_PBAOE
		{
			get
			{
				if (m_Hydra_PBAOE == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5700;
					spell.Icon = 5700;
					spell.TooltipId = 5700;
					spell.Damage = 2000;
					spell.Name = "Myrddraxis Breath of Annihilation";
					spell.Range = 0;
					spell.Radius = 1800;
					spell.SpellID = 11841;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Hydra_PBAOE = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_PBAOE);
				}
				return m_Hydra_PBAOE;
			}
		}
		private Spell m_Hydra_Stun;
		private Spell Hydra_Stun
		{
			get
			{
				if (m_Hydra_Stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 5703;
					spell.Icon = 5703;
					spell.TooltipId = 5703;
					spell.Duration = 30;
					spell.Name = "Myrddraxis Stun";
					spell.Range = 0;
					spell.Radius = 1800;
					spell.SpellID = 11842;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Body;
					m_Hydra_Stun = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_Stun);
				}
				return m_Hydra_Stun;
			}
		}
		#endregion
	}
}
///////////////////////////////////////////////////////////////////Myrddraxis-Heads////////////////////////////////////////////////////
#region 2nd Head of Myrddraxis
namespace DOL.GS
{
	public class MyrddraxisSecondHead : GameNPC
	{
		public MyrddraxisSecondHead() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Second Head of Myrddraxis Initializing...");
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
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 40000; }
		}
		public static int SecondHeadCount = 0;
		public override void Die(GameObject killer)
		{
			--SecondHeadCount;
			base.Die(killer);
		}
        public override void DealDamage(AttackData ad)
        {
			if(ad != null)
            {
				foreach(GameNPC hydra in GetNPCsInRadius(2000))
                {
					if(hydra != null)
                    {
						if(hydra.IsAlive && hydra.Brain is MyrddraxisBrain)
                        {
							hydra.Health += ad.Damage / 2;//dmg heals hydra
                        }
                    }
                }
				foreach (GameNPC heads in GetNPCsInRadius(2000))
				{
					if (heads != null)
					{
						if (heads.IsAlive && (heads.Brain is MyrddraxisThirdHeadBrain || heads.Brain is MyrddraxisFourthHeadBrain || heads.Brain is MyrddraxisFifthHeadBrain))
						{
							heads.Health += ad.Damage / 10;//dmg heals heads but not the one that is being attacked
						}
					}
				}
			}
            base.DealDamage(ad);
        }
        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165727);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = -1;
			MaxSpeedBase = 0;
			++SecondHeadCount;
			Faction = FactionMgr.GetFactionByID(105);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

			MyrddraxisSecondHeadBrain sbrain = new MyrddraxisSecondHeadBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MyrddraxisSecondHeadBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MyrddraxisSecondHeadBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled1 = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled1 = false;
			}
			if (Body.IsAlive && HasAggro && Body.TargetObject != null)
			{
				Body.CastSpell(Head2_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (IsPulled1==false)
				{
					GameLiving ptarget = Body.TargetObject as GameLiving;
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisThirdHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisFourthHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisFifthHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					IsPulled1 = true;
				}
			}
			base.Think();
		}
		#region spells
		private Spell m_Head2_DD;
		private Spell Head2_DD
		{
			get
			{
				if (m_Head2_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(5,8);
					spell.ClientEffect = 4159;
					spell.Icon = 4159;
					spell.TooltipId = 4159;
					spell.Damage = 350;
					spell.Name = "Breath of Darkness";
					spell.Range = 2000;
					spell.SpellID = 11845;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Cold;
					m_Head2_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Head2_DD);
				}
				return m_Head2_DD;
			}
		}
		#endregion
	}
}
#endregion
#region 3th Head of Myrddraxis
namespace DOL.GS
{
	public class MyrddraxisThirdHead : GameNPC
	{
		public MyrddraxisThirdHead() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Third Head of Myrddraxis Initializing...");
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
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 40000; }
		}
		public static int ThirdHeadCount = 0;
		public override void Die(GameObject killer)
		{
			--ThirdHeadCount;
			base.Die(killer);
		}
		public override void DealDamage(AttackData ad)
		{
			if (ad != null)
			{
				foreach (GameNPC hydra in GetNPCsInRadius(2000))
				{
					if (hydra != null)
					{
						if (hydra.IsAlive && hydra.Brain is MyrddraxisBrain)
						{
							hydra.Health += ad.Damage / 2;//dmg heals hydra
						}
					}
				}
				foreach (GameNPC heads in GetNPCsInRadius(2000))
				{
					if (heads != null)
					{
						if (heads.IsAlive && (heads.Brain is MyrddraxisSecondHeadBrain || heads.Brain is MyrddraxisFourthHeadBrain || heads.Brain is MyrddraxisFifthHeadBrain))
						{
							heads.Health += ad.Damage / 10;//dmg heals heads but not the one that is being attacked
						}
					}
				}
			}
			base.DealDamage(ad);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167005);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = -1;
			MaxSpeedBase = 0;
			++ThirdHeadCount;

			Faction = FactionMgr.GetFactionByID(105);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

			MyrddraxisThirdHeadBrain sbrain = new MyrddraxisThirdHeadBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MyrddraxisThirdHeadBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MyrddraxisThirdHeadBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled2 = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled2 = false;
			}
			if (Body.IsAlive && HasAggro && Body.TargetObject != null)
			{
				Body.CastSpell(Head3_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (IsPulled2==false)
				{
					GameLiving ptarget = Body.TargetObject as GameLiving;
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisSecondHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisFourthHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisFifthHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					IsPulled2 = true;
				}
			}
			base.Think();
		}
		#region spells
		private Spell m_Head3_DD;
		private Spell Head3_DD
		{
			get
			{
				if (m_Head3_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(5,8);
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.TooltipId = 360;
					spell.Damage = 350;
					spell.Name = "Breath of Flame";
					spell.Range = 2000;
					spell.SpellID = 11846;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Head3_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Head3_DD);
				}
				return m_Head3_DD;
			}
		}
		#endregion
	}
}
#endregion
#region 4th Head of Myrddraxis
namespace DOL.GS
{
	public class MyrddraxisFourthHead : GameNPC
	{
		public MyrddraxisFourthHead() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Fourth Head of Myrddraxis Initializing...");
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
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 40000; }
		}
		public static int FourthHeadCount = 0;
		public override void Die(GameObject killer)
		{
			--FourthHeadCount;
			base.Die(killer);
		}
		public override void DealDamage(AttackData ad)
		{
			if (ad != null)
			{
				foreach (GameNPC hydra in GetNPCsInRadius(2000))
				{
					if (hydra != null)
					{
						if (hydra.IsAlive && hydra.Brain is MyrddraxisBrain)
						{
							hydra.Health += ad.Damage / 2;//dmg heals hydra
						}
					}
				}
				foreach (GameNPC heads in GetNPCsInRadius(2000))
				{
					if (heads != null)
					{
						if (heads.IsAlive && (heads.Brain is MyrddraxisSecondHeadBrain || heads.Brain is MyrddraxisThirdHeadBrain || heads.Brain is MyrddraxisFifthHeadBrain))
						{
							heads.Health += ad.Damage / 10;//dmg heals heads but not the one that is being attacked
						}
					}
				}
			}
			base.DealDamage(ad);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161055);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = -1;
			MaxSpeedBase = 0;
			++FourthHeadCount;

			Faction = FactionMgr.GetFactionByID(105);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

			MyrddraxisFourthHeadBrain sbrain = new MyrddraxisFourthHeadBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MyrddraxisFourthHeadBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MyrddraxisFourthHeadBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled3 = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled3 = false;
			}
			if (Body.IsAlive && HasAggro && Body.TargetObject != null)
			{
				Body.CastSpell(Head4_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (IsPulled3==false)
				{
					GameLiving ptarget = Body.TargetObject as GameLiving;
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisSecondHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisThirdHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisFifthHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					IsPulled3 = true;
				}
			}
			base.Think();
		}
		#region spells
		private Spell m_Head4_DD;
		private Spell Head4_DD
		{
			get
			{
				if (m_Head4_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(5, 8);
					spell.ClientEffect = 759;
					spell.Icon = 759;
					spell.TooltipId = 759;
					spell.Damage = 350;
					spell.Name = "Breath of Spirit";
					spell.Range = 2000;
					spell.SpellID = 11847;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Spirit;
					m_Head4_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Head4_DD);
				}
				return m_Head4_DD;
			}
		}
		#endregion
	}
}
#endregion
#region 5th Head of Myrddraxis
namespace DOL.GS
{
	public class MyrddraxisFifthHead : GameNPC
	{
		public MyrddraxisFifthHead() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Fifth Head of Myrddraxis Initializing...");
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
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 40000; }
		}
		public static int FifthHeadCount = 0;
		public override void Die(GameObject killer)
		{
			--FifthHeadCount;
			base.Die(killer);
		}
		public override void DealDamage(AttackData ad)
		{
			if (ad != null)
			{
				foreach (GameNPC hydra in GetNPCsInRadius(2000))
				{
					if (hydra != null)
					{
						if (hydra.IsAlive && hydra.Brain is MyrddraxisBrain)
						{
							hydra.Health += ad.Damage / 2;//dmg heals hydra
						}
					}
				}
				foreach (GameNPC heads in GetNPCsInRadius(2000))
				{
					if (heads != null)
					{
						if (heads.IsAlive && (heads.Brain is MyrddraxisSecondHeadBrain || heads.Brain is MyrddraxisThirdHeadBrain || heads.Brain is MyrddraxisFourthHeadBrain))
						{
							heads.Health += ad.Damage / 10;//dmg heals heads but not the one that is being attacked
						}
					}
				}
			}
			base.DealDamage(ad);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160835);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = -1;
			MaxSpeedBase = 0;
			++FifthHeadCount;

			Faction = FactionMgr.GetFactionByID(105);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

			MyrddraxisFifthHeadBrain sbrain = new MyrddraxisFifthHeadBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MyrddraxisFifthHeadBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MyrddraxisFifthHeadBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled4 = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled4 = false;
			}
			if (Body.IsAlive && HasAggro && Body.TargetObject != null)
			{
				Body.CastSpell(Head5_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (IsPulled4==false)
				{
					GameLiving ptarget = Body.TargetObject as GameLiving;
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisSecondHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisThirdHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisFourthHeadBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					foreach (GameNPC head in Body.GetNPCsInRadius(2000))
					{
						if (head != null)
						{
							if (head.IsAlive && head.Brain is MyrddraxisBrain brain)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(ptarget, 10);
							}
						}
					}
					IsPulled4 = true;
				}
			}
			base.Think();
		}
		#region spells
		private Spell m_Head5_DD;
		private Spell Head5_DD
		{
			get
			{
				if (m_Head5_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(5, 8);
					spell.ClientEffect = 219;
					spell.Icon = 219;
					spell.TooltipId = 219;
					spell.Damage = 350;
					spell.Name = "Breath of Matter";
					spell.Range = 2000;
					spell.SpellID = 11848;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Head5_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Head5_DD);
				}
				return m_Head5_DD;
			}
		}
		#endregion
	}
}
#endregion