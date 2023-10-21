using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;

namespace Core.GS.PlayerTitles
{
    public class HardcoreTitle : APlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Hardcore";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Hardcore";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Hardcore title!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return player.HCFlag || player.HCCompleted;
        }
    }
    
    public class HardCoreSoloTitle : APlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Hardcore Solo Beetle";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Hardcore Solo Beetle";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Hardcore Solo Beetle title!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            const string customKey2 = "solo_to_50";
            var solo_to_50 = CoreDb<DbCoreCharacterXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey2)));
            
            return (player.HCFlag || player.HCCompleted) && (player.NoHelp || solo_to_50 != null);
        }
    }
}