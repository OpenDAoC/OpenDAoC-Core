using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

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
	public override void Notify(CoreEvent e, object sender, EventArgs args)
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
				DbSpell spell = new DbSpell();
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
				spell.Type = ESpellType.StyleSpeedDecrease.ToString();
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
				DbSpell spell = new DbSpell();
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
				spell.Type = ESpellType.StyleBleeding.ToString();
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