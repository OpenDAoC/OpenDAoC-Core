using System;
using DOL.Events;
using DOL.GS;

namespace GameServerScripts.Titles
{
    public class AdministratorTitle : TranslatedNoGenderGenericEventPlayerTitle
    {
        public override DOLEvent Event => GamePlayerEvent.GameEntered;
        protected override Tuple<string, string> DescriptionValue => new("Titles.PrivLevel.Administrator", "Titles.PrivLevel.Administrator");
        protected override Func<GamePlayer, bool> SuitableMethod => player => (ePrivLevel) player.Client.Account.PrivLevel is ePrivLevel.Admin;
    }

    public class GamemasterTitle : TranslatedNoGenderGenericEventPlayerTitle
    {
        public override DOLEvent Event => GamePlayerEvent.GameEntered;
        protected override Tuple<string, string> DescriptionValue => new("Titles.PrivLevel.Gamemaster", "Titles.PrivLevel.Gamemaster");
        protected override Func<GamePlayer, bool> SuitableMethod => player => (ePrivLevel) player.Client.Account.PrivLevel is ePrivLevel.GM;
    }
}
