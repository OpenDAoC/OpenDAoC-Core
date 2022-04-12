/*
Hamar
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
	public class Hamar : GameEpicNPC
	{
		public Hamar() : base()
		{ }
		
		/// <summary>
		/// Add Hamar to World
		/// </summary>
		public override bool AddToWorld()
		{
			Realm = eRealm.None;
			Model = 461;
			Size = 60;
			Level = 60;
			ParryChance = 15;
			EvadeChance = 15;
			Strength = 120;
			
			Health = MaxHealth;
			MaxDistance = 2500;
			TetherRange = 3500;
			
			// 1h Axe, left + main
			GameNpcInventoryTemplate hamarTemp = new GameNpcInventoryTemplate();
			hamarTemp.AddNPCEquipment(eInventorySlot.RightHandWeapon, 319);
			hamarTemp.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 319);
			Inventory = hamarTemp.CloseTemplate();
			
			// undead
			BodyType = 11;
			MeleeDamageType = eDamageType.Slash;
			Faction = FactionMgr.GetFactionByID(779);
			Name = "Hamar";
			
			Flags |= eFlags.GHOST;
			// double-wielded
			VisibleActiveWeaponSlots = 16;
			
			ScalingFactor = 40;
			base.SetOwnBrain(new HamarBrain());
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
			return 0.45;
		}
		
		/// <summary>
		/// Return to spawn point, Hamar can't be attacked while it's
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
			
			// When Hamar arrives at its spawn point, make it vulnerable again.
			if (e == GameNPCEvent.ArriveAtTarget)
				EvadeChance = 0;
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Hamar NPC Initializing...");
		}
	}

	namespace DOL.AI.Brain
	{
		public class HamarBrain : StandardMobBrain
		{
			private bool _startAttack = true;
			public HamarBrain() : base()
			{
				AggroLevel = 200;
				AggroRange = 550;
			}

			public override void Think()
			{
				base.Think();

				if (Body.InCombat && Body.IsAlive && HasAggro)
				{
					if (Body.TargetObject != null)
					{
						if (_startAttack)
						{
							foreach (GameNPC vendos in Body.GetNPCsInRadius(1000))
							{
								if (vendos == null) 
									return;
							
								foreach (GamePlayer player in Body.GetPlayersInRadius(1000))
								{
									if (player == null)
										return;

									if (vendos.Name.ToLower().Contains("snow vendo") && vendos.IsVisibleTo(Body))
									{
										vendos.StartAttack(player);
										_startAttack = false;
									}
								}
							}
						}
					}
				}
				else if(!Body.InCombat && Body.IsAlive && !HasAggro)
				{
					_startAttack = true;
				}
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
			/// Called whenever the Hamar body sends something to its brain.
			/// </summary>
			/// <param name="e">The event that occured.</param>
			/// <param name="sender">The source of the event.</param>
			/// <param name="args">The event details.</param>
			public override void Notify(DOLEvent e, object sender, EventArgs args)
			{
				base.Notify(e, sender, args);
			}
		}
	}
}