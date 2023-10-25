﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.GS.Effects;

namespace Core.GS.Spells;

/// <summary>
/// Spell Helper Class that handle most static method to help finding spell handlers/effects.
/// </summary>
public static class SpellHelper
{
	
	#region FindEffect by Spell Object
	/// <summary>
	/// Find Game Spell Effect by spell object
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spell">Spell Object to Find (Spell.ID Match)</param>
	/// <returns>First occurence GameSpellEffect build from spell in target's effect list or null</returns>
	public static GameSpellEffect FindEffectOnTarget(this GameLiving target, Spell spell)
	{
		GameSpellEffect effect = null;
		lock (target.EffectList)
		{
			effect = target.EffectsOnTarget(spell).FirstOrDefault();
		}
		
		return effect;
	}
	
	/// <summary>
	/// Find Game Spell Effects by spell object
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spell">Spell Object to Find (Spell.ID Match)</param>
	/// <returns>All GameSpellEffect build from spell in target's effect list or null</returns>
	public static List<GameSpellEffect> FindEffectsOnTarget(this GameLiving target, Spell spell)
	{
		List<GameSpellEffect> effects = null;
		lock (target.EffectList)
		{
			effects = target.EffectsOnTarget(spell).ToList();
		}
		
		return effects;
	}

	
	/// <summary>
	/// Find Game Spell Effects by spell object
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spell">Spell Object to Find (Spell.ID Match)</param>
	/// <returns>All GameSpellEffect build from spell in target's effect list or null</returns>
	private static IEnumerable<GameSpellEffect> EffectsOnTarget(this GameLiving target, Spell spell)
	{
		return target.EffectList.OfType<GameSpellEffect>().Where(fx => !(fx is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)fx).ImmunityState)
		                                                         && fx.Spell.ID == spell.ID);
	}
	#endregion
	
	#region Find Effect by Spell Type + Spell Name
	/// <summary>
	/// Find effect by spell type / spell name
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellType">Spell type to find</param>
	/// <param name="spellName">Spell name to find</param>
	/// <returns>First occurence GameSpellEffect matching Type and Name in target's effect list or null</returns>		
	public static GameSpellEffect FindEffectOnTarget(this GameLiving target, string spellType, string spellName)
	{
		GameSpellEffect effect = null;
		lock (target.EffectList)
		{
			effect = target.EffectsOnTarget(spellType, spellName).FirstOrDefault();
		}
		return effect;
	}
	
	/// <summary>
	/// Find effects by spell type / spell name
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellType">Spell type to find</param>
	/// <param name="spellName">Spell name to find</param>
	/// <returns>All GameSpellEffect matching Type and Name in target's effect list or null</returns>		
	public static List<GameSpellEffect> FindEffectsOnTarget(this GameLiving target, string spellType, string spellName)
	{
		List<GameSpellEffect> effects = null;
		lock (target.EffectList)
		{
			effects = target.EffectsOnTarget(spellType, spellName).ToList();
		}
		return effects;
	}

	/// <summary>
	/// Find Game Spell Effects by spell Type and Spell Name
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellType">Spell Type to Find</param>
	/// <param name="spellName">Spell Name to Find</param>
	/// <returns>All GameSpellEffect according to Type and Name in target's effect list or null</returns>
	private static IEnumerable<GameSpellEffect> EffectsOnTarget(this GameLiving target, string spellType, string spellName)
	{
		return target.EffectList.OfType<GameSpellEffect>().Where(fx => !(fx is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)fx).ImmunityState)
		                                                         && fx.Spell != null && fx.Spell.SpellType.Equals(spellType) && fx.Spell.Name.Equals(spellName));
	}
	#endregion
	
	#region Find Effect by Spell Type
	
	/// <summary>
	/// Find effect by spell type
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellType">Spell type to find</param>
	/// <returns>First occurence GameSpellEffect matching Type in target's effect list or null</returns>
	public static GameSpellEffect FindEffectOnTarget(this GameLiving target, string spellType)
	{
		GameSpellEffect effect = null;
		lock (target.EffectList)
		{
			effect = target.EffectsOnTarget(spellType).FirstOrDefault();
		}
		return effect;
	}
	
	/// <summary>
	/// Find effects by spell type
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellType">Spell type to find</param>
	/// <returns>All GameSpellEffect matching Type in target's effect list or null</returns>
	public static List<GameSpellEffect> FindEffectsOnTarget(this GameLiving target, string spellType)
	{
		List<GameSpellEffect> effects = null;
		lock (target.EffectList)
		{
			effects = target.EffectsOnTarget(spellType).ToList();
		}
		return effects;
	}
	
	/// <summary>
	/// Find Game Spell Effects by spell Type
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellType">Spell Type to Find</param>
	/// <returns>All GameSpellEffect according to Type and Name in target's effect list or null</returns>
	private static IEnumerable<GameSpellEffect> EffectsOnTarget(this GameLiving target, string spellType)
	{
		return target.EffectList.OfType<GameSpellEffect>().Where(fx => !(fx is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)fx).ImmunityState)
		                                                         && fx.Spell != null && fx.Spell.SpellType.Equals(spellType));
	}

	
	#endregion
	
	#region Find Effect by SpellHandler Object Match
	/// <summary>
	/// Find effect by spell handler
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Spell Handler to find (Exact Object Match)</param>
	/// <returns>First occurence of GameSpellEffect in target's effect list or null</returns>
	public static GameSpellEffect FindEffectOnTarget(this GameLiving target, ISpellHandler spellHandler)
	{
		GameSpellEffect effect = null;
		lock (target.EffectList)
		{
			effect = target.EffectsOnTarget(spellHandler).FirstOrDefault();
		}
		return effect;
	}

	/// <summary>
	/// Find effects by spell handler
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Spell Handler to find (Exact Object Match)</param>
	/// <returns>All GameSpellEffect matching spellhandler in target's effect list or null</returns>
	public static List<GameSpellEffect> FindEffectsOnTarget(this GameLiving target, ISpellHandler spellHandler)
	{
		List<GameSpellEffect> effects = null;
		lock (target.EffectList)
		{
			effects = target.EffectsOnTarget(spellHandler).ToList();
		}
		return effects;
	}

	/// <summary>
	/// Find effects by spell handler
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Spell Handler to find (Exact Object Match)</param>
	/// <returns>All GameSpellEffect matching SpellHandler in target's effect list</returns>
	private static IEnumerable<GameSpellEffect> EffectsOnTarget(this GameLiving target, ISpellHandler spellHandler)
	{
		return target.EffectList.OfType<GameSpellEffect>().Where(fx => !(fx is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)fx).ImmunityState)
		                                                         && fx.SpellHandler == spellHandler);
	}
	#endregion
	
	#region Find Effect By SpellHandler Type Hierarchy
	/// <summary>
	/// Find effect by spell handler Object Type (Hierarchical)
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Spell Handler to find (Hierarchical Type Match)</param>
	/// <returns>First occurence of GameSpellEffect in target's effect list or null</returns>
	public static GameSpellEffect FindEffectOnTarget(this GameLiving target, Type spellHandler)
	{
		GameSpellEffect effect = null;
		lock (target.EffectList)
		{
			effect = target.EffectsOnTarget(spellHandler).FirstOrDefault();
		}
		return effect;
	}
	
	/// <summary>
	/// Find effects by spell handler Object Type (Hierarchical)
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Spell Handler to find (Hierarchical Type Match)</param>
	/// <returns>All GameSpellEffect mathing Type Hierarchy in target's effect list</returns>
	public static List<GameSpellEffect> FindEffectsOnTarget(this GameLiving target, Type spellHandler)
	{
		List<GameSpellEffect> effects = null;
		lock (target.EffectList)
		{
			effects = target.EffectsOnTarget(spellHandler).ToList();
		}
		return effects;
	}
	
	/// <summary>
	/// Find effects by spell handler Type hierarchically
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Spell Handler to find (Exact Object Match)</param>
	/// <returns>All GameSpellEffect matching SpellHandler in target's effect list</returns>
	private static IEnumerable<GameSpellEffect> EffectsOnTarget(this GameLiving target, Type spellHandler)
	{
		return target.EffectList.OfType<GameSpellEffect>().Where(fx => !(fx is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)fx).ImmunityState)
		                                                         && spellHandler.IsInstanceOfType(fx.SpellHandler));
	}
	#endregion
	
	#region Find Pulsing Spell Effect by Spell Object		
	/// <summary>
	/// Find Pulsing Spell Effect by spell object
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spell">Spell Object to Find (Spell.ID Match)</param>
	/// <returns>First occurence PulsingSpellEffect build from spell in target's concentration list or null</returns>
	public static PulsingSpellEffect FindPulsingSpellOnTarget(this GameLiving target, Spell spell)
	{
		PulsingSpellEffect pulsingSpell = null;
		lock (target.effectListComponent.ConcentrationEffectsLock)
		{
			pulsingSpell = target.PulsingSpellsOnTarget(spell).FirstOrDefault();
		}
		
		return pulsingSpell;
	}
	/// <summary>
	/// Find Pulsing Spell Effects by spell object
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spell">Spell Object to Find (Spell.ID Match)</param>
	/// <returns>All PulsingSpellEffect build from spell in target's concentration list or null</returns>
	public static List<PulsingSpellEffect> FindPulsingSpellsOnTarget(this GameLiving target, Spell spell)
	{
		List<PulsingSpellEffect> effects = null;
		lock (target.EffectList)
		{
			effects = target.PulsingSpellsOnTarget(spell).ToList();
		}
		
		return effects;
	}
	
	/// <summary>
	/// Find Pulsing Spell Effects by spell object
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spell">Spell Object to Find (Spell.ID Match)</param>
	/// <returns>All PulsingSpellEffect build from spell in target's concentration list or null</returns>
	private static IEnumerable<PulsingSpellEffect> PulsingSpellsOnTarget(this GameLiving target, Spell spell)
	{
		return target.effectListComponent.ConcentrationEffects.OfType<PulsingSpellEffect>().Where(pfx => pfx.SpellHandler != null && pfx.SpellHandler.Spell != null
		                                                                      && pfx.SpellHandler.Spell.ID == spell.ID);
	}
	
	#endregion
	
	#region Find Pulsing Spell Effects by SpellHandler Object Match
	/// <summary>
	/// Find pulsing spell effect by spell handler
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="handler">Spell Handler to find (Exact Object Match)</param>
	/// <returns>First occurence of PulsingSpellEffect in targets' concentration list or null</returns>
	public static PulsingSpellEffect FindPulsingSpellOnTarget(this GameLiving target, ISpellHandler handler)
	{
		PulsingSpellEffect effect = null;
		lock (target.effectListComponent.ConcentrationEffectsLock)
		{
			effect = target.PulsingSpellsOnTarget(handler).FirstOrDefault();
		}
		return effect;
	}
	
	/// <summary>
	/// Find pulsing spell effect by spell handler
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="handler">Spell Handler to find (Exact Object Match)</param>
	/// <returns>First occurence of PulsingSpellEffect in targets' concentration list or null</returns>
	public static List<PulsingSpellEffect> FindPulsingSpellsOnTarget(this GameLiving target, ISpellHandler handler)
	{
		List<PulsingSpellEffect> effects = null;
		lock (target.effectListComponent.ConcentrationEffectsLock)
		{
			effects = target.PulsingSpellsOnTarget(handler).ToList();
		}
		return effects;
	}
	
	/// <summary>
	/// Find pulsing spells effect by spell handler
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="handler">Spell Handler to find (Exact Object Match)</param>
	/// <returns>All PulsingSpellEffect Matching SpellHandler in targets' concentration list</returns>
	private static IEnumerable<PulsingSpellEffect> PulsingSpellsOnTarget(this GameLiving target, ISpellHandler handler)
	{
		return target.effectListComponent.ConcentrationEffects.OfType<PulsingSpellEffect>().Where(pfx => pfx.SpellHandler == handler);
	}
	#endregion
	
	#region Find Static Effect by Effect Type
	/// <summary>
	/// Find Static Effect by Effect Type
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="effectType">Effect Type to find (Exact Type Match)</param>
	/// <returns>First occurence of IGameEffect in target's effect list or null</returns>
	public static IGameEffect FindStaticEffectOnTarget(this GameLiving target, Type effectType)
	{
		IGameEffect effect = null;
		lock (target.EffectList)
		{
			effect = target.StaticEffectsOnTarget(effectType).FirstOrDefault();
		}
		return effect;
	}
	
	/// <summary>
	/// Find Static Effects by Effect Type
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="effectType">Effect Type to find (Exact Type Match)</param>
	/// <returns>All IGameEffect matching Effect Type in target's effect list</returns>
	public static List<IGameEffect> FindStaticEffectsOnTarget(this GameLiving target, Type effectType)
	{
		List<IGameEffect> effects = null;
		lock (target.EffectList)
		{
			effects = target.StaticEffectsOnTarget(effectType).ToList();
		}
		return effects;
	}
	
	/// <summary>
	/// Find Static Effects by Effect Type
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="effectType">Effect Type to find (Exact Type Match)</param>
	/// <returns>All IGameEffect matching Effect Type in target's effect list</returns>
	private static IEnumerable<IGameEffect> StaticEffectsOnTarget(this GameLiving target, Type effectType)
	{
		return target.EffectList.Where(fx => fx.GetType() == effectType);
	}
	#endregion
	
	#region Find Immunity Effect by SpellHandler Type
	/// <summary>
	/// Find Immunity Effect by spellHandler Type
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Effect Type to find (Exact Type Match)</param>
	/// <returns>First occurence of Immunity State GameSpellEffect in target's effect list or null</returns>
	public static GameSpellEffect FindImmunityEffectOnTarget(this GameLiving target, Type spellHandler)
	{
		GameSpellEffect effect = null;
		lock (target.EffectList)
		{
			effect = target.ImmunityEffectsOnTarget(spellHandler).FirstOrDefault();
		}
		return effect;
	}
	
	/// <summary>
	/// Find Immunity Effects by spellHandler Type
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Effect Type to find (Exact Type Match)</param>
	/// <returns>All Immunity State GameSpellEffect matching SpellHandler Type in target's effect list</returns>
	public static List<GameSpellEffect> FindImmunityEffectsOnTarget(this GameLiving target, Type spellHandler)
	{
		List<GameSpellEffect> effects = null;
		lock (target.EffectList)
		{
			effects = target.ImmunityEffectsOnTarget(spellHandler).ToList();
		}
		return effects;
	}

	/// <summary>
	/// Find Immunity Effects by spellHandler Type
	/// Inner Method to get Enumerable For LINQ.
	/// </summary>
	/// <param name="target">Living to find effect on</param>
	/// <param name="spellHandler">Effect Type to find (Exact Type Match)</param>
	/// <returns>All Immunity State GameSpellEffect matching SpellHandler Type in target's effect list</returns>
	private static IEnumerable<GameSpellEffect> ImmunityEffectsOnTarget(this GameLiving target, Type spellHandler)
	{
		return target.EffectList.OfType<GameSpellEffect>().Where(fx => (fx is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)fx).ImmunityState)
		                                                         && fx.SpellHandler != null && fx.SpellHandler.GetType() == spellHandler);
	}
	#endregion
}