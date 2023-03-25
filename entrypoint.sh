#!/bin/bash

# Delete the existing serverconfig.xml and create it from scratch
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

# Start the AtlasCore application
cd /app && exec dotnet DOLServer.dll

