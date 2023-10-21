using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.PacketHandler;

namespace Core.GS.PlayerTitles
{
    public class NoHelpTitle : APlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Solo Beetle";
        }

        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Solo Beetle";
        }

        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Solo Beetle title!", EChatType.CT_Important,
                EChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            const string customKey2 = "solo_to_50";
            var solo_to_50 = CoreDb<DbCoreCharacterXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
                .IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey2)));

            return player.NoHelp || solo_to_50 != null;
        }
    }
}