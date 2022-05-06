using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using System.Collections.Generic;
using DOL.Events;

namespace DOL.GS
{
	public class Krackenschtein : GameEpicBoss
	{
		public Krackenschtein() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Krackenschtein Initializing...");
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
			get { return 30000; }
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (!source.IsWithinRadius(this, 300)) //take no damage
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GamePet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(Name + " is immune to your damage!", eChatType.CT_System,
							eChatLoc.CL_ChatWindow);

					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override void StartAttack(GameObject target)
        {
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162981);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(82);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));
			MaxSpeedBase = 0;

			KrackenschteinBrain sbrain = new KrackenschteinBrain();
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
	public class KrackenschteinBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public KrackenschteinBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
        #region bolt random enemy
        public static bool CanCast = false;
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
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
							Enemys_To_DD.Add(player);
					}
				}
			}
			if (CanCast == false)
			{
				GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
				RandomTarget = Target;//set random target to static RandomTarget
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastBolt), 1000);
				CanCast = true;
			}
		}
		public int CastBolt(ECSGameTimer timer)
		{
			GamePlayer oldTarget = (GamePlayer)Body.TargetObject;//old target
			if (RandomTarget != null && RandomTarget.IsAlive && !Body.IsCasting)
			{
				Body.TargetObject = RandomTarget;
				Body.TurnTo(RandomTarget);
				Body.CastSpell(Boss_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetBolt), Util.Random(15000, 20000));
			return 0;
		}
		public int ResetBolt(ECSGameTimer timer)//reset here so boss can start dot again
		{
			RandomTarget = null;
			CanCast = false;
			return 0;
		}
		#endregion
		#region Teleport random enemy
		public static bool CanPort = false;
		public static GamePlayer teleporttarget = null;
		public static GamePlayer TeleportTarget
		{
			get { return teleporttarget; }
			set { teleporttarget = value; }
		}
		List<GamePlayer> Enemys_To_Port = new List<GamePlayer>();
		public void TeleportRandomTarget()
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_Port.Contains(player))
							Enemys_To_Port.Add(player);
					}
				}
			}
			if (CanPort == false)
			{
				GamePlayer Target = (GamePlayer)Enemys_To_Port[Util.Random(0, Enemys_To_Port.Count - 1)];//pick random target from list
				TeleportTarget = Target;//set random target to static RandomTarget
				switch(Util.Random(1,4))
                {
					case 1: TeleportTarget.MoveTo(180, 32956, 37669, 16465, 1028); break;
					case 2: TeleportTarget.MoveTo(180, 31879, 38149, 16465, 2109); break;
					case 3: TeleportTarget.MoveTo(180, 31727, 37401, 16465, 3225); break;
					case 4: TeleportTarget.MoveTo(180, 32159, 36387, 16465, 3618); break;
				}
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetPort), Util.Random(25000, 35000));
				CanPort = true;			
			}
		}
		public int ResetPort(ECSGameTimer timer)//reset here so boss can start dot again
		{
			TeleportTarget = null;
			CanPort = false;
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
				CanCast = false;
				CanPort = false;
				RandomTarget = null;
				TeleportTarget = null;
				if(Enemys_To_DD.Count>0)
                {
					Enemys_To_DD.Clear();
                }
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "KrackenschteinBaf")
							AddAggroListTo(npc.Brain as StandardMobBrain);
					}
				}
				if(!Body.IsCasting)
					Body.CastSpell(Boss_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

				PickRandomTarget();
				TeleportRandomTarget();
			}
			base.Think();
		}
		private Spell m_Boss_Bolt;
		private Spell Boss_Bolt
		{
			get
			{
				if (m_Boss_Bolt == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4559;
					spell.Icon = 4559;
					spell.TooltipId = 4559;
					spell.Name = "Bolt of Doom";
					spell.Damage = 250;
					spell.Range = 1800;
					spell.SpellID = 11835;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.Bolt.ToString();
					spell.DamageType = (int)eDamageType.Cold;
					spell.Uninterruptible = true;
					m_Boss_Bolt = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_Bolt);
				}
				return m_Boss_Bolt;
			}
		}
		private Spell m_Boss_PBAOE;
		private Spell Boss_PBAOE
		{
			get
			{
				if (m_Boss_PBAOE == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = Util.Random(4,8);
					spell.ClientEffect = 1695;
					spell.Icon = 1695;
					spell.TooltipId = 1695;
					spell.Name = "Thunder Stomp";
					spell.Damage = 250;
					spell.Range = 0;
					spell.Radius = 1000;
					spell.SpellID = 11836;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Energy;
					spell.Uninterruptible = true;
					m_Boss_PBAOE = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_PBAOE);
				}
				return m_Boss_PBAOE;
			}
		}
	}
}

