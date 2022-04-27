using System;
using DOL.AI.Brain;
using DOL.Database;
using System.Collections.Generic;
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
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 40;// dmg reduction for rest resists
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
			if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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
			return 0.45;
		}
		public override int MaxHealth
		{
			get { return 15000; }
		}
        public override void Die(GameObject killer)
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
            base.Die(killer);
        }
        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166729);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(11);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(11));
			CreateMonsters = false;
			if(CreateMonsters==false)
            {
				CreatePitMonsters();
				CreateMonsters = true;
            }
			SyssroRuthlessBrain sbrain = new SyssroRuthlessBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public static bool CreateMonsters = false;
		public void CreatePitMonsters()
        {
			if (PitMonster.PitMonsterCount == 0)
			{
                #region Pit Monster location
                PitMonster Add = new PitMonster();
				Add.X = 41375;
				Add.Y = 40134;
				Add.Z = 7726;
				Add.CurrentRegion = CurrentRegion;
				Add.Heading = 2600;
				Add.AddToWorld();

				PitMonster Add2 = new PitMonster();
				Add2.X = 41484;
				Add2.Y = 40198;
				Add2.Z = 7725;
				Add2.CurrentRegion = CurrentRegion;
				Add2.Heading = 1818;
				Add2.AddToWorld();

				PitMonster Add3 = new PitMonster();
				Add3.X = 41695;
				Add3.Y = 40142;
				Add3.Z = 7730;
				Add3.CurrentRegion = CurrentRegion;
				Add3.Heading = 1287;
				Add3.AddToWorld();

				PitMonster Add4 = new PitMonster();
				Add4.X = 41916;
				Add4.Y = 40469;
				Add4.Z = 7724;
				Add4.CurrentRegion = CurrentRegion;
				Add4.Heading = 347;
				Add4.AddToWorld();

				PitMonster Add5 = new PitMonster();
				Add5.X = 41995;
				Add5.Y = 40874;
				Add5.Z = 7726;
				Add5.CurrentRegion = CurrentRegion;
				Add5.Heading = 2754;
				Add5.AddToWorld();

				PitMonster Add6 = new PitMonster();
				Add6.X = 41780;
				Add6.Y = 41185;
				Add6.Z = 7727;
				Add6.CurrentRegion = CurrentRegion;
				Add6.Heading = 48;
				Add6.AddToWorld();

				PitMonster Add7 = new PitMonster();
				Add7.X = 41488;
				Add7.Y = 41402;
				Add7.Z = 7736;
				Add7.CurrentRegion = CurrentRegion;
				Add7.Heading = 1438;
				Add7.AddToWorld();

				PitMonster Add8 = new PitMonster();
				Add8.X = 41261;
				Add8.Y = 41246;
				Add8.Z = 7728;
				Add8.CurrentRegion = CurrentRegion;
				Add8.Heading = 3480;
				Add8.AddToWorld();

				PitMonster Add9 = new PitMonster();
				Add9.X = 41001;
				Add9.Y = 40966;
				Add9.Z = 7727;
				Add9.CurrentRegion = CurrentRegion;
				Add9.Heading = 2868;
				Add9.AddToWorld();

				PitMonster Add10 = new PitMonster();
				Add10.X = 40978;
				Add10.Y = 40538;
				Add10.Z = 7727;
				Add10.CurrentRegion = CurrentRegion;
				Add10.Heading = 3357;
				Add10.AddToWorld();

				PitMonster Add11 = new PitMonster();
				Add11.X = 41335;
				Add11.Y = 40736;
				Add11.Z = 7707;
				Add11.CurrentRegion = CurrentRegion;
				Add11.Heading = 2775;
				Add11.AddToWorld();

				PitMonster Add12 = new PitMonster();
				Add12.X = 41712;
				Add12.Y = 40832;
				Add12.Z = 7712;
				Add12.CurrentRegion = CurrentRegion;
				Add12.Heading = 1342;
				Add12.AddToWorld();

				PitMonster Add13 = new PitMonster();
				Add13.X = 41564;
				Add13.Y = 40489;
				Add13.Z = 7711;
				Add13.CurrentRegion = CurrentRegion;
				Add13.Heading = 107;
				Add13.AddToWorld();

				PitMonster Add14 = new PitMonster();
				Add14.X = 41481;
				Add14.Y = 41083;
				Add14.Z = 7719;
				Add14.CurrentRegion = CurrentRegion;
				Add14.Heading = 1829;
				Add14.AddToWorld();

				PitMonster Add15 = new PitMonster();
				Add15.X = 41490;
				Add15.Y = 40758;
				Add15.Z = 7701;
				Add15.CurrentRegion = CurrentRegion;
				Add15.Heading = 1979;
				Add15.AddToWorld();
				#endregion
			}
        }
	}
}
namespace DOL.AI.Brain
{
	public class SyssroRuthlessBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsTargetPicked = false;
				IsPulled = false;
				RandomTarget = null;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
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
			if (source is GamePlayer || source is GamePet)
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
						truc = ((source as GamePet).Owner as GamePlayer);
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
		public static int PitMonsterCount = 0;
        public override void Die(GameObject killer)
        {
			--PitMonsterCount;
            base.Die(killer);
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
			++PitMonsterCount;

			Faction = FactionMgr.GetFactionByID(11);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(11));
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
					DBSpell spell = new DBSpell();
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
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.StyleSpeedDecrease.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Body;
					m_Snare = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Snare);
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
			foreach(GamePlayer player in Body.GetPlayersInRadius((ushort)Body.AttackRange))
            {
				if(player != null)
                {
					if(player.IsAlive && player.Client.Account.PrivLevel == 1 && !AggroTable.ContainsKey(player))
                    {
						AddToAggroList(player, 200);
                    }
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