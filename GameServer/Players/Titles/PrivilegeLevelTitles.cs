using System;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Players.Titles;

/// <summary>
/// Administrator
/// </summary>
public class AdministratorTitle : TranslatedNoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.GameEntered; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.PrivLevel.Administrator", "Titles.PrivLevel.Administrator"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => player.Client.Account.PrivLevel == (uint)EPrivLevel.Admin; }}
}
/// <summary>
/// Game Master
/// </summary>
public class GamemasterTitle : TranslatedNoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.GameEntered; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.PrivLevel.Gamemaster", "Titles.PrivLevel.Gamemaster"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => player.Client.Account.PrivLevel == (uint)EPrivLevel.GM; }}
}

public class Friend : NoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.GameEntered; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("My Uncle Works At Nintendo", "My Uncle Works At Nintendo"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => player.GetAchievementProgress("NintendoDad") > 0; }}
}