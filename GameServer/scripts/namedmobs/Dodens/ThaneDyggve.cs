/*
Thane Dyggve.
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

	public class ThaneDyggve : GameEpicNPC
	{

		public ThaneDyggve() : base()
		{
			
		}
		/// <summary>
		/// Add Thane Dyggve to World
		/// </summary>
		public override bool AddToWorld()
		{
			LoadEquipmentTemplateFromDatabase("Thane_Dyggve");
			Realm = eRealm.None;
			Model = 202;
			Size = 50;
			Level = 66;
			ParryChance = 15;
			Health = MaxHealth;
			MaxDistance = 2500;
			TetherRange = 3500;
			MeleeDamageType = eDamageType.Crush;
			Faction = FactionMgr.GetFactionByID(779);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			
			Name = "Thane Dyggve";

			ScalingFactor = 60;
			base.SetOwnBrain(new ThaneDyggveBrain());
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
			if (this.IsAlive && keyName == GS.Abilities.CCImmunity)
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
		/// Return to spawn point, Thane Dyggve can't be attacked while it's
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
			// When Thane Dyggve arrives at its spawn point, make it vulnerable again.

			if (e == GameNPCEvent.ArriveAtTarget)
				EvadeChance = 0;
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Thane Dyggve NPC Initializing...");
		}
	}

	namespace DOL.AI.Brain
	{
		public class ThaneDyggveBrain : StandardMobBrain
		{
			protected String[] m_MjollnirAnnounce;
			protected bool castsMjollnir = true;
			public ThaneDyggveBrain() : base()
			{
				m_MjollnirAnnounce = new String[]
				{
					"You feel your energy draining and {0} summons powerful lightning hammers!",
					"{0} takes another energy drain as he prepares to unleash a raging Mjollnir upon you!"
				};
			}

			public override void Think()
			{
				if (Body.InCombat && Body.IsAlive && HasAggro)
				{
					if (Body.TargetObject != null)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastMjollnir), 2000);
						if (Body.IsCasting)
						{
							if (castsMjollnir)
							{
								int messageNo = Util.Random(1, m_MjollnirAnnounce.Length) - 1;
								BroadcastMessage(String.Format(m_MjollnirAnnounce[messageNo], Body.Name));
							}
							castsMjollnir = false;
						}
						else
						{
							castsMjollnir = true;
						}
					}
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
			/// Called whenever the Thane Dyggve's body sends something to its brain.
			/// </summary>
			/// <param name="e">The event that occured.</param>
			/// <param name="sender">The source of the event.</param>
			/// <param name="args">The event details.</param>
			public override void Notify(DOLEvent e, object sender, EventArgs args)
			{
				base.Notify(e, sender, args);
			}

			/// <summary>
			/// Cast Mjollnir on the Target
			/// </summary>
			/// <param name="timer">The timer that started this cast.</param>
			/// <returns></returns>
			private int CastMjollnir(RegionTimer timer)
			{
				Body.CastSpell(Mjollnir, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				return 0;
			}
			
			#region MjollnirSpell
			private Spell m_Mjollnir;
			/// <summary>
			/// The Mjollnir spell.
			/// </summary>
			protected Spell Mjollnir
			{
				get
				{
					if (m_Mjollnir == null)
					{
						DBSpell spell = new DBSpell();
						spell.AllowAdd = false;
						spell.CastTime = 4;
						spell.Uninterruptible = true;
						spell.RecastDelay = 30;
						spell.ClientEffect = 3541;
						spell.Icon = 3541;
						spell.Description = "Damages the target for 800.";
						spell.Name = "Command Mjollnir";
						spell.Range = 1500;
						spell.Radius = 350;
						spell.Value = 0;
						spell.Duration = 0;
						spell.Damage = 1000;
						spell.DamageType = 12;
						spell.SpellID = 3541;
						spell.Target = "Enemy";
						spell.MoveCast = false;
						spell.Type = eSpellType.DirectDamage.ToString();
						m_Mjollnir = new Spell(spell, 50);
						SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mjollnir);
					}
					return m_Mjollnir;
				}
			}

			#endregion
			
		}
	}
}