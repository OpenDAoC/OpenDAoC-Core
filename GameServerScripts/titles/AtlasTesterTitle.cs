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
    /// "I'm bugged" title granted to all account that have played the alpha (ended December 17th, 2021)
    /// </summary>
    public class AtlasTestTitle : TranslatedNoGenderGenericEventPlayerTitle
    {
        DateTime alphaEnd = new DateTime(2021, 12, 17);
        public override DOLEvent Event { get { return GamePlayerEvent.GameEntered; }}
        protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Atlas.Bugged", "Titles.Atlas.Bugged"); }}
        protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => player.Client.Account.CreationDate < alphaEnd; }} // Title awarded to players that played the alpha (end 17 dec 2021)
        
    }
    
    /// <summary>
    /// "Linkdead" title granted to all account that have played the alpha (ended December 17th, 2021)
    /// </summary>
    public class AtlasTestTitleLD : TranslatedNoGenderGenericEventPlayerTitle
    {
        DateTime alphaEnd = new(2021, 12, 17);
        public override DOLEvent Event { get { return GamePlayerEvent.GameEntered; }}
        protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Atlas.LD", "Titles.Atlas.LD"); }}
        protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => player.Client.Account.CreationDate < alphaEnd; }} 
    }
    
    /// <summary>
    /// "I'm growing" title granted to all account that have played the PvE Beta
    /// </summary>
    public class AtlasTestTitleGrowing : TranslatedNoGenderGenericEventPlayerTitle
    {
        DateTime alphaEnd = new(2022, 01, 30);
        public override DOLEvent Event { get { return GamePlayerEvent.GameEntered; }}
        protected override Tuple<string, string> DescriptionValue { get { return new Tuple<string, string>("Titles.Atlas.PvEBeta", "Titles.Atlas.PvEBeta"); }}
        protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => player.Client.Account.CreationDate < alphaEnd; }} 
    }
}

// protected override Func<DOL.GS.GamePlayer, bool> SuitableMethod { get { return player => DateTime.Now.Subtract(player.CreationDate).TotalDays >= 178; }} // ~half year