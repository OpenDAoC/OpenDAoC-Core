/*
Gudlaugr.
<author>Kelt</author>
 */
using System;
using DOL.Database;
using DOL.Events;
using DOL.AI.Brain;
using DOL.GS.Scripts.DOL.AI.Brain;

namespace DOL.GS.Scripts
{
	public class Gudlaugr : GameEpicBoss
	{
		/// <summary>
		/// Add Gudlaugr to World
		/// </summary>
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9919);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			Faction = FactionMgr.GetFactionByID(779);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			BodyType = 1;
			ScalingFactor = 40;
			GudlaugrBrain sbrain = new GudlaugrBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			SaveIntoDatabase();
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
				case eDamageType.Slash: return 40; // dmg reduction for melee dmg
				case eDamageType.Crush: return 40; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
				default: return 70; // dmg reduction for rest resists
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
		public override void OnAttackEnemy(AttackData ad)
		{
			GudlaugrBrain brain = new GudlaugrBrain();
			if (TargetObject != null)
			{
				if (ad.Target.IsWithinRadius(this, AttackRange))
				{
					if (!ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Bleed))
					{
						CastSpell(brain.Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
					if (!ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff))
					{
						CastSpell(brain.Snare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
				}
			}
			base.OnAttackEnemy(ad);
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
				base.Think();
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
					Body.Strength = Body.NPCTemplate.Strength;
				}
				else
				{
					// transmorph to demon wolf
					Body.ScalingFactor = 60;
					Body.Strength = 330;
					Body.Model = 649;
					Body.Size = 110;
					
					if (transmorph)
					{
						Body.Health = Body.MaxHealth;
						transmorph = false;
					}
				}
			}
			
			#region Snare
			public Spell m_Snare;
			/// <summary>
			/// The Snare spell.
			/// </summary>
			public Spell Snare
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
						spell.Description = "Reduces the target's movement speed by 60% for 25 seconds.";
						spell.Name = "Bite wound";
						spell.Range = 300;
						spell.Radius = 0;
						spell.Value = 60;
						spell.Duration = 25;
						spell.DamageType = 10;
						spell.SpellID = 20300;
						spell.Target = "Enemy";
						spell.MoveCast = true;
						spell.Type = eSpellType.StyleSpeedDecrease.ToString();
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
			public Spell m_Bleed;
			/// <summary>
			/// The Snare spell.
			/// </summary>
			public Spell Bleed
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
						spell.Pulse = 0;
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