using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class SyssroRuthless : GameEpicBoss
	{
		public SyssroRuthless() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Sys'sro the Ruthless Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20; // dmg reduction for melee dmg
				case eDamageType.Crush: return 20; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
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

		public override int MeleeAttackRange => 350;
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;
			return base.HasAbility(keyName);
		}
        public override void ProcessDeath(GameObject killer)
        {
			foreach(GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
            {
				if(npc != null)
                {
					if(npc.IsAlive && npc.Brain is PitMonsterBrain)
                    {
						npc.Die(npc);
                    }
                }
            }
            base.ProcessDeath(killer);
        }
        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166729);
			LoadTemplate(npcTemplate);
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Faction = FactionMgr.GetFactionByID(11);
			CreatePitMonsters();
			SyssroRuthlessBrain sbrain = new SyssroRuthlessBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		private readonly List<PitMonster> _pitMonsters = new List<PitMonster>();

		private static readonly (int X, int Y, int Z, ushort Heading)[] _pitMonsterSpawns =
		[
			(41375, 40134, 7726, 2600),
			(41484, 40198, 7725, 1818),
			(41695, 40142, 7730, 1287),
			(41916, 40469, 7724, 347),
			(41995, 40874, 7726, 2754),
			(41780, 41185, 7727, 48),
			(41488, 41402, 7736, 1438),
			(41261, 41246, 7728, 3480),
			(41001, 40966, 7727, 2868),
			(40978, 40538, 7727, 3357),
			(41335, 40736, 7707, 2775),
			(41712, 40832, 7712, 1342),
			(41564, 40489, 7711, 107),
			(41481, 41083, 7719, 1829),
			(41490, 40758, 7701, 1979)
		];

		public void CreatePitMonsters()
        {
			foreach (PitMonster monster in _pitMonsters)
			{
				if (monster != null && monster.IsAlive && monster.ObjectState is eObjectState.Active)
					return;
			}

			_pitMonsters.Clear();

			foreach ((int x, int y, int z, ushort heading) in _pitMonsterSpawns)
			{
				PitMonster add = new PitMonster();
				add.X = x;
				add.Y = y;
				add.Z = z;
				add.CurrentRegion = CurrentRegion;
				add.Heading = heading;
				add.AddToWorld();
				_pitMonsters.Add(add);
			}
        }

	}
}
namespace DOL.AI.Brain
{
	public class SyssroRuthlessBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SyssroRuthlessBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		public static bool IsTargetPicked = false;
		public static bool IsPulled = false;
		List<GamePlayer> Port_Enemys = new List<GamePlayer>();
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
								{
									Port_Enemys.Add(player);
								}
							}
						}
					}
				}
				if (Port_Enemys.Count > 0)
				{
					GamePlayer Target = (GamePlayer)Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
					RandomTarget = Target;
					if (RandomTarget.IsAlive && RandomTarget != null)
					{
						RandomTarget.MoveTo(50, 41489, 40699, 8145, 2096);
						Port_Enemys.Remove(RandomTarget);
						RandomTarget = null;//reset random target to null
						IsTargetPicked = false;
					}
				}
			}
			return 0;
		}
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsTargetPicked = false;
				IsPulled = false;
				RandomTarget = null;
			}
			if (HasAggro && Body.TargetObject != null)
			{
				if (IsTargetPicked == false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ThrowPlayer), Util.Random(10000, 15000));//timer to port and pick player
					IsTargetPicked = true;
                }
				if (IsPulled == false)
				{
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.PackageID == "SyssroBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with IssordenBaf PackageID
							}
						}
					}
					IsPulled = true;
				}
			}
			base.Think();
		}
	}
}
//////////////////////////////////////////////////////////////////////Pit snare mobs//////////////////////////////////////
namespace DOL.GS
{
	public class PitMonster : GameNPC
	{
		public PitMonster() : base() { }
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
					damageType == eDamageType.Energy || damageType == eDamageType.Heat
					|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit ||
					damageType == eDamageType.Crush || damageType == eDamageType.Thrust
					|| damageType == eDamageType.Slash)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GameSummonedPet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System,
							eChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
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
			get { return 5000; }
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
			{
				CastSpell(Snare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
        public override bool AddToWorld()
		{
			Model = 823;
			Name = "Sys'sro's Pit Monster";
			Size = 37;
			Level = (byte)Util.Random(60, 65);
			Strength = 80;
			Dexterity = 200;
			Constitution = 100;
			Quickness = 130;
			MaxSpeedBase = 0;
			Faction = FactionMgr.GetFactionByID(11);
			RespawnInterval = -1;
			PitMonsterBrain sbrain = new PitMonsterBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
		private Spell m_Snare;

		private Spell Snare
		{
			get
			{
				if (m_Snare == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2135;
					spell.Icon = 2135;
					spell.TooltipId = 2135;
					spell.Name = "Beast Snare";
					spell.Value = 60;
					spell.Duration = 30;
					spell.Range = 350;
					spell.SpellID = 11801;
					spell.Target = eSpellTarget.ENEMY.ToString();
					spell.Type = eSpellType.StyleSpeedDecrease.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Body;
					m_Snare = new Spell(spell, 70);
				}
				return m_Snare;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class PitMonsterBrain : StandardMobBrain
	{
		public PitMonsterBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 0;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			foreach(GamePlayer player in Body.GetPlayersInRadius((ushort)Body.attackComponent.AttackRange))
            {
				if(player != null)
                {
					if(player.IsAlive && player.Client.Account.PrivLevel == 1 && !IsInAggroList(player))
						AddToAggroList(player, 200);
                }
				if(player == null || !player.IsAlive)
                {
					ClearAggroList();
                }
            }
			base.Think();
		}
	}
}
