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
	public class OFGreenKnight : GameNPC
	{
		public OFGreenKnight() : base() { }
		public static GameNPC greenKnight = new GameNPC();

		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100; //more str more dmg will he deal, modify ingame for easier adjust
		}
		
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161621);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Constitution = npcTemplate.Constitution;
			Dexterity = npcTemplate.Dexterity;
			Quickness = npcTemplate.Quickness;
			Empathy = npcTemplate.Empathy;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;

			// humanoid
			BodyType = 6;
			Race = 2005;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 46, 0, 0, 0); //Slot,model,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 48, 0);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 47, 0);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 49, 32, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 50, 32, 0, 0);
			template.AddNPCEquipment(eInventorySlot.Cloak, 57, 32, 0, 0);
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 7, 32, 0, 0);
			Inventory = template.CloseTemplate();

			Model = 334;
			Name = "Green Knight";
			Level = 75;
			Gender = eGender.Male;

			// twohanded
			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Slash;
			
			//must be peace on start, unless he will be aggresive
			Flags = eFlags.PEACE;

			OFGreenKnightBrain sBrain = new OFGreenKnightBrain();
			SetOwnBrain(sBrain);
			sBrain.AggroLevel = 100;
			sBrain.AggroRange = 800;
			base.AddToWorld();

			return true;
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
					spell.RecastDelay = 0;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.Damage = 300;
					spell.DamageType = (int) eDamageType.Heat;
					spell.Name = "Might of the Forrest";
					spell.Range = 1000;
					spell.SpellID = 360;
					spell.Target = "Enemy";
					spell.Type = "DirectDamage";
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
		public OFGreenKnightBrain() : base() { }

		/// <summary>
		/// Picking random healer bools check
		/// </summary>
		public static bool pickheal1 = true;
		public static bool pickheal2 = true;
		public static bool pickheal3 = true;
		public static bool pickheal4 = true;
		public static bool pickheal5 = true;
		public static bool pickheal6 = true;
		public static bool pickheal7 = true;
		public static bool pickheal8 = true;
		public static bool pickheal9 = true;
		/// /////////////////
		/// Spawnning trees at health stages////

		public static bool spawntree1 = true;
		public static bool spawntree2 = true;
		public static bool spawntree3 = true;
		public static bool spawntree4 = true;
		public static bool spawntree5 = true;
		public static bool spawntree6 = true;
		public static bool spawntree7 = true;
		public static bool spawntree8 = true;
		public static bool spawntree9 = true;
		/// /////////////////
		/// 
		public void PickHeal()
        {
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					List<GamePlayer> healer = new List<GamePlayer>();
					
					foreach (GamePlayer ppl in Body.GetPlayersInRadius(2500))
					{
						if (ppl.IsAlive)
						{
							//cleric, bard, healer, warden, friar, druid, mentalist, shaman
							if (ppl.CharacterClass.ID is 6 or 48 or 26 or 46 or 10 or 47 or 42 or 28) 
							{
								healer.Add(ppl);
							}
							else
							{
								Body.StartAttack(ppl);
								healer.Clear();
								break;
							}
						}
					}
					//pick random heal class from list
					int ptarget = Util.Random(0, healer.Count - 1); 
					
					if (ptarget >= 0)
					{
						GamePlayer enemy = healer[ptarget];
						if (Body.AttackState)
						{
							//boss stop attack
							ClearAggroList();
							Body.StopAttack();
							
							//boss pick his heal class
							Body.StartAttack(enemy);
						}
					}
					healer.Clear();
					
				}
			}
		}
		public void GkTeleport()
        {
	        //teleport chance and heal, modify here to adjust
			if(Util.Chance(3))
            {
				int randPortLoc = Util.Random(1, 4);
				if (Body.InCombat && HasAggro)
                {
					switch (randPortLoc)
					{
						//he will teleport away and heal himself(only once), only aggro again if pulled. Can be rupted to avoid being healed
						case 1:
							{
								Body.MoveTo(1, 593193, 416481, 4833, 4029);
								if (!Body.IsCasting)
								{
									Body.Flags = 0;
									Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
								}
							}
							break;
						case 2:
							{
								Body.MoveTo(1, 593256, 420780, 5050, 2005);
								if (!Body.IsCasting)
								{
									Body.Flags = 0;
									Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
								}
							}
							break;
						case 3:
							{
								Body.MoveTo(1, 596053, 420171, 4918, 1164);
								if (!Body.IsCasting)
								{
									Body.Flags = 0;
									Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
								}
							}
							break;
						case 4:
							{
								Body.MoveTo(1, 590876, 418052, 4942, 3271);
								if (!Body.IsCasting)
								{
									Body.Flags = 0;
									Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
								}
							}
							break;
						default:
							break;
					}
                }
            }
        }
		
		public override void Think()
		{
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if (Body.HealthPercent < 100)
					{
						//Boss teleport method
						GkTeleport();
					}
					
					if(Body.HealthPercent <= 90 && Body.HealthPercent > 80)
                    {
	                    switch (Util.Random(1,2))
	                    {
		                    case 1:
			                    if (pickheal1)
			                    {
				                    PickHeal();
				                    pickheal1 = false;
			                    }
			                    break;
		                    case 2:
			                    if (spawntree1)
			                    {
				                    Spawn();
				                    spawntree1 = false;
			                    }
			                    break;
	                    }
                    }
					if (Body.HealthPercent <= 80 && Body.HealthPercent > 70)
					{
						switch (Util.Random(1,2))
						{
							case 1:
								if (pickheal2)
								{
									PickHeal();
									pickheal2 = false;
								}
								break;
							case 2:
								if (spawntree2)
								{
									Spawn();
									spawntree2 = false;
								}
								break;
						}
					}
					if (Body.HealthPercent <= 70 && Body.HealthPercent > 60)
					{
						switch (Util.Random(1,2))
						{
							case 1:
								if (pickheal3)
								{
									PickHeal();
									pickheal3 = false;
								}
								break;
							case 2:
								if (spawntree3)
								{
									Spawn();
									spawntree3 = false;
								}
								break;
						}
					}
					if (Body.HealthPercent <= 60 && Body.HealthPercent > 50)
					{
						switch (Util.Random(1,2))
						{
							case 1:
								if (pickheal4)
								{
									PickHeal();
									pickheal4 = false;
								}
								break;
							case 2:
								if (spawntree4)
								{
									Spawn();
									spawntree4 = false;
								}
								break;
						}
					}
					if (Body.HealthPercent <= 50 && Body.HealthPercent > 40)
					{
						switch (Util.Random(1,2))
						{
							case 1:
								if (pickheal5)
								{
									PickHeal();
									pickheal5 = false;
								}
								break;
							case 2:
								if (spawntree5)
								{
									Spawn();
									spawntree5 = false;
								}
								break;
						}
					}
					if (Body.HealthPercent <= 40 && Body.HealthPercent > 30)
					{
						switch (Util.Random(1,2))
						{
							case 1:
								if (pickheal6)
								{
									PickHeal();
									pickheal6 = false;
								}
								break;
							case 2:
								if (spawntree6)
								{
									Spawn();
									spawntree6 = false;
								}
								break;
						}
					}
					if (Body.HealthPercent <= 30 && Body.HealthPercent > 20)
					{
						switch (Util.Random(1,2))
						{
							case 1:
								if (pickheal7)
								{
									PickHeal();
									pickheal7 = false;
								}
								break;
							case 2:
								if (spawntree7)
								{
									Spawn();
									spawntree7 = false;
								}
								break;
						}
					}
					if (Body.HealthPercent <= 20 && Body.HealthPercent > 10)
					{
						switch (Util.Random(1,2))
						{
							case 1:
								if (pickheal8)
								{
									PickHeal();
									pickheal8 = false;
								}
								break;
							case 2:
								if (spawntree8)
								{
									Spawn();
									spawntree8 = false;
								}
								break;
						}
					}
					if (Body.HealthPercent <= 10 && Body.HealthPercent > 1)
					{
						switch (Util.Random(1,2))
						{
							case 1:
								if (pickheal9)
								{
									PickHeal();
									pickheal9 = false;
								}
								break;
							case 2:
								if (spawntree9)
								{
									Spawn();
									spawntree9 = false;
								}
								break;
						}
					}
				}

			}
			//we reset him so he return to his orginal peace flag and max health and reseting pickheal phases
			if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
			{
				Body.Flags = GameNPC.eFlags.PEACE;
				Body.Health = Body.MaxHealth;
				Body.WalkToSpawn(300);//move boss back to his spawn point
				pickheal1 = true;
				pickheal2 = true;
				pickheal3 = true;
				pickheal4 = true;
				pickheal5 = true;
				pickheal6 = true;
				pickheal7 = true;
				pickheal8 = true;
				pickheal9 = true;

				spawntree1 = true;
				spawntree2 = true;
				spawntree3 = true;
				spawntree4 = true;
				spawntree5 = true;
				spawntree6 = true;
				spawntree7 = true;
				spawntree8 = true;
				spawntree9 = true;

				foreach (GameNPC npc in Body.GetNPCsInRadius(6500))
				{
					if (npc.Brain is GKTreesBrain)
					{
						//Remove all the trees
						npc.RemoveFromWorld();
					}
				}
			}
			base.Think();
		}
			
		public void Spawn() // We define here adds
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
		}
		public Spell GreenKnightHeal
		{
			get
			{
				DBSpell spell = new DBSpell();
				spell.AllowAdd = false;
				spell.Uninterruptible = true;
				spell.Power = 0;
				spell.CastTime = 2;
				spell.ClientEffect = 4811;
				spell.RecastDelay = 0;
				spell.Icon = 4811;
				spell.Value = (double) Body.MaxHealth / 95; //Modify here if heal is too strong
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
			AggroRange = 500;
		}

		//remove minions if they were last in combat and they have no aggro anymore
		public override void Think()
		{
			if (!HasAggro && Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
			{
				Body.RemoveFromWorld();
			}
			base.Think();
		}
		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			base.Notify(e, sender, args);
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
			get { return 600 * Constitution / 100; } 
		}
		public override bool AddToWorld()
		{
			Model = 97;
			RoamingRange = 250;
			Constitution = 100;
			RespawnInterval = -1;
			Size = (byte)Util.Random(90, 135);
			Level = (byte)Util.Random(47, 49); // Trees level
			Name = "rotting downy felwood";
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
