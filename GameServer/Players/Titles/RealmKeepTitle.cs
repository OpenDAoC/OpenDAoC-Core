using System;
using Core.GS.Events;

namespace Core.GS.Players;

#region keep
/// <summary>
/// "Frontier Challenger" title granted to everyone who captured 10+ keeps.
/// </summary>
public class FrontierChallengerTitle : TranslatedNoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.CapturedKeepsChanged; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Claim.Frontier.FrontierChallenger", "Titles.Claim.Frontier.FrontierChallenger"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => player.CapturedKeeps >= 10 && player.CapturedKeeps < 50; }}
}
/// <summary>
/// "Frontier Vindicator" title granted to everyone who captured 50+ keeps.
/// </summary>
public class FrontierVindicatorTitle : TranslatedNoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.CapturedKeepsChanged; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Claim.Frontier.FrontierVindicator", "Titles.Claim.Frontier.FrontierVindicator"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => player.CapturedKeeps >= 50 && player.CapturedKeeps < 500; }}
}
/// <summary>
/// "Frontier Challenger" title granted to everyone who captured 10+ keeps.
/// </summary>
public class FrontierProtectorTitle : NoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.CapturedKeepsChanged; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Frontier Protector", "Frontier Protector"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => player.CapturedKeeps >= 500; }}
}
#endregion
#region tower
/// <summary>
/// "Stronghold Soldier" title granted to everyone who captured 100+ towers.
/// </summary>
public class StrongholdSoldierTitle : TranslatedNoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.CapturedTowersChanged; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Claim.Stronghold.StrongholdSoldier", "Titles.Claim.Stronghold.StrongholdSoldier"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => player.CapturedTowers >= 100 && player.CapturedTowers < 1000; }}
}
/// <summary>
/// "Stronghold Chief" title granted to everyone who captured 1000+ towers.
/// </summary>
public class StrongholdChiefTitle : TranslatedNoGenderGenericEventPlayerTitle
{
	public override CoreEvent Event { get { return GamePlayerEvent.CapturedTowersChanged; }}
	protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Claim.Stronghold.StrongholdChief", "Titles.Claim.Stronghold.StrongholdChief"); }}
	protected override Func<Core.GS.GamePlayer, bool> SuitableMethod { get { return player => player.CapturedTowers >= 1000; }}
}
#endregion