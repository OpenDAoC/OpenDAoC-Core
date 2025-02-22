using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [Cmd(
        "&version",
        ePrivLevel.Player,
        "Get the version of the game server",
        "/version")]
    public class VersionCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private static string _buildTime;
        private static Lock _lock = new();

        public void OnCommand(GameClient client, string[] args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            if (string.IsNullOrEmpty(_buildTime))
            {
                lock (_lock)
                {
                    if (string.IsNullOrEmpty(_buildTime))
                        _buildTime = GetBuildDate(assembly);
                }
            }

            client.Out.SendMessage($"{assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title}: {_buildTime}", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            static string GetBuildDate(Assembly assembly)
            {
                string buildTime = string.Empty;

                foreach (AssemblyMetadataAttribute attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                {
                    if (!attribute.Key.Equals("BuildTime"))
                        continue;

                    buildTime = attribute.Value;
                }

                if (string.IsNullOrEmpty(buildTime))
                    return default;

                if (!DateTime.TryParse(buildTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dateTime))
                    dateTime = default;

                return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            }
        }
    }
}
