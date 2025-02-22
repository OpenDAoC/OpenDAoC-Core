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
        private static DateTime? _buildTime;
        private static Lock _lock = new();

        public void OnCommand(GameClient client, string[] args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            if (!_buildTime.HasValue)
            {
                lock (_lock)
                {
                    if (!_buildTime.HasValue)
                        _buildTime = GetBuildDate(assembly);
                }
            }

            client.Out.SendMessage($"{assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title}: {_buildTime.Value}", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            static DateTime GetBuildDate(Assembly assembly)
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

                return DateTime.TryParse(buildTime, out DateTime result) ? result : default;
            }
        }
    }
}
