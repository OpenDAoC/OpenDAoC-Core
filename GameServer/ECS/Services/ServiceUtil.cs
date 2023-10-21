using System;
using System.Reflection;
using log4net;

namespace Core.GS
{
    public static class ServiceUtil
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void HandleServiceException<T>(Exception exception, string serviceName, T entity, GameObject entityOwner) where T : class, IManagedEntity
        {
            log.Error($"Critical error encountered in {serviceName}: {exception}");
            EntityManager.Remove(entity);

            if (entityOwner is GamePlayer player)
            {
                if (player.PlayerClass.ID == (int) EPlayerClass.Necromancer && player.IsShade)
                    player.Shade(false);

                player.Out.SendPlayerQuit(false);
                player.Quit(true);
                CraftingProgressMgr.FlushAndSaveInstance(player);
                player.SaveIntoDatabase();
            }
            else
                entityOwner?.RemoveFromWorld();
        }
    }
}
