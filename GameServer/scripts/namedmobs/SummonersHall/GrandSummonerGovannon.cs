using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class GrandSummonerGovannon : GameEpicBoss
	{
		public GrandSummonerGovannon() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 70;// dmg reduction for melee dmg
				case eDamageType.Crush: return 70;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 70;// dmg reduction for melee dmg
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
			return 800;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.55;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public int BleedCast(RegionTimer timer)
        {
			
			return 0;
        }
        public override void OnAttackEnemy(AttackData ad)
        {
			if(ad != null && GrandSummonerGovannonBrain.Stage2==true)
            {
				if(Util.Chance(35))//30% chance to make a bleed
                {
					if (!ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Bleed))
					{
						this.CastSpell(Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
				}
            }
            base.OnAttackEnemy(ad);
        }
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(this.CurrentRegionID))
			{
				if (npc != null)
				{
					if (npc.IsAlive && (npc.Brain is SummonedDemonBrain || npc.Brain is SummonedSacrificeBrain || npc.Brain is ShadeOfAelfgarBrain))
					{
						npc.RemoveFromWorld();
					}
				}
			}
			base.Die(killer);
        }
        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18801);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Faction = FactionMgr.GetFactionByID(206);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			GrandSummonerGovannonBrain.SpawnSacrifices1 = false;
			GrandSummonerGovannonBrain.Stage2 = false;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 86, 43, 0, 0); //Slot,model,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 88, 43);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 87, 43);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 89, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 90, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.Cloak, 57, 65, 0, 0);
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 442, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			GrandSummonerGovannonBrain sbrain = new GrandSummonerGovannonBrain();
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

			npcs = WorldMgr.GetNPCsByNameFromRegion("Grand Summoner Govannon", 248, (eRealm)0);
			if (npcs.Length == 0)
			{
				log.Warn("Grand Summoner Govannon not found, creating it...");

				log.Warn("Initializing Grand Summoner Govannon...");
				GrandSummonerGovannon OF = new GrandSummonerGovannon();
				OF.Name = "Grand Summoner Govannon";
				OF.Model = 61;
				OF.Realm = 0;
				OF.Level = 80;
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
				OF.Faction = FactionMgr.GetFactionByID(206);
				OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

				OF.X = 34577;
				OF.Y = 31371;
				OF.Z = 15998;
				OF.MaxDistance = 2000;
				OF.TetherRange = 1300;
				OF.MaxSpeedBase = 250;
				OF.Heading = 19;
				OF.IsCloakHoodUp = true;

				GrandSummonerGovannonBrain ubrain = new GrandSummonerGovannonBrain();
				ubrain.AggroLevel = 100;
				ubrain.AggroRange = 600;
				OF.SetOwnBrain(ubrain);
				OF.AddToWorld();
				OF.Brain.Start();
				OF.SaveIntoDatabase();
			}
			else
				log.Warn("Grand Summoner Govannon exist ingame, remove it and restart server if you want to add by script code.");
		}
		private Spell m_Bleed;
		private Spell Bleed
		{
			get
			{
				if (m_Bleed == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2130;
					spell.Icon = 3411;
					spell.TooltipId = 3411;
					spell.Damage = 85;
					spell.Name = "Demon's Scar";
					spell.Description = "Does 85 damage to a target every 3 seconds for 30 seconds.";
					spell.Message1 = "You are bleeding! ";
					spell.Message2 = "{0} is bleeding! ";
					spell.Duration = 30;
					spell.Frequency = 30;
					spell.Range = 250;
					spell.SpellID = 11762;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.StyleBleeding.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Body;
					m_Bleed = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bleed);
				}
				return m_Bleed;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class GrandSummonerGovannonBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GrandSummonerGovannonBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool SpawnSacrifices1 = false;
		public static bool Stage2 = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
        public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Stage2 = false;
				SpawnSacrifices1 = false;
				Body.Health = Body.MaxHealth;
				foreach(GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
					if(npc != null)
                    {
						if(npc.IsAlive && (npc.Brain is SummonedDemonBrain || npc.Brain is SummonedSacrificeBrain || npc.Brain is ShadeOfAelfgarBrain))
                        {
							npc.RemoveFromWorld();
                        }
                    }
                }
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if(Stage2==true)//demon form
                {
					Body.CastSpell(GovannonDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				SpawnShadeOfAelfgar();
				foreach (GameNPC shade in Body.GetNPCsInRadius(4000))
				{
					if (shade != null)
					{
						if (shade.IsAlive && shade.Brain is ShadeOfAelfgarBrain)
						{
							AddAggroListTo(shade.Brain as ShadeOfAelfgarBrain);
						}
					}
				}
				if (Body.HealthPercent <= 80 && SpawnSacrifices1 == false)
				{
					if (Stage2 == false)
					{
						BroadcastMessage(String.Format(Body.Name + " gathers more strength."));
						Body.Empathy = 325;
						Body.Size = 80;
					}
					foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						if (player != null)
							player.Out.SendSpellEffectAnimation(Body, Body, 14215, 0, false, 1);
					}
					SpawnDemonAndSacrifice();
					SpawnSacrifices1 = true;
				}
				if(Body.HealthPercent <= 50 && Stage2==false)
                {
					MorphIntoDemon();
					Stage2 = true;
                }
			}
			if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
			{
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18801);
				Body.Health = Body.MaxHealth;
				Body.Model = Convert.ToUInt16(npcTemplate.Model);
				Body.Size = Convert.ToByte(npcTemplate.Size);
				Body.Empathy = Convert.ToInt16(npcTemplate.Empathy);
			}
			base.Think();
		}
		public void MorphIntoDemon()
        {
			Body.Health = Body.MaxHealth;//heal to max hp
			Body.Model = 636;//demon model
			Body.Size = 170;//bigger size
			Body.Empathy = 350;//more dmg
			SpawnSacrifices1 = false;
        }
		public void SpawnDemonAndSacrifice() // spawn sacrifice and demon
		{
			SummonedSacrifice Add1 = new SummonedSacrifice();
			Add1.X = 31018;
			Add1.Y = 40889;
			Add1.Z = 15491;
			Add1.CurrentRegionID = 248;
			Add1.Heading = 3054;
			Add1.LoadedFromScript = true;
			Add1.Faction = FactionMgr.GetFactionByID(187);
			Add1.AddToWorld();

			SummonedDemon Add2 = new SummonedDemon();
			Add2.X = 33215;
			Add2.Y = 40883;
			Add2.Z = 15491;
			Add2.CurrentRegionID = 248;
			Add2.Heading = 1004;
			Add2.LoadedFromScript = true;
			Add2.Faction = FactionMgr.GetFactionByID(187);
			Add2.AddToWorld();
		}
		public void SpawnShadeOfAelfgar()
        {
			if (SummonedSacrifice.SacrificeKilledCount == 1 && SummonedDemon.SummonedDemonCount == 1)//both summoned demon and sacrifice must be killed
			{
				if (ShadeOfAelfgar.ShadeOfAelfgarCount == 0)//make sure there is only 1 always
				{
					ShadeOfAelfgar Add1 = new ShadeOfAelfgar();
					Add1.X = 32128;
					Add1.Y = 41667;
					Add1.Z = 15491;
					Add1.CurrentRegionID = 248;
					Add1.Heading = 2030;
					Add1.LoadedFromScript = true;
					Add1.Faction = FactionMgr.GetFactionByID(187);
					Add1.AddToWorld();
					SummonedDemon.SummonedDemonCount = 0;
					SummonedSacrifice.SacrificeKilledCount = 0;
				}
			}
		}
		private Spell m_GovannonDot;
		public Spell GovannonDot
		{
			get
			{
				if (m_GovannonDot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 5;
					spell.RecastDelay = Util.Random(20,35);
					spell.ClientEffect = 585;
					spell.Icon = 585;
					spell.TooltipId = 585;
					spell.Damage = 150;
					spell.Frequency = 20;
					spell.Duration = 24;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Govannon's Shroud of Agony";
					spell.Description = "Inflicts 150 damage to the target every 3 sec for 36 seconds.";
					spell.Message1 = "Your body is covered with painful sores!";
					spell.Message2 = "{0}'s skin erupts in open wounds!";
					spell.Message3 = "The destructive energy wounding you fades.";
					spell.Message4 = "The destructive energy around {0} fades.";
					spell.Range = 1500;
					spell.Radius = 300;
					spell.SpellID = 11763;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.Type = eSpellType.DamageOverTime.ToString();
					m_GovannonDot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GovannonDot);
				}
				return m_GovannonDot;
			}
		}
	}
}
/// <summary>
/// //////////////////////////////////////////////////////////////////Summoned Sacrifice && Demon//////////////////////////////////////////////////////////////////
/// </summary>
#region Summoner Sacrifice
namespace DOL.GS
{
	public class SummonedSacrifice : GameNPC
	{
		public override int MaxHealth
		{
			get { return 6000; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 500;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
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
		public static int SacrificeKilledCount = 0;
        public override void Die(GameObject killer)
        {
            base.Die(killer);
        }
        public override bool AddToWorld()
		{
			Model = 122;
			Name = "summoned sacrifice";
			SacrificeKilledCount = 0;
			RespawnInterval = -1;
			Size = 45;
			Level = (byte)Util.Random(62, 68);
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			SummonedSacrificeBrain sacrifice = new SummonedSacrificeBrain();
			SetOwnBrain(sacrifice);
			base.AddToWorld();
			return true;
		}

	}
}
namespace DOL.AI.Brain
{
	public class SummonedSacrificeBrain : StandardMobBrain
	{
		public SummonedSacrificeBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 0;
		}
		public override void AttackMostWanted()// mob doesnt attack
		{
		}
		public override void OnAttackedByEnemy(AttackData ad)//another check to not attack enemys
		{
			base.OnAttackedByEnemy(ad);
		}
		public override void Think()
		{
			Point3D point1 = new Point3D(32063, 40896, 15468);
			if(!Body.IsWithinRadius(point1,40))
            {
				Body.WalkTo(point1, 35);
            }
			else
            {
				SummonedSacrifice.SacrificeKilledCount = 1;
				Body.Die(Body);//is at point so it die
            }
			base.Think();
		}
	}
}
#endregion

#region Summoned Demon
namespace DOL.GS
{
	public class SummonedDemon : GameNPC
	{
		public override int MaxHealth
		{
			get { return 6000; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 500;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
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
		public static int SummonedDemonCount = 0;
		public override void Die(GameObject killer)
		{
			base.Die(killer);
		}
		public override bool AddToWorld()
		{
			Model = 253;
			Name = "summoned demon";
			SummonedDemonCount = 0;
			RespawnInterval = -1;
			Size = 30;
			Level = (byte)Util.Random(62, 68);
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			SummonedDemonBrain sacrifice = new SummonedDemonBrain();
			SetOwnBrain(sacrifice);
			base.AddToWorld();
			return true;
		}

	}
}
namespace DOL.AI.Brain
{
	public class SummonedDemonBrain : StandardMobBrain
	{
		public SummonedDemonBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 0;
		}
		public override void AttackMostWanted()// mob doesnt attack
		{
		}
		public override void OnAttackedByEnemy(AttackData ad)//another check to not attack enemys
		{
			base.OnAttackedByEnemy(ad);
		}
		public override void Think()
		{
			Point3D point1 = new Point3D(32063, 40896, 15468);
			if (!Body.IsWithinRadius(point1, 40))
			{
				Body.WalkTo(point1, 35);
			}
			else
			{
				SummonedDemon.SummonedDemonCount = 1;
				Body.Die(Body);//is at point so it die
			}
			base.Think();
		}
	}
}
#endregion

#region Shade of Aelfgar
namespace DOL.GS
{
	public class ShadeOfAelfgar : GameEpicNPC
	{
		public override int MaxHealth
		{
			get { return 10000; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 600;
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
				default: return 75;// dmg reduction for rest resists
			}
		}
		public static int ShadeOfAelfgarCount = 0;
		public override void Die(GameObject killer)
		{
			++ShadeOfAelfgarCount;
			base.Die(killer);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18803);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			ShadeOfAelfgarBrain.RandomTarget = null;
			ShadeOfAelfgarBrain.CanPort = false;
			ShadeOfAelfgarCount = 0;
			RespawnInterval = -1;
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			ShadeOfAelfgarBrain sacrifice = new ShadeOfAelfgarBrain();
			SetOwnBrain(sacrifice);
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad)
		{
			if (Util.Chance(30))//30% chance to make a bleed
			{
				this.CastSpell(AelfgarStun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		private Spell m_AelfgarStun;
		public Spell AelfgarStun
		{
			get
			{
				if (m_AelfgarStun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(20, 35);
					spell.ClientEffect = 3379;
					spell.Icon = 3379;
					spell.TooltipId = 3379;
					spell.Duration = 5;
					spell.DamageType = (int)eDamageType.Spirit;
					spell.Name = "Aelfgar's Shout";
					spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
					spell.Message1 = "You are stunned!";
					spell.Message2 = "{0} is stunned!";
					spell.Message3 = "You recover from the stun.";
					spell.Message4 = "{0} recovers from the stun.";
					spell.Range = 350;
					spell.Radius = 500;
					spell.SpellID = 11764;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.Type = eSpellType.Stun.ToString();
					m_AelfgarStun = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AelfgarStun);
				}
				return m_AelfgarStun;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class ShadeOfAelfgarBrain : StandardMobBrain
	{
		public ShadeOfAelfgarBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		List<GamePlayer> Enemys_To_Port = new List<GamePlayer>();
		public static bool CanPort = false;
		public void PickRandomTarget()
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_Port.Contains(player))
						{
							Enemys_To_Port.Add(player);//add player to list
						}
					}
				}
			}
			if (Enemys_To_Port.Count > 0)
			{
				if (CanPort == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_Port[Util.Random(0, Enemys_To_Port.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					RandomTarget.MoveTo(Body.CurrentRegionID, 32091, 39684, 16302, 4094);
					int _resetPortTime = Util.Random(10000, 20000);
					ECSGameTimer _ResetPort = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetPort), _resetPortTime);//port every 10-20s
					_ResetPort.Start(_resetPortTime);
					CanPort = true;
				}
			}
		}
		public int ResetPort(ECSGameTimer timer)//reset here so boss can start dot again
		{
			RandomTarget = null;
			CanPort = false;
			return 0;
		}
		public override void Think()
		{
			if(!HasAggressionTable())
            {
				RandomTarget = null;
				CanPort = false;
				if (Enemys_To_Port.Count > 0)
				{
					Enemys_To_Port.Clear();//clear list if it reset
				}
			}
			if(Body.IsAlive && Body.InCombat && HasAggro)
            {
				if(Util.Chance(5))
                {
					PickRandomTarget();
                }
            }
			base.Think();
		}
	}
}
#endregion