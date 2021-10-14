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
        public override DOLEvent Event { get { return GamePlayerEvent.GameEntered; }}
        protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Atlas.Bugged", "Titles.Atlas.Bugged"); }}
        protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => player.CreationDate < DateTime.Now; }} // fake check TODO change with launch date
    }
    
    /// <summary>
    /// "I'm bugged" title granted to all account that have played the beta
    /// </summary>
    public class AtlasTestTitleLD : TranslatedNoGenderGenericEventPlayerTitle
    {
        public override DOLEvent Event { get { return GamePlayerEvent.GameEntered; }}
        protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Atlas.LD", "Titles.Atlas.LD"); }}
        protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => player.CreationDate < DateTime.Now; }} // fake check TODO change with launch date
    }
	
}

// protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => DateTime.Now.Subtract(player.CreationDate).TotalDays >= 178; }} // ~half year