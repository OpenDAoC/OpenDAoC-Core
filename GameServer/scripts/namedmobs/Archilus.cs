using System;
using System.Reflection;
using DOL.AI;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class Archilus : GameNPC
	{
		protected String m_SpawnAnnounce;

		public Archilus()
		{
			m_SpawnAnnounce = "{0} will start to \'shake violently\' and spawns out some {1}!";
			TetherRange = 4500;
			ScalingFactor = 25;
		}

		public override bool AddToWorld()
		{
			this.Name = "Archilus";
			this.GuildName = "";
			this.Model = 817;
			this.Size = 100;
			this.Level = 58;
			this.Realm = eRealm.None;
			base.AddToWorld();
			return true;
		}
		
		/// <summary>
		/// Broadcast relevant messages.
		/// </summary>
		/// <param name="message">The message to be broadcast.</param>
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		
		public void Spawn(GamePlayer player)
		{
			GameNPC mob = new GameNPC();
			SetVariables(mob);
			//Level Range of 40-45
			int level = Util.Random(40, 45);
			mob.Level = (byte) level;
			BroadcastMessage(String.Format(m_SpawnAnnounce, this.Name, mob.Name));
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
			mob.Name = "young death shroud";
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
			
			this.Level = 60;
			this.Size = 100;
			base.Die(killer);
			foreach (GameNPC npc in this.GetNPCsInRadius(5000))
			{
				if (npc.Name.Contains("young death shroud"))
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
				if (this.HealthPercent < 90)
				{
					new RegionTimer(this, new RegionTimerCallback(timer => CastShroud(timer, player)), 1000);
					
				}
			}

			base.TakeDamage(source, damageType, damageAmount, criticalAmount);
		}

		private int CastShroud(RegionTimer timer, GamePlayer player)
		{
			Spawn(player);
			return 0;
		}
		
		public void SendReply(GamePlayer player, string msg)
		{
			player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}
		
	}

}