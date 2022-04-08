/*
Jarl Ormarr
<author>Kelt</author>
 */
using System;
using System.Collections.Generic;
using System.Text;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using System.Reflection;
using System.Collections;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts.DOL.AI.Brain;

namespace DOL.GS.Scripts
{
	public class JarlOrmarr : GameEpicNPC
	{

		public JarlOrmarr() : base()
		{
			
		}
		/// <summary>
		/// Add Jarl Ormarr to World
		/// </summary>
		public override bool AddToWorld()
		{
			LoadEquipmentTemplateFromDatabase("Jarl_Ormarr");
			Realm = eRealm.None;
			Model = 2316;
			Size = 52;
			Level = 60;
			MaxSpeedBase = 200;
			ParryChance = 15;
			EvadeChance = 5;
			Strength = 130;
			
			Health = MaxHealth;
			MaxDistance = 2500;
			TetherRange = 3500;

			// humanoid
			BodyType = 6;
			MeleeDamageType = eDamageType.Slash;
			Faction = FactionMgr.GetFactionByID(779);
			Name = "Jarl Ormarr";
			
			// right hand
			VisibleActiveWeaponSlots = (byte) eActiveWeaponSlot.Standard;
			
			ScalingFactor = 40;
			base.SetOwnBrain(new JarlOrmarrBrain());
			base.AddToWorld();
			
			return true;
		}
		
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int MaxHealth
		{
			get
			{
				return 20000;
			}
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 800;
		}

		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.55;
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