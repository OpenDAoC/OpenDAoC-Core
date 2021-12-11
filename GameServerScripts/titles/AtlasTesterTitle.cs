/*
 *
 * Atlas - Test phase titles
 *
 */

using System;

using DOL.Events;

namespace GameServerScripts.Titles
{
    /// <summary>
    /// "I'm bugged" title granted to all account that have played the beta
    /// </summary>
    public class AtlasTestTitle : TranslatedNoGenderGenericEventPlayerTitle
    {
        DateTime alphaEnd = new DateTime(2021, 12, 17);
        public override DOLEvent Event { get { return GamePlayerEvent.GameEntered; }}
        
        protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Atlas.Bugged", "Titles.Atlas.Bugged"); }}
        protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => player.Client.Account.CreationDate < alphaEnd; }} // Title awarded to players that played the alpha (end 17 dec 2021)
        
    }

    /// <summary>
    /// "I'm bugged" title granted to all account that have played the beta
    /// </summary>
    public class AtlasTestTitleLD : TranslatedNoGenderGenericEventPlayerTitle
    {
        DateTime alphaEnd = new(2021, 12, 17);
        public override DOLEvent Event { get { return GamePlayerEvent.GameEntered; }}
        protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Atlas.LD", "Titles.Atlas.LD"); }}
        protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => player.Client.Account.CreationDate < alphaEnd; }} // Title awarded to players that played the alpha (end 17 dec 2021)
    }
	
}

// protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => DateTime.Now.Subtract(player.CreationDate).TotalDays >= 178; }} // ~half year