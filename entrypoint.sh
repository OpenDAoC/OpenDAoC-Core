#!/bin/sh

# Default values for UID and GID if not provided
APP_UID=${UID:-1000}
APP_GID=${GID:-1000}

# Check if running as root
if [ "$(id -u)" != "0" ]; then
    >&2 echo "ERROR: Not running as root. Please run as root, and pass HOST_UID and HOST_GID."
    exit 120
elif [ "${APP_UID}" = "" ]; then
    >&2 echo "ERROR: HOST_UID is not set. Please run as root, and pass HOST_UID and HOST_GID."
    exit 121
elif [ "${APP_GID}" = "" ]; then
    >&2 echo "ERROR: HOST_GID is not set. Please run as root, and pass HOST_UID and HOST_GID."
    exit 122
fi

# Handle existing group with the same GID
EXISTING_GROUP_NAME=$(getent group "${APP_GID}" | cut -d: -f1)
if [ "${EXISTING_GROUP_NAME}" != "" ]; then
    delgroup "${EXISTING_GROUP_NAME}"
fi

# (Re)create the group 'appgroup'
delgroup appgroup 2>/dev/null || true
addgroup -g "${APP_GID}" appgroup

# (Re)create the user 'appuser'
deluser appuser 2>/dev/null || true
adduser -D -H -u "${APP_UID}" -G appgroup appuser

# Delete the existing serverconfig.xml
rm -f /app/config/serverconfig.xml

# Create serverconfig.xml with environment variables
cat << EOF > /app/config/serverconfig.xml
<?xml version="1.0" encoding="utf-8"?>
<root>
    <Server>
        <Port>${SERVER_PORT}</Port>
        <IP>${SERVER_IP}</IP>
        <RegionIP>${REGION_IP}</RegionIP>
        <RegionPort>${REGION_PORT}</RegionPort>
        <UdpIP>${UDP_IP}</UdpIP>
        <UdpPort>${UDP_PORT}</UdpPort>
        <EnableUPnP>${ENABLE_UPNP}</EnableUPnP>
        <DetectRegionIP>${DETECT_REGION_IP}</DetectRegionIP>
        <ServerName>${SERVER_NAME}</ServerName>
        <ServerNameShort>${SERVER_NAME_SHORT}</ServerNameShort>
        <LogConfigFile>${LOG_CONFIG_FILE}</LogConfigFile>
        <ScriptCompilationTarget>${SCRIPT_COMPILATION_TARGET}</ScriptCompilationTarget>
        <ScriptAssemblies>${SCRIPT_ASSEMBLIES}</ScriptAssemblies>
        <EnableCompilation>${ENABLE_COMPILATION}</EnableCompilation>
        <AutoAccountCreation>${AUTO_ACCOUNT_CREATION}</AutoAccountCreation>
        <GameType>${GAME_TYPE}</GameType>
        <CheatLoggerName>${CHEAT_LOGGER_NAME}</CheatLoggerName>
        <GMActionLoggerName>${GM_ACTION_LOGGER_NAME}</GMActionLoggerName>
        <InvalidNamesFile>${INVALID_NAMES_FILE}</InvalidNamesFile>
        <DBType>${DB_TYPE}</DBType>
        <DBConnectionString>${DB_CONNECTION_STRING}</DBConnectionString>
        <DBAutosave>${DB_AUTOSAVE}</DBAutosave>
        <DBAutosaveInterval>${DB_AUTOSAVE_INTERVAL}</DBAutosaveInterval>
        <CpuUse>${CPU_USE}</CpuUse>
    </Server>
</root>
EOF

# Change ownership of the /app directory
chown -R appuser:appgroup /app

# Switch to the non-root user and start the server
exec su-exec appuser sh -c "cd /app && dotnet CoreServer.dll"
