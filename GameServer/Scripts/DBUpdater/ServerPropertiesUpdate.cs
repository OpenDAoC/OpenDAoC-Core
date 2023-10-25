using Core.Database.Tables;
using Core.GS.Database;
using log4net;

namespace Core.GS.Scripts;

[DbUpdate]
public class ServerPropertiesUpdate : IDbUpdater
{
    /// <summary>
    /// Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public void Update()
    {
        RemoveACLKUNLS();
    }

    #region RemoveACLKUNLS
    /// <summary>
    /// Removes the no longer used 'allowed_custom_language_keys' and 'use_new_language_system' entries.
    /// </summary>
    private void RemoveACLKUNLS()
    {
        log.Info("Updating the ServerProperty table...");

        var properties = GameServer.Database.SelectAllObjects<DbServerProperty>();

        bool aclkFound = false;
        bool unlsFound = false;
        foreach (DbServerProperty property in properties)
        {
            if (property.Key != "allowed_custom_language_keys" && property.Key != "use_new_language_system")
                continue;

            if (property.Key == "allowed_custom_language_keys")
                aclkFound = true;

            if (property.Key == "use_new_language_system")
                unlsFound = true;

            GameServer.Database.DeleteObject(property);

            if (aclkFound && unlsFound)
                break;
        }

        log.Info("ServerProperty table update complete!");
    }
    #endregion RemoveACLKUNLS
}