using System;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Players;

/// <summary>
/// GenericEventPlayerTitle Allow to Implement easily custom event based player title
/// </summary>
public abstract class GenericEventPlayerTitle : EventPlayerTitle
{
	/// <summary>
	/// Tuple of String Description / Name / Female Description / Female Name 
	/// </summary>
	protected abstract Tuple<string, string, string, string> GenericNames { get; }
	
	/// <summary>
	/// Suitable Lamba Method
	/// </summary>
	protected abstract Func<GamePlayer, bool> SuitableMethod { get; }
	
	/// <summary>
	/// Should this Title go through Translator
	/// </summary>
	protected abstract bool Translate { get; }
	
	/// <summary>
	/// Get Description for this Title
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public override string GetDescription(GamePlayer player)
	{
		string description = GenericNames.Item1;
		
		if (player.Gender == EGender.Female && !string.IsNullOrEmpty(GenericNames.Item3))
			description = GenericNames.Item3;
		
		return Translate ? TryTranslate(description, player) : description;
	}
	
	/// <summary>
	/// Get Value for this Title
	/// </summary>
	/// <param name="source">The player looking.</param>
	/// <param name="player"></param>
	/// <returns></returns>
	public override string GetValue(GamePlayer source, GamePlayer player)
	{
		string titleValue = GenericNames.Item2;
		
		if (player.Gender == EGender.Female && !string.IsNullOrEmpty(GenericNames.Item4))
			titleValue = GenericNames.Item4;
					
		return Translate ? TryTranslate(titleValue, source) : titleValue;
	}
	
	protected static string TryTranslate(string value, GamePlayer source)
	{
		return LanguageMgr.TryTranslateOrDefault(source, string.Format("!{0}!", value), value);
	}
	
	/// <summary>
	/// Return True if this Title Suit the Targeted Player
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public override bool IsSuitable(GamePlayer player)
	{
		if (SuitableMethod != null)
			return SuitableMethod(player);
		
		return false;
	}
}

public abstract class NoGenderGenericEventPlayerTitle : GenericEventPlayerTitle
{
	/// <summary>
	/// Tuple of String Description / Name / Female Description / Female Name 
	/// </summary>
	protected override Tuple<string, string, string, string> GenericNames
	{
		get
		{
			return new Tuple<string, string, string, string>(DescriptionValue.Item1, DescriptionValue.Item2, null, null);
		}
	}
	
	/// <summary>
	/// Tuple of String Descrition / Name Value
	/// </summary>
	protected abstract Tuple<string, string> DescriptionValue { get; }
	
	/// <summary>
	/// Should this Title go through Translator
	/// </summary>
	protected override bool Translate { get { return false; }}
}

public abstract class TranslatedGenericEventPlayerTitle : GenericEventPlayerTitle
{
	/// <summary>
	/// Should this Title go through Translator
	/// </summary>
	protected override bool Translate { get { return true; }}
}

public abstract class TranslatedNoGenderGenericEventPlayerTitle : NoGenderGenericEventPlayerTitle
{
	/// <summary>
	/// Should this Title go through Translator
	/// </summary>
	protected override bool Translate { get { return true; }}
}