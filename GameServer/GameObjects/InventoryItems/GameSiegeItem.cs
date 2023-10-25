using System.Reflection;
using Core.Database.Tables;
using log4net;

namespace Core.GS;

/// <summary>
/// Items of this class will proc on GameKeepComponent and GameKeepDoors, checked for in GameLiving-CheckWeaponMagicalEffect
/// Used for Bruiser, or any other item that can fire a proc on keep components.  Itemtemplates must be set to Core.GS.GameSiegeItem
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

        public GameSiegeItem(DbInventoryItem item)
                : base(item)
        {
        }
}