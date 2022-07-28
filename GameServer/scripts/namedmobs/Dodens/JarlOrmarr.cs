/*
Jarl Ormarr
<author>Kelt</author>
 */
using DOL.Database;
using DOL.Events;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts.DOL.AI.Brain;
using System;

namespace DOL.GS.Scripts
{
	public class JarlOrmarr : GameEpicBoss
	{
		public JarlOrmarr() : base()
		{		
		}
		/// <summary>
		/// Add Jarl Ormarr to World
		/// </summary>
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9918);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
	
			// humanoid
			BodyType = 6;
			MeleeDamageType = eDamageType.Slash;
			Faction = FactionMgr.GetFactionByID(779);
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			// right hand
			VisibleActiveWeaponSlots = (byte) eActiveWeaponSlot.Standard;			
			ScalingFactor = 40;
			base.SetOwnBrain(new JarlOrmarrBrain());
			LoadedFromScript = false; //load from database
			SaveIntoDatabase();
			base.AddToWorld();		
			return true;
		}
		
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get
			{
				return 350;
			}
			set
			{ }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
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
		/// <summary>
		/// Return to spawn point, Jarl Ormarr can't be attacked while it's
		/// on it's way.
		/// </summary>
		public override void WalkToSpawn()
		{
			EvadeChance = 100;
			WalkToSpawn(MaxSpeed);
		}
		public override void OnAttackedByEnemy(AttackData ad)
		{
			if (EvadeChance == 100)
				return;

			base.OnAttackedByEnemy(ad);
		}	
		/// <summary>
		/// Handle event notifications.
		/// </summary>
		/// <param name="e">The event that occured.</param>
		/// <param name="sender">The sender of the event.</param>
		public override void Notify(DOLEvent e, object sender)
		{
			base.Notify(e, sender);
			
			// When Jarl Ormarr arrives at its spawn point, make it vulnerable again.
			if (e == GameNPCEvent.ArriveAtTarget)
				EvadeChance = 0;
		}	
		public override void Die(GameObject killer)
		{
			// debug
			log.Debug($"{Name} killed by {killer.Name}");

			GamePlayer playerKiller = killer as GamePlayer;

			if (playerKiller?.Group != null)
			{
				foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
				{
					AtlasROGManager.GenerateOrbAmount(groupPlayer,OrbsReward);
				}
			}
			base.Die(killer);
		}
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Jarl Ormarr NPC Initializing...");
		}
	}
	namespace DOL.AI.Brain
	{
		public class JarlOrmarrBrain : StandardMobBrain
		{
			protected String[] m_HitAnnounce;
			public JarlOrmarrBrain() : base()
			{
				m_HitAnnounce = new String[]
				{
					"Haha! You call that a hit? I\'ll show you a hit!",
					"I am a warrior, you can\'t kill me!"
				};
				
				AggroLevel = 50;
				AggroRange = 400;
			}
			public override void Think()
			{
				base.Think();
			}
			/// <summary>
			/// Broadcast relevant messages to the raid.
			/// </summary>
			/// <param name="message">The message to be broadcast.</param>
			public void BroadcastMessage(String message)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
				{
					player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
				}
			}		
			/// <summary>
			/// Called whenever the Jarl Ormarr body sends something to its brain.
			/// </summary>
			/// <param name="e">The event that occured.</param>
			/// <param name="sender">The source of the event.</param>
			/// <param name="args">The event details.</param>
			public override void Notify(DOLEvent e, object sender, EventArgs args)
			{
				base.Notify(e, sender, args);
				if (e == GameObjectEvent.TakeDamage)
				{
					if (Util.Chance(3))
					{
						int messageNo = Util.Random(1, m_HitAnnounce.Length) - 1;
						BroadcastMessage(String.Format(m_HitAnnounce[messageNo]));
					}
				}
			}
		}
	}
}