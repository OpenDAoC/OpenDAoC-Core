/*
 * Author:	Kelteen & Glimmer
 * Date:	10.09.2021 
 * Modyfication Date: 06.01.2022 by Glimmer
 * This Script is for the Green Knight in Old Frontiers/RvR
 * Script is for interacting with players.
 * To create Boss type ingame /mob create DOL.GS.OFGreenKnight
 * Boss must be in Peace flag before starting fight, so players can interact with him
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.AI;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using System.Timers;
using DOL;
using DOL.GS.GameEvents;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.GS.Utils;
using DOL.GS.RealmAbilities;
using System.Threading;
using DOL.Language;
using log4net;

namespace DOL.GS
{
	public class OFGreenKnight : GameEpicBoss
	{
		public OFGreenKnight() : base() { }

		public static int TauntID = 103;
		public static int TauntClassID = 2;//armsman
		public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);
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
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100; //more str more dmg will he deal, modify ingame for easier adjust
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
		public override void Die(GameObject killer)//on kill generate orbs
		{
			// debug
			log.Debug($"{Name} killed by {killer.Name}");

			GamePlayer playerKiller = killer as GamePlayer;

			if (playerKiller?.Group != null)
			{
				foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
				{
					AtlasROGManager.GenerateOrbAmount(groupPlayer, 5000);//5k orbs for every player in group
				}
			}
			base.Die(killer);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161621);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			Faction = FactionMgr.GetFactionByID(236);// fellwoods
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(236));
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			BodyType = 6;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 46, 0, 0, 0); //Slot,model,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 48, 0);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 47, 0);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 49, 32, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 50, 32, 0, 0);
			template.AddNPCEquipment(eInventorySlot.Cloak, 57, 32, 0, 0);
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 7, 32, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);
			Styles.Add(taunt);
			MaxSpeedBase = 400;

			OFGreenKnightBrain.walk1 = false; OFGreenKnightBrain.atpoint1 = false;
			OFGreenKnightBrain.walk2 = false; OFGreenKnightBrain.atpoint2 = false;
			OFGreenKnightBrain.walk3 = false; OFGreenKnightBrain.atpoint3 = false;
			OFGreenKnightBrain.walk4 = false; OFGreenKnightBrain.atpoint4 = false;
			OFGreenKnightBrain.walk5 = false; OFGreenKnightBrain.Pick_healer = false;
			OFGreenKnightBrain.walk6 = false; OFGreenKnightBrain.IsSpawningTrees = false;
			OFGreenKnightBrain.walk7 = false; OFGreenKnightBrain.IsWalking = false;
			OFGreenKnightBrain.walk8 = false;
			OFGreenKnightBrain.walk9 = false;

			Flags = eFlags.PEACE;
			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Slash;
			OFGreenKnightBrain sbrain = new OFGreenKnightBrain();
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
			npcs = WorldMgr.GetNPCsByNameFromRegion("Green Knight", 1, (eRealm)0);
			if (npcs.Length == 0)
			{
				log.Warn("Green Knight not found, creating it...");

				log.Warn("Initializing Green Knight ...");
				OFGreenKnight OF = new OFGreenKnight();
				OF.Name = "Green Knight";
				OF.Model = 334;
				OF.Realm = 0;
				OF.Level = 79;
				OF.Size = 120;
				OF.CurrentRegionID = 1;//albion Forest sauvage
				OF.MeleeDamageType = eDamageType.Slash;
				OF.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
				OF.Faction = FactionMgr.GetFactionByID(236);
				OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(236));
				OF.BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;
				OF.MaxSpeedBase = 300;

				OF.X = 592990;
				OF.Y = 418687;
				OF.Z = 5012;
				OF.Heading = 3331;
				OFGreenKnightBrain ubrain = new OFGreenKnightBrain();
				OF.SetOwnBrain(ubrain);
				OF.AddToWorld();
				OF.SaveIntoDatabase();
				OF.Brain.Start();
			}
			else
				log.Warn("Green Knight exist ingame, remove it and restart server if you want to add by script code.");
		}
		//This function is the callback function that is called when
		//a player right clicks on the npc
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			//Now we turn the npc into the direction of the person it is
			//speaking to.
			TurnTo(player.X, player.Y);
			this.Emote(eEmote.Salute);
			//We send a message to player and make it appear in a popup
			//window. Text inside the [brackets] is clickable in popup
			//windows and will generate a /whis text command!
			player.Out.SendMessage(
				"You are wise to speak with me " + player.CharacterClass.Name + "! My forest is a delicate beast that can easily turn against you. " +
				"Should you wake the beast within, I must then rise to [defend it].",
				eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}

		//This function is the callback function that is called when
		//someone whispers something to this mob!
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!base.WhisperReceive(source, str))
				return false;

			//If the source is no player, we return false
			if (!(source is GamePlayer))
				return false;

			//We cast our source to a GamePlayer object
			GamePlayer t = (GamePlayer)source;

			//Now we turn the npc into the direction of the person it is
			//speaking to.
			TurnTo(t.X, t.Y);

			//We test what the player whispered to the npc and
			//send a reply. The Method SendReply used here is
			//defined later in this class ... read on
			switch (str)
			{
				case "defend it":
					{
						SendReply(t,
								  "Caution will be your guide through the dark places of Sauvage. " +
									  "Tread lightly " + t.CharacterClass.Name + "! I am ever watchful of my home!");
						if (t.IsAlive && t.IsAttackable)
						{
							Flags = 0;
							StartAttack(t);
						}
					}
					break;
				case "defend":
					{
						SendReply(t,
								  "Caution will be your guide through the dark places of Sauvage. " +
									  "Tread lightly " + t.CharacterClass.Name + "! I am ever watchful of my home!");
						if (t.IsAlive && t.IsAttackable)
						{
							Flags = 0;
							StartAttack(t);
						}
					}
					break;
				default:
					break;
			}
			return true;
		}
		public override void OnAttackEnemy(AttackData ad)
		{
			// 30% chance to proc heat dd
			if (Util.Chance(30))
			{
				//Here boss cast very X s aoe heat dmg, we can adjust it in spellrecast delay
				CastSpell(GreenKnightHeatDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}			
			base.OnAttackEnemy(ad);
		}		
		//This function sends some text to a player and makes it appear
		//in a popup window. We just define it here so we can use it in
		//the WhisperToMe function instead of writing the long text
		//everytime we want to send some reply!
		public void SendReply(GamePlayer target, string msg)
		{
			target.Out.SendMessage(
				msg,
				eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}
		
		#region Heat DD Spell
		private Spell m_HeatDDSpell;
		/// <summary>
		/// Casts Heat dd
		/// </summary>
		public Spell GreenKnightHeatDD
		{
			get
			{
				if (m_HeatDDSpell == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.Damage = 250;
					spell.DamageType = (int) eDamageType.Heat;
					spell.Name = "Might of the Forrest";
					spell.Range = 0;
					spell.Radius = 350;
					spell.SpellID = 11755;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Radius = 500;
					spell.EffectGroup = 0;
					m_HeatDDSpell= new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HeatDDSpell);
				}
				return m_HeatDDSpell;
			}
		}
		#endregion
	}
}
namespace DOL.AI.Brain
{
	public class OFGreenKnightBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OFGreenKnightBrain() : base() 
		{
			AggroLevel = 100;
			AggroRange = 600;
		}
		public override void AttackMostWanted()// mob doesnt attack
		{
			if (IsWalking == true)
				return;
			else
			{
				base.AttackMostWanted();
			}
		}
		public override void OnAttackedByEnemy(AttackData ad)//another check to not attack enemys
		{
			if (IsWalking == true)
				return;
			else
			{
				base.OnAttackedByEnemy(ad);
			}
		}
        #region GK pick random healer
        public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		List<GamePlayer> healer = new List<GamePlayer>();
		public int PickHeal(RegionTimer timer)
        {
			if (Body.IsAlive)
			{
				if (Body.InCombat && Body.IsAlive && HasAggro)
				{
					if (Body.TargetObject != null)
					{
						foreach (GamePlayer ppl in Body.GetPlayersInRadius(2500))
						{
							if (ppl != null)
							{
								if (ppl.IsAlive && ppl.Client.Account.PrivLevel == 1)
								{
									//cleric, bard, healer, warden, friar, druid, mentalist, shaman
									if (ppl.CharacterClass.ID is 6 or 48 or 26 or 46 or 10 or 47 or 42 or 28)
									{
										if (!healer.Contains(ppl))
										{
											healer.Add(ppl);
										}
									}
								}
							}
						}
						if (healer.Count > 0)
						{
							GamePlayer Target = (GamePlayer)healer[Util.Random(0, healer.Count - 1)];//pick random target from list
							RandomTarget = Target;//set random target to static RandomTarget
							if (RandomTarget != null)//check if it's not null
							{
								ClearAggroList();//clear aggro list or it may still stick to current target
								AddToAggroList(RandomTarget, 150);//set that target big aggro so boss will attack him
								Body.StartAttack(RandomTarget);//attack target
							}
							RandomTarget = null;//reset static ranmdomtarget to null
							Pick_healer = false;//reset flag
						}
					}
				}
			}
			return 0;
		}
        #endregion

        #region GK check flags & strings & PortPoints list
        public static bool Pick_healer = false;
		public static bool IsSpawningTrees = false;
		public static bool walk1 = false; public static bool atpoint1 = false;
		public static bool walk2 = false; public static bool atpoint2 = false;
		public static bool walk3 = false; public static bool atpoint3 = false;
		public static bool walk4 = false; public static bool atpoint4 = false;
		public static bool walk5 = false;
		public static bool walk6 = false;
		public static bool walk7 = false;
		public static bool walk8 = false;
		public static bool walk9 = false;
		
		public static bool IsWalking = false;
		public static bool IsAtPoint1 = false;
		public static bool IsAtPoint2 = false;
		public static bool IsAtPoint3 = false;
		public static bool IsAtPoint4 = false;
		public List<string> PortPoints = new List<string>();
		public static string string1 = "point1";
		public static string string2 = "point2";
		public static string string3 = "point3";
		public static string string4 = "point4";
        #endregion

        #region GK Teleport/Walk method
        public int GkTeleport(RegionTimer timer)
		{
			if (Body.IsAlive)
			{				
				Point3D point1 = new Point3D();
				point1.X = 593193; point1.Y = 416481; point1.Z = 4833;
				Point3D point2 = new Point3D();
				point2.X = 593256; point2.Y = 420780; point2.Z = 5050;
				Point3D point3 = new Point3D();
				point3.X = 596053; point3.Y = 420171; point3.Z = 4918;
				Point3D point4 = new Point3D();
				point4.X = 590876; point4.Y = 418052; point4.Z = 4942;
				if(!PortPoints.Contains(string1) && !PortPoints.Contains(string2) && !PortPoints.Contains(string3) && !PortPoints.Contains(string4))
                {
					if (atpoint1 == false)
					{
						PortPoints.Add(string1);
					}
					if (atpoint2 == false)
					{
						PortPoints.Add(string2);
					}
					if (atpoint3 == false)
					{
						PortPoints.Add(string3);
					}
					if (atpoint4 == false)
					{
						PortPoints.Add(string4);
					}
				}
				if (PortPoints.Count > 0)
				{
					foreach (string stg in PortPoints)
					{
						switch (stg)
						{
							//he will teleport away and heal himself(only once), only aggro again if pulled. Can be rupted to avoid being healed
							case "point1":
								{
									if (!Body.IsWithinRadius(point1, 50))
									{
										Body.StopAttack();
										Body.WalkTo(point1, 400);
										IsWalking = true;
									}
								}
								break;
							case "point2":
								{
									if (!Body.IsWithinRadius(point2, 50))
									{
										Body.StopAttack();
										Body.WalkTo(point2, 400);
										IsWalking = true;
									}
								}
								break;
							case "point3":
								{
									if (!Body.IsWithinRadius(point3, 50))
									{
										Body.StopAttack();
										Body.WalkTo(point3, 400);
										IsWalking = true;
									}
								}
								break;
							case "point4":
								{
									if (!Body.IsWithinRadius(point4, 50))
									{
										Body.StopAttack();
										Body.WalkTo(point4, 400);
										IsWalking = true;
									}
								}
								break;
						}
					}
				}
			}
			return 0;
		}
        #endregion
        public override void Think()
		{
			Point3D point1 = new Point3D();
			point1.X = 593193; point1.Y = 416481; point1.Z = 4833;
			Point3D point2 = new Point3D();
			point2.X = 593256; point2.Y = 420780; point2.Z = 5050;
			Point3D point3 = new Point3D();
			point3.X = 596053; point3.Y = 420171; point3.Z = 4918;
			Point3D point4 = new Point3D();
			point4.X = 590876; point4.Y = 418052; point4.Z = 4942;
			if (Body.IsAlive && Body.HealthPercent < 25)//mobs slow down when they got low hp
			{
				Body.CurrentSpeed = 300;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
                #region GK walking and healing
                if (Body.IsWithinRadius(point1,40) && atpoint1==false)
                {
					IsWalking = false;
					Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if(PortPoints.Contains(string1))
                    {
						PortPoints.Remove(string1);
                    }
					atpoint1 = true;
					atpoint2 = false;
					atpoint3 = false;
					atpoint4 = false;
				}
				else if (Body.IsWithinRadius(point2, 40) && atpoint2 == false)
				{
					IsWalking = false;
					Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (PortPoints.Contains(string2))
					{
						PortPoints.Remove(string2);
					}
					atpoint2 = true;
					atpoint1 = false;
					atpoint3 = false;
					atpoint4 = false;
				}
				if (Body.IsWithinRadius(point3, 40) && atpoint3 == false)
				{
					IsWalking = false;
					Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (PortPoints.Contains(string3))
					{
						PortPoints.Remove(string3);
					}
					atpoint3 = true;
					atpoint2 = false;
					atpoint1 = false;
					atpoint4 = false;
				}
				if (Body.IsWithinRadius(point4, 40) && atpoint4 == false)
				{
					IsWalking = false;
					Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (PortPoints.Contains(string4))
					{
						PortPoints.Remove(string4);
					}
					atpoint4 = true;
					atpoint2 = false;
					atpoint3 = false;
					atpoint1 = false;
				}
				if (Body.HealthPercent <= 90 && walk1==false)
                {
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk1 = true;
                }
				else if((Body.HealthPercent <= 80 && walk2 == false))
                {
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk2 = true;
				}
				else if ((Body.HealthPercent <= 70 && walk3 == false))
				{
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk3 = true;
				}
				else if ((Body.HealthPercent <= 60 && walk4 == false))
				{
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk4 = true;
				}
				else if ((Body.HealthPercent <= 50 && walk5 == false))
				{
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk5 = true;
				}
				else if ((Body.HealthPercent <= 40 && walk6 == false))
				{
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk6 = true;
				}
				else if ((Body.HealthPercent <= 30 && walk7 == false))
				{
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk7 = true;
				}
				else if ((Body.HealthPercent <= 20 && walk8 == false))
				{
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk8 = true;
				}
				else if ((Body.HealthPercent <= 10 && walk9 == false))
				{
					new RegionTimer(Body, new RegionTimerCallback(GkTeleport), 1000);
					walk9 = true;
				}
                #endregion
                if (Pick_healer==false)
                {
					new RegionTimer(Body, new RegionTimerCallback(PickHeal), Util.Random(40000, 60000));//40s-60s will try pick heal class
					Pick_healer=true;
				}
				if (IsSpawningTrees == false)
				{
					new RegionTimer(Body, new RegionTimerCallback(SpawnTrees), Util.Random(25000, 35000));//25s-35s will spawn trees
					IsSpawningTrees = true;
				}
				if(Body.TargetObject != null)
                {
					Body.styleComponent.NextCombatStyle = OFGreenKnight.taunt;
				}
			}
			//we reset him so he return to his orginal peace flag and max health and reseting pickheal phases
			if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
			{
				Body.Flags = GameNPC.eFlags.PEACE;
				Body.Health = Body.MaxHealth;
				Body.WalkToSpawn(400);//move boss back to his spawn point
				foreach (GameNPC npc in Body.GetNPCsInRadius(6500))
				{
					if (npc.Brain is GKTreesBrain)
					{
						//Remove all the trees
						npc.RemoveFromWorld();
					}
				}
				walk1 = false; atpoint1 = false;
				walk2 = false; atpoint2 = false;
				walk3 = false; atpoint3 = false;
				walk4 = false; atpoint4 = false;
				walk5 = false; Pick_healer = false;
				walk6 = false; IsSpawningTrees = false;
				walk7 = false; IsWalking = false;
				walk8 = false;
				walk9 = false;
			}
			base.Think();
		}		
		public int SpawnTrees(RegionTimer timer) // We define here adds
		{
			if (Body.IsAlive && Body.InCombat && HasAggro)
			{
				//spawning each tree in radius of 4000 on every player
				List<GamePlayer> player = new List<GamePlayer>();
				foreach (GamePlayer ppl in Body.GetPlayersInRadius(4000))
				{
					player.Add(ppl);

					if (ppl.IsAlive)
					{
						for (int i = 0; i <= player.Count - 1; i++)
						{
							GKTrees add = new GKTrees();
							add.X = ppl.X;
							add.Y = ppl.Y;
							add.Z = ppl.Z;
							add.CurrentRegion = Body.CurrentRegion;
							add.Heading = ppl.Heading;
							add.AddToWorld();
							add.StartAttack(ppl);
						}
					}
					player.Clear();
				}
				IsSpawningTrees = false;
			}
			return 0;
		}
		public Spell GreenKnightHeal
		{
			get
			{
				DBSpell spell = new DBSpell();
				spell.AllowAdd = false;
				spell.Uninterruptible = false;
				spell.Power = 0;
				spell.CastTime = 5;
				spell.ClientEffect = 4811;
				spell.RecastDelay = 5;
				spell.Icon = 4811;
				spell.Value = Body.MaxHealth / 10; //Modify here if heal is too strong
				spell.Duration = 0;
				spell.Name = "Holly Hand";
				spell.Range = 0;
				spell.SpellID = 4811;
				spell.Target = "Self";
				spell.Type = "Heal";
				spell.Radius = 0;
				spell.EffectGroup = 4801;
				return new Spell(spell, 50);
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class GKTreesBrain : StandardMobBrain
	{
		public GKTreesBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}
        public override void Think()
        {
			if(!HasAggressionTable())
            {
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            }
            base.Think();
        }
    }
}
namespace DOL.GS
{
	public class GKTrees : GameNPC
	{
		public override int MaxHealth
		{
			//trees got low hp, because they spawn preaty often. Modify here to adjust hp
			get { return 800 * Constitution / 100; } 
		}
		public override bool AddToWorld()
		{
			Model = 97;
			RoamingRange = 250;
			Strength = 150;
			Constitution = 100;
			RespawnInterval = -1;
			Size = (byte)Util.Random(90, 135);
			Level = (byte)Util.Random(47, 49); // Trees level
			Name = "rotting downy felwood";
			Faction = FactionMgr.GetFactionByID(236);// fellwoods
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(236));
			PackageID = "GreenKnightAdd";
			GKTreesBrain treesbrain = new GKTreesBrain();
			SetOwnBrain(treesbrain);
			treesbrain.AggroLevel = 100;
			treesbrain.AggroRange = 800;
			base.AddToWorld();
			return true;
		}

	}
}
