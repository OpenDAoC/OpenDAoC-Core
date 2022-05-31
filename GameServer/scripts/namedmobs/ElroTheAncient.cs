using System;
using System.Reflection;
using DOL.AI;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Database;

namespace DOL.GS.Scripts
{
	public class ElroTheAncient : GameEpicBoss
	{
		public ElroTheAncient()
		{
			TetherRange = 4500;
			ScalingFactor = 55;
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
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 300; }
		#endregion
		public override bool AddToWorld()
		{
			this.Name = "Elro the Ancient";
			this.GuildName = "";
			this.Model = 767;
			this.Size = 150;
			this.Level = 65;
			this.Realm = eRealm.None;
			base.AddToWorld();
			return true;
		}

		public void Spawn(GamePlayer player)
		{
			GameNPC mob = new GameNPC();
			SetVariables(mob);
			//Level Range of 50-55
			int level = Util.Random(50, 55);
			mob.Level = (byte) level;
			mob.Size = 50;
			mob.AddToWorld();

			mob.StartAttack(player);
		}

		public void SetVariables(GameNPC mob)
		{
			mob.X = this.X + 350;
			mob.Y = this.Y + 350;
			mob.Z = this.Z;
			mob.CurrentRegion = this.CurrentRegion;
			mob.Heading = this.Heading;
			//mob.Level = this.Level;
			mob.Realm = this.Realm;
			mob.Name = "ancient treant";
			mob.Model = this.Model;
			mob.Size = 50;
			mob.Flags = this.Flags;
			mob.MeleeDamageType = this.MeleeDamageType;
			mob.RespawnInterval = -1; // dont respawn
			mob.RoamingRange = this.RoamingRange;
			mob.MaxDistance = 4000;

			// also copies the stats

			mob.Strength = this.Strength;
			mob.Constitution = this.Constitution;
			mob.Dexterity = this.Dexterity;
			mob.Quickness = this.Quickness;
			mob.Intelligence = this.Intelligence;
			mob.Empathy = this.Empathy;
			mob.Piety = this.Piety;
			mob.Charisma = this.Charisma;

			//Fill the living variables
			mob.CurrentSpeed = 200;

			mob.MaxSpeedBase = this.MaxSpeedBase;
			mob.Size = this.Size;
			mob.NPCTemplate = this.NPCTemplate;
			mob.Inventory = this.Inventory;
			mob.EquipmentTemplateID = this.EquipmentTemplateID;
			if (mob.Inventory != null)
				mob.SwitchWeapon(this.ActiveWeaponSlot);

			ABrain brain = null;
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				brain = (ABrain) assembly.CreateInstance(this.Brain.GetType().FullName, true);
				if (brain != null)
					break;
			}

			if (brain == null)
			{
				mob.SetOwnBrain(new StandardMobBrain());
			}
			else if (brain is StandardMobBrain)
			{
				StandardMobBrain sbrain = (StandardMobBrain) brain;
				StandardMobBrain tsbrain = (StandardMobBrain) this.Brain;
				sbrain.AggroLevel = tsbrain.AggroLevel;
				sbrain.AggroRange = tsbrain.AggroRange;
				mob.SetOwnBrain(sbrain);
			}
		}

		public override void Die(GameObject killer)
		{
			base.Die(killer);
			foreach (GameNPC npc in this.GetNPCsInRadius(5000))
			{
				if (npc.Name.Contains("ancient treant"))
				{
					npc.Die(killer);
				}
			}
		}

		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			GamePlayer player = source as GamePlayer;
			if (player != null)
			{
				if (this.HealthPercent < 95 && this.HealthPercent > 90)
				{
					new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(timer => CastTreant(timer, player)), 1000);
					
				}
				
				else if (this.HealthPercent < 60 && this.HealthPercent > 55)
				{
					new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(timer => CastTreant(timer, player)), 1000);
					
				}
				
				else if (this.HealthPercent < 25 && this.HealthPercent > 30)
				{
					new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(timer => CastTreant(timer, player)), 1000);
					
				}
			}

			base.TakeDamage(source, damageType, damageAmount, criticalAmount);
		}

		private int CastTreant(ECSGameTimer timer, GamePlayer player)
		{
			foreach (GamePlayer enemy in this.GetPlayersInRadius(2500))
			{
				Spawn(enemy);
			}
			
			return 0;
		}

		public void SendReply(GamePlayer player, string msg)
		{
			player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}
		
	}

}