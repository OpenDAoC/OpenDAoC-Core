using System;
using Core.GS.Events;

namespace Core.GS.Players.Titles;

/// <summary>
/// "Veteran" title granted to all chars that play for at least 180 days.
/// </summary>
public class VeteranTitle : TranslatedNoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.GameEntered; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Time.Character.Veteran", "Titles.Time.Character.Veteran"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => DateTime.Now.Subtract(player.CreationDate).TotalDays >= 178; }} // ~half year
}

/// <summary>
/// "Elder" title granted to all chars on all accounts that created for at least one year.
/// </summary>
public class ElderTitle : TranslatedNoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.GameEntered; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Time.Account.ElderTitle", "Titles.Time.Account.ElderTitle"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => DateTime.Now.Subtract(player.Client.Account.CreationDate).TotalDays >= 365; }} // a year
}