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
			Size = 120;
			Level = 75;
			Gender = eGender.Male;

			VisibleActiveWeaponSlots = (byte) eActiveWeaponSlot.TwoHanded;
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
		/*
		private static int TauntID = 103;
		private static int TauntClassID = 2;
		Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

		protected override Style GetStyleToUse()
		{
			if (this.TargetObject == null)
			{
				return base.GetStyleToUse();
			}
			if (this.TargetObject is GameLiving) // on definie la position par rapport a la cible
			{
				GameLiving living = this.TargetObject as GameLiving;


				if (Util.Chance(100)) //100% chances to use this style unless we change it
				{
					if (living.IsAlive)
					{
						Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID); // taunt
						if (taunt != null)
						{
							this.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
							this.ParryChance = 15;//He parry alot!
							return taunt;
						}
					}
				}
			}
			return base.GetStyleToUse();
		}
		*/

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
			if (Body.InCombat == true && Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					IList pplayer = new ArrayList(); //list of heal player classses
					if (Body.TargetObject != null)
					{
						foreach (GamePlayer ppl in Body.GetPlayersInRadius(2500))
						{
							if (ppl.IsAlive)
							{
								if (ppl.CharacterClass.ID == 6     //cleric
									|| ppl.CharacterClass.ID == 48 //bard
									|| ppl.CharacterClass.ID == 26 //healer
									|| ppl.CharacterClass.ID == 46 //warden
									|| ppl.CharacterClass.ID == 10 //friar
									|| ppl.CharacterClass.ID == 47 //druid
									|| ppl.CharacterClass.ID == 42 //mentalist
									|| ppl.CharacterClass.ID == 28)//shaman
								{
									if (pplayer.Contains(ppl))
									{
									}
									else
									{
										pplayer.Add(ppl); //adding heal class to  list
									}
								}
							}
						}
						if (pplayer.Count > 0)//player list must be more than 0!
						{
							GamePlayer ptarget = (GamePlayer)pplayer[Util.Random(0, pplayer.Count - 1)]; //pick random heal class from list
							if (Body.AttackState == true)
							{
								ClearAggroList();
								Body.StopAttack();//boss stop attack
								Body.TargetObject = ptarget;//boss pick his heal class
								Body.Follow(ptarget, 50, 4000); //boss follow him

								if (Body.GetDistanceTo(ptarget) < 50)//Boss is away from target
								{
									if (AggroTable.Count > 0)
									{
										ClearAggroList();
									}

									Body.CurrentSpeed = 600;
								}
								else if (Body.GetDistanceTo(ptarget) > 50)//Boss is close to target
								{
									AddToAggroList(ptarget, 100);
									Body.CurrentSpeed = 300;
								}

								//ClearAggroList();
								if (Body.IsWithinRadius(ptarget, 100))
								{
									Body.StartAttack(ptarget);//boss attack target
								}
							}
						}
					}
				}
			}
		}
		public IList PlayersToAttack = new ArrayList();
		public void GKTeleport()
        {
			if(Util.Chance(10))//teleport chance and heal, modify here to adjust
            {
				int RandPortLoc = Util.Random(1, 4);
				if (Body.InCombat==true && HasAggro)
                {
					switch (RandPortLoc)
					{
						case 1:
							{
								//he will teleport away and heal himself(only once), only aggro again if pulled. Can be rupted to avoid being healed
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

					Body.CastSpell(GreenKnightHeatDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//Here boss cast very X s aoe heat dmg, we can adjust it in spellrecast delay

					if (Body.HealthPercent < 100)
					{
						GKTeleport();//Boss teleport method
					}
					if(Body.HealthPercent<=90 && Body.HealthPercent >80)
                    {
						if (pickheal1 == true)
						{
							PickHeal();
							pickheal1 = false;
						}
						else if(spawntree1 == true)
                        {
							Spawn();
							spawntree1 = false;
                        }
                    }
					if (Body.HealthPercent <= 80 && Body.HealthPercent > 70)
					{
						if (pickheal2 == true)
						{
							PickHeal();
							pickheal2 = false;
						}
						else if (spawntree2 == true)
						{
							Spawn();
							spawntree2 = false;
						}
					}
					if (Body.HealthPercent <= 70 && Body.HealthPercent > 60)
					{
						if (pickheal3 == true)
						{
							PickHeal();
							pickheal3 = false;
						}
						else if (spawntree3 == true)
						{
							Spawn();
							spawntree3 = false;
						}
					}
					if (Body.HealthPercent <= 60 && Body.HealthPercent > 50)
					{
						if (pickheal4 == true)
						{
							PickHeal();
							pickheal4 = false;
						}
						else if (spawntree4 == true)
						{
							Spawn();
							spawntree4 = false;
						}
					}
					if (Body.HealthPercent <= 50 && Body.HealthPercent > 40)
					{
						if (pickheal5 == true)
						{
							PickHeal();
							pickheal5 = false;
						}
						else if (spawntree5 == true)
						{
							Spawn();
							spawntree5 = false;
						}
					}
					if (Body.HealthPercent <= 40 && Body.HealthPercent > 30)
					{
						if (pickheal6 == true)
						{
							PickHeal();
							pickheal6 = false;
						}
						else if (spawntree6 == true)
						{
							Spawn();
							spawntree6 = false;
						}
					}
					if (Body.HealthPercent <= 30 && Body.HealthPercent > 20)
					{
						if (pickheal7 == true)
						{
							PickHeal();
							pickheal7 = false;
						}
						else if (spawntree7 == true)
						{
							Spawn();
							spawntree7 = false;
						}
					}
					if (Body.HealthPercent <= 20 && Body.HealthPercent > 10)
					{
						if (pickheal8 == true)
						{
							PickHeal();
							pickheal8 = false;
						}
						else if (spawntree8 == true)
						{
							Spawn();
							spawntree8 = false;
						}
					}
					if (Body.HealthPercent <= 10 && Body.HealthPercent > 1)
					{
						if (pickheal9 == true)
						{
							PickHeal();
							pickheal9 = false;
						}
						else if (spawntree9 == true)
						{
							Spawn();
							spawntree9 = false;
						}
					}
				}

			}
			//we reset him so he return to his orginal peace flag and max health and reseting pickheal phases
			if (Body.InCombatInLast(40 * 1000) == false && this.Body.InCombatInLast(45 * 1000))
			{
				Body.Flags = GameNPC.eFlags.PEACE;
				this.Body.Health = this.Body.MaxHealth;
				Body.MoveTo(1, 592882, 418797, 5008, 3406);//move boss back to his spawn point
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

				foreach (GameNPC npc in WorldMgr.GetNPCsByName("rotting downy felwood", eRealm.None))
				{
					if (npc.RespawnInterval == -1 && npc.PackageID== "GreenKnightAdd")
					{
						if (npc.Brain is GKTreesBrain)
						{
							//Remove all the minions
							npc.RemoveFromWorld();
						}
					}
				}
			}
			base.Think();
		}
			
		public void Spawn() // We define here adds
		{
			foreach (GamePlayer ppl in Body.GetPlayersInRadius(4000))//spawning each tree in radius of 4000 on every player
			{
				if (ppl.IsAlive)
				{
					GKTrees Add = new GKTrees();
					Add.X = ppl.X;
					Add.Y = ppl.Y;
					Add.Z = ppl.Z;
					Add.CurrentRegion = Body.CurrentRegion;
					Add.Heading = ppl.Heading;
					Add.AddToWorld();
				}
			}
		}
		public Spell GreenKnightHeal
		{
			get
			{
				DBSpell spell = new DBSpell();
				spell.AllowAdd = false;
				spell.CastTime = 8; // 8s cast time to give players chance to rupt him
				spell.ClientEffect = 1414;
				spell.RecastDelay = 8;
				spell.Icon = 1414;
				spell.Value = Body.MaxHealth / 10; //Modify here if heal is too strong
				spell.Duration = 0;
				spell.Name = "Holly Hand";
				spell.Range = 1650;
				spell.SpellID = 140004;
				spell.Target = "Self";
				spell.Type = "Heal";
				spell.Radius = 0;
				spell.EffectGroup = 0;
				return new Spell(spell, 50);
			}
		}

		public Spell GreenKnightHeatDD
		{
			get
			{
				DBSpell spell = new DBSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 20;
				spell.ClientEffect = 368;
				spell.Icon = 368;
				spell.Damage = 100;
				spell.DamageType = 13;
				spell.Name = "Might of the Forrest";
				spell.Range = 1650;
				spell.SpellID = 140005;
				spell.Target = "Enemy";
				spell.Type = "DirectDamage";
				spell.Radius = 500;
				spell.EffectGroup = 0;
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
			get { return 600 * Constitution / 100; } //trees got low hp, because they spawn preaty often. Modify here to adjust hp
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
