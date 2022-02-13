/*
Gudlaugr.
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
using DOL.GS.Scripts.DOL.AI.Brain;


namespace DOL.GS.Scripts
{

	public class Gudlaugr : GameNPC
	{
		/// <summary>
		/// Add Gudlaugr to World
		/// </summary>
		public override bool AddToWorld()
		{
			Realm = eRealm.None;
			Model = 650;
			Size = 40;
			Level = 64;
			Strength = 255;
			Dexterity = 120;
			Constitution = 1200;
			Intelligence = 220;
			Health = MaxHealth;
			Piety = 130;
			Empathy = 130;
			Charisma = 130;
			MaxDistance = 4000;
			TetherRange = 3500;
			Faction = FactionMgr.GetFactionByID(779);
			Name = "Gudlaugr";
			BodyType = 1;

			ScalingFactor = 40;
			base.SetOwnBrain(new GudlaugrBrain());

			// set aggrolevel + range
			GudlaugrBrain brain = new GudlaugrBrain();
			brain.AggroLevel = 200;
			brain.AggroRange = 550;
			base.AddToWorld();
			
			return true;
		}
		
		public override int MaxHealth
		{
			get { return 1500 * Constitution / 100; }
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Gudlaugr NPC Initializing...");
		}
	}

	namespace DOL.AI.Brain
	{
		public class GudlaugrBrain : StandardMobBrain
		{
			public GudlaugrBrain() : base() { }

			public static bool transmorph = true;
			public override void Think()
			{
				if (Body.InCombat && Body.IsAlive && HasAggro)
				{
					if (Body.TargetObject != null)
					{
						// Someone hit Gudlaugr. The Wolf starts to change model and Size.
						RageMode(true);
						
					}
				}
				else if(!Body.InCombat && Body.IsAlive && !HasAggro)
				{
					// will be little wolf again
					RageMode(false);
				}
			}
			
			/// <summary>
			/// Called whenever the gudlaugr's body sends something to its brain.
			/// </summary>
			/// <param name="e">The event that occured.</param>
			/// <param name="sender">The source of the event.</param>
			/// <param name="args">The event details.</param>
			public override void Notify(DOLEvent e, object sender, EventArgs args)
			{
				base.Notify(e, sender, args);
				if (sender == Body)
				{
					Gudlaugr gud = sender as Gudlaugr;
					if (e == GameLivingEvent.AttackFinished)
					{
						base.Notify(GameLivingEvent.AttackFinished, Body, args);
						
						new RegionTimer(Body, new RegionTimerCallback(CastSnare), 100);
						if (Bleed.TargetHasEffect(Body.TargetObject) == false && Body.TargetObject.IsVisibleTo(Body))
						{
							new RegionTimer(Body, new RegionTimerCallback(StartBleed), 150);
						}
						
					}

				}
			}
			/// <summary>
			/// Check if the wolf starts raging or not
			/// </summary>
			/// <param name="rage"></param>
			public void RageMode(bool rage)
			{

				if (!rage)
				{
					// transmorph to little white wolf
					Body.ScalingFactor = 40;
					Body.Model = 650;
					Body.Size = 40;
				}
				else
				{
					// transmorph to demon wolf
					Body.ScalingFactor = 60;
					Body.Strength = 400;
					Body.Constitution = 1500;
					Body.Model = 649;
					Body.Size = 110;
					
					if (transmorph)
					{
						Body.Health = Body.MaxHealth;
						transmorph = false;
					}
				}
			}

			/// <summary>
			/// Cast Snare on the Target
			/// </summary>
			/// <param name="timer">The timer that started this cast.</param>
			/// <returns></returns>
			private int CastSnare(RegionTimer timer)
			{
				Body.CastSpell(Snare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				return 0;
			}
			
			/// <summary>
			/// Starts Bleed on the Target
			/// </summary>
			/// <param name="timer">The timer that started this cast.</param>
			/// <returns></returns>
			private int StartBleed(RegionTimer timer)
			{
				Body.CastSpell(Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				return 0;
			}
			
			#region Snare
			private Spell m_Snare;
			/// <summary>
			/// The Snare spell.
			/// </summary>
			protected Spell Snare
			{
				get
				{
					if (m_Snare == null)
					{
						DBSpell spell = new DBSpell();
						spell.AllowAdd = false;
						spell.CastTime = 0;
						spell.Uninterruptible = true;
						spell.ClientEffect = 2135;
						spell.Icon = 0;
						spell.Description = "Reduces the target's movement speed by 60% for 12 seconds.";
						spell.Name = "Bite wound";
						spell.Range = 300;
						spell.Radius = 0;
						spell.Value = 60;
						spell.Duration = 12;
						spell.Damage = 150;
						spell.DamageType = 10;
						spell.SpellID = 20300;
						spell.Target = "Enemy";
						spell.MoveCast = true;
						spell.Type = eSpellType.DamageSpeedDecrease.ToString();
						spell.Message1 = "You begin to move more slowly!";
						spell.Message2 = "{0} begins moving more slowly!";
						m_Snare = new Spell(spell, 60);
						SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Snare);
					}
					return m_Snare;
				}
			}

			#endregion
			
			#region StyleBleed
			private Spell m_Bleed;
			/// <summary>
			/// The Snare spell.
			/// </summary>
			protected Spell Bleed
			{
				get
				{
					if (m_Bleed == null)
					{
						DBSpell spell = new DBSpell();
						spell.AllowAdd = false;
						spell.CastTime = 0;
						spell.Uninterruptible = true;
						spell.ClientEffect = 2130;
						spell.Icon = 3472;
						spell.Description = "Does 100 damage to a target every 3 seconds for 40 seconds. ";
						spell.Name = "Bite wound";
						spell.Range = 500;
						spell.Radius = 0;
						spell.Value = 0;
						spell.Duration = 40;
						spell.Frequency = 40;
						spell.Pulse = 1;
						spell.Damage = 100;
						spell.DamageType = 10;
						spell.SpellID = 20209;
						spell.Target = "Enemy";
						spell.MoveCast = true;
						spell.Type = eSpellType.StyleBleeding.ToString();
						spell.Message1 = "You are bleeding! ";
						spell.Message2 = "{0} is bleeding! ";
						m_Bleed = new Spell(spell, 60);
						SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bleed);
					}
					return m_Bleed;
				}
			}

			#endregion
		}
	}
}