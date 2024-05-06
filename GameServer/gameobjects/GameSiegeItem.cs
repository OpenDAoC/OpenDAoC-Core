using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS
{
    /// <summary>
    /// Items of this class will proc on GameKeepComponent and GameKeepDoors, checked for in GameLiving-CheckWeaponMagicalEffect
    /// Used for Bruiser, or any other item that can fire a proc on keep components.  Itemtemplates must be set to DOL.GS.GameSiegeItem
    /// in the classtype field
    /// </summary>
    public class GameSiegeItem : GameInventoryItem
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private GameSiegeItem() { }

        public GameSiegeItem(DbItemTemplate template)
            : base(template)
        {
        }

        public GameSiegeItem(DbItemUnique template)
            : base(template)
        {
        }
    }
}
