using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DOL.AI.Brain;
using DOL.Config;
using DOL.Events;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.GS.ServerRules;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ScriptMgr
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static Dictionary<string, Assembly> m_compiledScripts = new();
        private static ConcurrentDictionary<eSpellType, Func<GameLiving, Spell, SpellLine, ISpellHandler>> _spellHandlerConstructorCache = new();
        private static ConcurrentDictionary<int, Func<ICharacterClass>> _characterClassConstructorCache = new();

        /// <summary>
        /// This class will hold all info about a gamecommand
        /// </summary>
        public class GameCommand
        {
            public String[] Usage { get; set; }
            public string m_cmd;
            public uint m_lvl;
            public string m_desc;
            public ICommandHandler m_cmdHandler;
        }

        /// <summary>
        /// Collection of every /command
        /// </summary>
        private static Dictionary<string, GameCommand> m_gameCommands = new Dictionary<string, GameCommand>(StringComparer.InvariantCultureIgnoreCase);


        /// <summary>
        /// Get an array of all script assemblies
        /// </summary>
        public static Assembly[] Scripts
        {
            get
            {
                return m_compiledScripts.Values.ToArray();
            }
        }

        /// <summary>
        /// Get an array of GameServer Assembly with all scripts assemblies
        /// </summary>
        public static Assembly[] GameServerScripts
        {
            get
            {
                return m_compiledScripts.Values.Concat(new[] { typeof(GameServer).Assembly }).ToArray();
            }
        }

        /// <summary>
        /// Get all loaded assemblies with Scripts First
        /// </summary>
        public static Assembly[] AllAssemblies
        {
            get
            {
                return Scripts.Union(AppDomain.CurrentDomain.GetAssemblies()).ToArray();
            }
        }

        /// <summary>
        /// Get all loaded assemblies with Scripts Last
        /// </summary>
        public static Assembly[] AllAssembliesScriptsLast
        {
            get
            {
                return AppDomain.CurrentDomain.GetAssemblies().Where(asm => !Scripts.Contains(asm)).Concat(Scripts).ToArray();
            }
        }

        /// <summary>
        /// Gets the requested command if it exists
        /// </summary>
        /// <param name="commandName">The command to retrieve</param>
        /// <returns>Returns the command if it exists, otherwise the return value is null</returns>
        public static GameCommand GetCommand(string commandName)
        {
            GameCommand cmd;
            if (m_gameCommands.TryGetValue(commandName, out cmd))
                return cmd;

            return null;
        }

        /// <summary>
        /// Looking for exact match first, then, if nothing found, trying to guess command using first letters
        /// </summary>
        /// <param name="commandName">The command to retrieve</param>
        /// <returns>Returns the command if it exists, otherwise the return value is null</returns>
        public static GameCommand GuessCommand(string commandName)
        {
            GameCommand cmd;
            if (m_gameCommands.TryGetValue(commandName, out cmd))
                return cmd;

            // Trying to guess the command
            var commands = m_gameCommands.Where(kv => kv.Value != null && kv.Key.StartsWith(commandName, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value);

            if (commands.Count() == 1)
                return commands.First();

            return null;
        }

        /// <summary>
        /// Returns an array of all the available commands with the specified plvl and their descriptions
        /// </summary>
        /// <param name="plvl">plvl of the commands to get</param>
        /// <param name="addDesc"></param>
        /// <returns></returns>
        public static SortedDictionary<ePrivLevel, List<string>> GetCommandList(bool addDesc)
        {
            Dictionary<ePrivLevel, List<string>> sortedCommands = m_gameCommands
                .GroupBy(kvp => (ePrivLevel) kvp.Value.m_lvl)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(kv => string.Format("* /{0}{2}{1}", kv.Key.Remove(0, 1), addDesc ? kv.Value.m_desc : string.Empty, addDesc ? " - " : string.Empty))
                        .OrderBy(cmd => cmd)
                        .ToList()
                );

            return new SortedDictionary<ePrivLevel, List<string>>(sortedCommands);
        }

        /// <summary>
        /// Parses a directory for all source files
        /// </summary>
        /// <param name="path">The root directory to start the search in</param>
        /// <param name="filter">A filter representing the types of files to search for</param>
        /// <param name="deep">True if subdirectories should be included</param>
        /// <returns>An ArrayList containing FileInfo's for all files in the path</returns>
        private static IList<FileInfo> ParseDirectory(DirectoryInfo path, string filter, bool deep)
        {
            if (!path.Exists)
                return new List<FileInfo>();

            return path.GetFiles(filter, SearchOption.TopDirectoryOnly).Union(deep ? path.GetDirectories().Where(di => !di.Name.Equals("obj", StringComparison.OrdinalIgnoreCase)).SelectMany(di => di.GetFiles(filter, SearchOption.AllDirectories)) : new FileInfo[0]).ToList();
        }

        /// <summary>
        /// Searches the script assembly for all command handlers
        /// </summary>
        /// <returns>True if succeeded</returns>
        public static bool LoadCommands(bool quiet = false)
        {
            m_gameCommands.Clear();

            //build array of disabled commands
            string[] disabledarray = ServerProperties.Properties.DISABLED_COMMANDS.Split(';');

            foreach (var script in GameServerScripts)
            {
                if (log.IsDebugEnabled)
                    log.Debug("ScriptMgr: Searching for commands in " + script.GetName());
                // Walk through each type in the assembly
                foreach (Type type in script.GetTypes())
                {
                    // Pick up a class
                    if (type.IsClass != true) continue;
                    if (type.GetInterface("DOL.GS.Commands.ICommandHandler") == null) continue;

                    try
                    {
                        object[] objs = type.GetCustomAttributes(typeof(CmdAttribute), false);
                        foreach (CmdAttribute attrib in objs)
                        {
                            bool disabled = false;
                            foreach (string str in disabledarray)
                            {
                                if (attrib.Cmd.Replace('&', '/') == str)
                                {
                                    disabled = true;

                                    if (log.IsInfoEnabled)
                                        log.Info("Will not load command " + attrib.Cmd + " as it is disabled in server properties");

                                    break;
                                }
                            }

                            if (disabled)
                                continue;

                            if (m_gameCommands.ContainsKey(attrib.Cmd))
                            {
                                if (log.IsInfoEnabled)
                                    log.Info(attrib.Cmd + " from " + script.GetName() + " has been suppressed, a command of that type already exists!");

                                continue;
                            }
                            if (log.IsDebugEnabled && quiet == false)
                                log.Debug("ScriptMgr: Command - '" + attrib.Cmd + "' - (" + attrib.Description + ") required plvl:" + attrib.Level);

                            var cmd = new GameCommand();
                            cmd.Usage = attrib.Usage;
                            cmd.m_cmd = attrib.Cmd;
                            cmd.m_lvl = attrib.Level;
                            cmd.m_desc = attrib.Description;
                            cmd.m_cmdHandler = (ICommandHandler)Activator.CreateInstance(type);
                            m_gameCommands.Add(attrib.Cmd, cmd);
                            if (attrib.Aliases != null)
                            {
                                foreach (string alias in attrib.Aliases)
                                {
                                    m_gameCommands.Add(alias, cmd);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("LoadCommands", e);
                    }
                }
            }

            if (log.IsInfoEnabled)
                log.Info("Loaded " + m_gameCommands.Count + " commands!");

            return true;
        }

        /// <summary>
        /// Called when a command needs to be handled
        /// </summary>
        /// <param name="client">Client executing the command</param>
        /// <param name="cmdLine">Args for the command</param>
        /// <returns>True if succeeded</returns>
        public static bool HandleCommand(GameClient client, string cmdLine)
        {
            try
            {
                // parse args
                string[] pars = ParseCmdLine(cmdLine);
                GameCommand myCommand = GuessCommand(pars[0]);

                //If there is no such command, return false
                if (myCommand == null) return false;

                if (client.Account.PrivLevel < myCommand.m_lvl)
                {
                    if (!SinglePermission.HasPermission(client.Player, pars[0].Substring(1, pars[0].Length - 1)))
                    {
                        if (pars[0][0] == '&')
                            pars[0] = '/' + pars[0].Remove(0, 1);
                        //client.Out.SendMessage("You do not have enough priveleges to use " + pars[0], eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        //why should a player know the existing commands..
                        client.Out.SendMessage("No such command (" + pars[0] + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return true;
                    }
                    //else execute the command
                }

                ExecuteCommand(client, myCommand, pars);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("HandleCommand", e);
            }
            return true;
        }

        /// <summary>
        /// Called when a command needs to be handled without plvl being taken into consideration
        /// </summary>
        /// <param name="client">Client executing the command</param>
        /// <param name="cmdLine">Args for the command</param>
        /// <returns>True if succeeded</returns>
        public static bool HandleCommandNoPlvl(GameClient client, string cmdLine)
        {
            try
            {
                string[] pars = ParseCmdLine(cmdLine);
                GameCommand myCommand = GuessCommand(pars[0]);

                //If there is no such command, return false
                if (myCommand == null) return false;

                ExecuteCommand(client, myCommand, pars);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("HandleCommandNoPlvl", e);
            }
            return true;
        }

        /// <summary>
        /// Splits string to substrings
        /// </summary>
        /// <param name="cmdLine">string that should be split</param>
        /// <returns>Array of substrings</returns>
        private static string[] ParseCmdLine(string cmdLine)
        {
            if (cmdLine == null)
            {
                throw new ArgumentNullException("cmdLine");
            }

            List<string> args = new List<string>();
            int state = 0;
            StringBuilder arg = new StringBuilder(cmdLine.Length >> 1);

            for (int i = 0; i < cmdLine.Length; i++)
            {
                char c = cmdLine[i];
                switch (state)
                {
                    case 0: // waiting for first arg char
                        if (c == ' ') continue;
                        arg.Length = 0;
                        if (c == '"') state = 2;
                        else
                        {
                            state = 1;
                            i--;
                        }
                        break;
                    case 1: // reading arg
                        if (c == ' ')
                        {
                            args.Add(arg.ToString());
                            state = 0;
                        }
                        arg.Append(c);
                        break;
                    case 2: // reading string
                        if (c == '"')
                        {
                            args.Add(arg.ToString());
                            state = 0;
                        }
                        arg.Append(c);
                        break;
                }
            }
            if (state != 0) args.Add(arg.ToString());

            string[] pars = new string[args.Count];
            args.CopyTo(pars);

            return pars;
        }

        /// <summary>
        /// Checks for 'help' param and executes command
        /// </summary>
        /// <param name="client">Client executing the command</param>
        /// <param name="myCommand">command to be executed</param>
        /// <param name="pars">Args for the command</param>
        /// <returns>Command result</returns>
        private static void ExecuteCommand(GameClient client, GameCommand myCommand, string[] pars)
        {
            // what you type in script is what you get; needed for overloaded scripts,
            // like emotes, to handle case insensitive and guessed commands correctly
            pars[0] = myCommand.m_cmd;

            //Log the command usage
            if (client.Account == null || ((ServerProperties.Properties.LOG_ALL_GM_COMMANDS && client.Account.PrivLevel > 1) || myCommand.m_lvl > 1))
            {
                string commandText = String.Join(" ", pars);
                string targetName = "(no target)";
                string playerName = (client.Player == null) ? "(player is null)" : client.Player.Name;
                string accountName = (client.Account == null) ? "account is null" : client.Account.Name;

                if (client.Player == null)
                {
                    targetName = "(player is null)";
                }
                else if (client.Player.TargetObject != null)
                {
                    targetName = client.Player.TargetObject.Name;
                    if (client.Player.TargetObject is GamePlayer)
                        targetName += "(" + ((GamePlayer)client.Player.TargetObject).Client.Account.Name + ")";
                }
                GameServer.Instance.LogGMAction("Command: " + playerName + "(" + accountName + ") -> " + targetName + " - \"/" + commandText.Remove(0, 1) + "\"");

            }
            if (client.Player != null)
            {
                client.Player.Notify(DOL.Events.GamePlayerEvent.ExecuteCommand, new ExecuteCommandEventArgs(client.Player, myCommand, pars));
            }
            myCommand.m_cmdHandler.OnCommand(client, pars);
        }

        /// <summary>
        /// Compiles the scripts into an assembly
        /// </summary>
        /// <param name="compileVB">True if the source files will be in VB.NET</param>
        /// <param name="scriptFolder">Path to the source files</param>
        /// <param name="outputPath">Name of the assembly to be generated</param>
        /// <param name="asm_names">References to other assemblies</param>
        /// <returns>True if succeeded</returns>
        public static bool CompileScripts(bool compileVB, string scriptFolder, string outputPath, string[] asm_names)
        {
            var outputFile = new FileInfo(outputPath);
            if (!scriptFolder.EndsWith(@"\") && !scriptFolder.EndsWith(@"/"))
                scriptFolder = scriptFolder + "/";

            //Reset the assemblies
            m_compiledScripts.Clear();

            //Check if there are any scripts, if no scripts exist, that is fine as well
            IList<FileInfo> files = ParseDirectory(new DirectoryInfo(scriptFolder), compileVB ? "*.vb" : "*.cs", true);
            if (files.Count == 0)
            {
                return true;
            }

            //Recompile is required as standard
            bool recompileRequired = true;

            //This file should hold the script infos
            var configFile = new FileInfo(outputFile.FullName + ".xml");

            //If the script assembly is missing, recompile is required
            if (!outputFile.Exists)
            {
                if (log.IsDebugEnabled)
                    log.Debug("Script assembly missing, recompile required!");
            }
            else
            {
                //Script assembly found, check if we have a file modify info
                if (configFile.Exists)
                {
                    //Ok, we have a config file containing the script file sizes and dates
                    //let's check if any script was modified since last compiling them
                    if (log.IsDebugEnabled)
                        log.Debug("Found script info file");

                    try
                    {
                        XmlConfigFile config = XmlConfigFile.ParseXMLFile(configFile);

                        //Assume no scripts changed
                        recompileRequired = false;

                        Dictionary<string, ConfigElement> precompiledScripts = new Dictionary<string, ConfigElement>(config.Children);

                        //Now test the files
                        foreach (FileInfo finfo in files)
                        {
                            if (config[finfo.FullName]["size"].GetInt(0) != finfo.Length
                                || config[finfo.FullName]["lastmodified"].GetLong(0) != finfo.LastWriteTime.ToFileTime())
                            {
                                //Recompile required
                                recompileRequired = true;
                                break;
                            }
                            precompiledScripts.Remove(finfo.FullName);
                        }

                        recompileRequired |= precompiledScripts.Count > 0; // some compiled script was removed

                        if (recompileRequired && log.IsDebugEnabled)
                        {
                            log.Debug("At least one file was modified, recompile required!");
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Error during script info file to scripts compare", e);
                    }
                }
                else
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Script info file missing, recompile required!");
                }
            }

            //If we need no compiling, we load the existing assembly!
            if (!recompileRequired)
            {
                recompileRequired = !LoadAssembly(outputFile.FullName);

                if (!recompileRequired)
                {
                    //Return success!
                    return true;
                }
            }

            //We need a recompile, if the dll exists, delete it firsthand
            if (outputFile.Exists)
                outputFile.Delete();

            var compilationSuccessful = false;
            try
            {
                var compiler = new DOLScriptCompiler();
                if (compileVB) compiler.SetToVisualBasicNet();

                var compiledAssembly = compiler.Compile(outputFile, files);
                foreach (var errorMessage in compiler.GetDetailedErrorMessages())
                {
                    log.Error(errorMessage);
                }
                if (compiler.HasErrors) return false;
                compilationSuccessful = true;

                AddOrReplaceAssembly(compiledAssembly);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("CompileScripts", e);
                m_compiledScripts.Clear();
            }
            //now notify our callbacks
            if (!compilationSuccessful) return false;

            var newconfig = new XmlConfigFile();
            foreach (var finfo in files)
            {
                newconfig[finfo.FullName]["size"].Set(finfo.Length);
                newconfig[finfo.FullName]["lastmodified"].Set(finfo.LastWriteTime.ToFileTime());
            }
            if (log.IsDebugEnabled)
                log.Debug("Writing script info file");

            newconfig.Save(configFile);

            return true;
        }



        /// <summary>
        /// Load an Assembly from DLL path.
        /// </summary>
        /// <param name="dllName">path to Assembly DLL File</param>
        /// <returns>True if assembly is loaded</returns>
        public static bool LoadAssembly(string dllName)
        {
            try
            {
                Assembly asm = Assembly.LoadFrom(dllName);
                ScriptMgr.AddOrReplaceAssembly(asm);

                if (log.IsInfoEnabled)
                    log.InfoFormat("Assembly {0} loaded successfully from path {1}", asm.FullName, dllName);

                return true;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error loading Assembly from path {0} - {1}", dllName, e);
            }

            return false;
        }

        /// <summary>
        /// Add or replace an assembly in the collection of compiled assemblies
        /// </summary>
        /// <param name="assembly"></param>
        public static void AddOrReplaceAssembly(Assembly assembly)
        {
            if (!m_compiledScripts.TryAdd(assembly.FullName, assembly))
            {
                m_compiledScripts[assembly.FullName] = assembly;

                if (log.IsDebugEnabled)
                    log.Debug($"Replaced assembly {assembly.FullName}");
            }
        }

        /// <summary>
        /// Removes an assembly from the game servers list of usable assemblies
        /// </summary>
        /// <param name="fullName"></param>
        public static bool RemoveAssembly(string fullName)
        {
            return m_compiledScripts.ContainsKey(fullName);
        }

        /// <summary>
        /// searches the given assembly for AbilityActionHandlers
        /// </summary>
        /// <param name="asm">The assembly to search through</param>
        /// <returns>Hashmap consisting of keyName => AbilityActionHandler Type</returns>
        public static IList<KeyValuePair<string, Type>> FindAllAbilityActionHandler(Assembly asm)
        {
            List<KeyValuePair<string, Type>> abHandler = new List<KeyValuePair<string, Type>>();
            if (asm != null)
            {
                foreach (Type type in asm.GetTypes())
                {
                    if (!type.IsClass)
                        continue;
                    if (type.GetInterface("DOL.GS.IAbilityActionHandler") == null)
                        continue;
                    if (type.IsAbstract)
                        continue;

                    object[] objs = type.GetCustomAttributes(typeof(SkillHandlerAttribute), false);
                    for (int i = 0; i < objs.Length; i++)
                    {
                        if (objs[i] is SkillHandlerAttribute)
                        {
                            SkillHandlerAttribute attr = objs[i] as SkillHandlerAttribute;
                            abHandler.Add(new KeyValuePair<string, Type>(attr.KeyName, type));
                            //DOLConsole.LogLine("Found ability action handler "+attr.KeyName+": "+type);
                            //									break;
                        }
                    }
                }
            }
            return abHandler;
        }

        /// <summary>
        /// searches the script directory for SpecActionHandlers
        /// </summary>
        /// <param name="asm">The assembly to search through</param>
        /// <returns>Hashmap consisting of keyName => SpecActionHandler Type</returns>
        public static IList<KeyValuePair<string, Type>> FindAllSpecActionHandler(Assembly asm)
        {
            List<KeyValuePair<string, Type>> specHandler = new List<KeyValuePair<string, Type>>();
            if (asm != null)
            {
                foreach (Type type in asm.GetTypes())
                {
                    if (!type.IsClass)
                        continue;
                    if (type.GetInterface("DOL.GS.ISpecActionHandler") == null)
                        continue;
                    if (type.IsAbstract)
                        continue;

                    object[] objs = type.GetCustomAttributes(typeof(SkillHandlerAttribute), false);
                    for (int i = 0; i < objs.Length; i++)
                    {
                        if (objs[i] is SkillHandlerAttribute)
                        {
                            SkillHandlerAttribute attr = objs[0] as SkillHandlerAttribute;
                            specHandler.Add(new KeyValuePair<string, Type>(attr.KeyName, type));
                            //DOLConsole.LogLine("Found spec action handler "+attr.KeyName+": "+type);
                            break;
                        }
                    }
                }
            }
            return specHandler;
        }


        /// <summary>
        /// Searches for ClassSpec's by id in a given assembly
        /// </summary>
        /// <param name="id">the classid to search</param>
        /// <returns>ClassSpec that was found or null if not found</returns>
        public static ICharacterClass FindCharacterClass(int id)
        {
            if (_characterClassConstructorCache.TryGetValue(id, out var constructor))
                return constructor();

            foreach (Assembly asm in GameServerScripts)
            {
                foreach (Type type in asm.GetTypes())
                {
                    // Pick up a class
                    if (type.IsClass != true)
                        continue;

                    if (type.IsAbstract)
                        continue;

                    if (type.GetInterface("DOL.GS.ICharacterClass") == null)
                        continue;

                    try
                    {
                        object[] objs = type.GetCustomAttributes(typeof(CharacterClassAttribute), false);

                        foreach (CharacterClassAttribute attrib in objs)
                        {
                            if (attrib.ID == id)
                                return _characterClassConstructorCache.GetOrAdd(id, (key) => CompiledConstructorFactory.CompileConstructor(type, []) as Func<ICharacterClass>)();
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error(e);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Return a CharacterClass "Base" Class (or current Class if already base)
        /// </summary>
        /// <param name="id">the classid to search</param>
        /// <returns>Base ClassSpec that was found or null if not found</returns>
        public static ICharacterClass FindCharacterBaseClass(int id)
        {
            var charClass = FindCharacterClass(id);

            if (charClass == null)
                return null;

            if (!charClass.HasAdvancedFromBaseClass())
                return charClass;

            try
            {
                object[] objs = charClass.GetType().BaseType.GetCustomAttributes(typeof(CharacterClassAttribute), true);
                foreach (CharacterClassAttribute attrib in objs)
                {
                    if (attrib.Name.Equals(charClass.BaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        var baseClass = FindCharacterClass(attrib.ID);
                        if (baseClass != null && !baseClass.HasAdvancedFromBaseClass())
                            return baseClass;
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("FindCharacterBaseClass", e);
            }

            return null;
        }

        /// <summary>
        /// Searches for NPC guild scripts
        /// </summary>
        /// <param name="realm">Realm for searching handlers</param>
        /// <param name="asm">The assembly to search through</param>
        /// <returns>
        /// all handlers that were found, guildname(string) => classtype(Type)
        /// </returns>
        protected static Hashtable FindAllNPCGuildScriptClasses(eRealm realm, Assembly asm)
        {
            Hashtable ht = new Hashtable();
            if (asm != null)
            {
                foreach (Type type in asm.GetTypes())
                {
                    // Pick up a class
                    if (type.IsClass != true) continue;
                    if (!type.IsSubclassOf(typeof(GameNPC))) continue;

                    try
                    {
                        object[] objs = type.GetCustomAttributes(typeof(NPCGuildScriptAttribute), false);
                        if (objs.Length == 0) continue;

                        foreach (NPCGuildScriptAttribute attrib in objs)
                        {
                            if (attrib.Realm == eRealm.None || attrib.Realm == realm)
                            {
                                ht[attrib.GuildName] = type;
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("FindAllNPCGuildScriptClasses", e);
                    }
                }
            }
            return ht;
        }

        protected static Hashtable[] m_gs_guilds = new Hashtable[(int)eRealm._Last + 1];
        protected static Hashtable[] m_script_guilds = new Hashtable[(int)eRealm._Last + 1];

        /// <summary>
        /// searches for a npc guild script
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="realm"></param>
        /// <returns>type of class for searched npc guild or null</returns>
        public static Type FindNPCGuildScriptClass(string guild, eRealm realm)
        {
            if (string.IsNullOrEmpty(guild)) return null;

            Type type = null;
            if (m_script_guilds[(int)realm] == null)
            {
                Hashtable allScriptGuilds = new Hashtable();

                foreach (Assembly asm in GameServerScripts)
                {
                    Hashtable scriptGuilds = FindAllNPCGuildScriptClasses(realm, asm);
                    if (scriptGuilds == null) continue;
                    foreach (DictionaryEntry entry in scriptGuilds)
                    {
                        if (allScriptGuilds.ContainsKey(entry.Key)) continue; // guild is already found
                        allScriptGuilds.Add(entry.Key, entry.Value);
                    }
                }
                m_script_guilds[(int)realm] = allScriptGuilds;
            }

            //SmallHorse: First test if no realm-guild hashmap is null, then test further
            //Also ... you can not use "nullobject as anytype" ... this crashes!
            //You have to test against NULL result before casting it... read msdn doku
            if (m_script_guilds[(int)realm] != null && m_script_guilds[(int)realm][guild] != null)
                type = m_script_guilds[(int)realm][guild] as Type;

            if (type == null)
            {
                if (m_gs_guilds[(int)realm] == null)
                {
                    Assembly gasm = Assembly.GetAssembly(typeof(GameServer));
                    m_gs_guilds[(int)realm] = FindAllNPCGuildScriptClasses(realm, gasm);
                }
            }

            //SmallHorse: First test if no realm-guild hashmap is null, then test further
            //Also ... you can not use "nullobject as anytype" ... this crashes!
            //You have to test against NULL result before casting it... read msdn doku
            if (m_gs_guilds[(int)realm] != null && m_gs_guilds[(int)realm][guild] != null)
                type = m_gs_guilds[(int)realm][guild] as Type;

            return type;
        }


        private static Type m_defaultControlledBrainType = typeof(ControlledMobBrain);
        public static Type DefaultControlledBrainType
        {
            get { return m_defaultControlledBrainType; }
            set { m_defaultControlledBrainType = value; }
        }

        /// <summary>
        /// Constructs a new brain for player controlled npcs
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static IControlledBrain CreateControlledBrain(GamePlayer owner)
        {
            Type[] constructorParams = new Type[] { typeof(GamePlayer) };
            ConstructorInfo handlerConstructor = m_defaultControlledBrainType.GetConstructor(constructorParams);
            return (IControlledBrain)handlerConstructor.Invoke(new object[] { owner });
        }

        /// <summary>
        /// Create a spell handler for caster with given spell
        /// </summary>
        /// <param name="caster">caster that uses the spell</param>
        /// <param name="spell">the spell itself</param>
        /// <param name="line">the line that spell belongs to or null</param>
        /// <returns>spellhandler or null if not found</returns>
        public static ISpellHandler CreateSpellHandler(GameLiving caster, Spell spell, SpellLine line)
        {
            if (spell == null)
                return null;

            // try to find it in assemblies when not in cache
            if (!_spellHandlerConstructorCache.TryGetValue(spell.SpellType, out var handlerConstructor))
                handlerConstructor = CacheSpellHandlerConstructor(spell.SpellType);

            if (handlerConstructor != null)
            {
                try
                {
                    return handlerConstructor(caster, spell, line);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Failed to create spellhandler " + handlerConstructor, e);
                }
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Couldn't find spell handler for spell type " + spell.SpellType);
            }

            return null;
        }

        public static void CacheSpellHandlerConstructors()
        {
            foreach (eSpellType spellType in Enum.GetValues<eSpellType>())
                CacheSpellHandlerConstructor(spellType);
        }

        public static Func<GameLiving, Spell, SpellLine, ISpellHandler> CacheSpellHandlerConstructor(eSpellType spellType)
        {
            Func<GameLiving, Spell, SpellLine, ISpellHandler> handlerConstructor = null;

            foreach (Assembly script in GameServerScripts)
            {
                foreach (Type type in script.GetTypes())
                {
                    if (type.IsClass != true || type.GetInterface("DOL.GS.Spells.ISpellHandler") == null)
                        continue;

                    object[] objects = type.GetCustomAttributes(typeof(SpellHandlerAttribute), false);

                    if (objects.Length == 0)
                        continue;

                    foreach (SpellHandlerAttribute attrib in objects)
                    {
                        if (attrib.SpellType == spellType)
                        {
                            try
                            {
                                handlerConstructor = CompiledConstructorFactory.CompileConstructor(type, [typeof(GameLiving), typeof(Spell), typeof(SpellLine)]) as Func<GameLiving, Spell, SpellLine, ISpellHandler>;
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                    log.Error($"Couldn't find a SpellHandler constructor for {spellType}");

                                continue;
                            }

                            if (log.IsDebugEnabled)
                                log.Debug($"Found spell handler {type}");

                            break;
                        }
                    }

                    if (handlerConstructor == null)
                        continue;

                    _spellHandlerConstructorCache.TryAdd(spellType, handlerConstructor);
                    return handlerConstructor;
                }
            }

            return null;
        }

        /// <summary>
        /// Clear all spell handlers from the cashe, forcing a reload when a spell is cast
        /// </summary>
        public static void ClearSpellHandlerCache()
        {
            _spellHandlerConstructorCache.Clear();
        }

        /// <summary>
        /// Create server rules handler for specified server type
        /// </summary>
        /// <param name="serverType">server type used to look for rules handler</param>
        /// <returns>server rules handler or normal server type handler if errors</returns>
        public static IServerRules CreateServerRules(EGameServerType serverType)
        {
            Type rules = null;

            // first search in scripts
            foreach (Assembly script in Scripts)
            {
                foreach (Type type in script.GetTypes())
                {
                    if (type.IsClass == false) continue;
                    if (type.GetInterface("DOL.GS.ServerRules.IServerRules") == null) continue;

                    // look for attribute
                    try
                    {
                        object[] objs = type.GetCustomAttributes(typeof(ServerRulesAttribute), false);
                        if (objs.Length == 0) continue;

                        foreach (ServerRulesAttribute attrib in objs)
                        {
                            if (attrib.ServerType == serverType)
                            {
                                rules = type;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("CreateServerRules", e);
                    }
                    if (rules != null) break;
                }
            }

            if (rules == null)
            {
                // second search in gameserver
                foreach (Type type in Assembly.GetAssembly(typeof(GameServer)).GetTypes())
                {
                    if (type.IsClass == false) continue;
                    if (type.GetInterface("DOL.GS.ServerRules.IServerRules") == null) continue;

                    // look for attribute
                    try
                    {
                        object[] objs = type.GetCustomAttributes(typeof(ServerRulesAttribute), false);
                        if (objs.Length == 0) continue;

                        foreach (ServerRulesAttribute attrib in objs)
                        {
                            if (attrib.ServerType == serverType)
                            {
                                rules = type;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("CreateServerRules", e);
                    }
                    if (rules != null) break;
                }

            }

            if (rules != null)
            {
                try
                {
                    IServerRules rls = (IServerRules)Activator.CreateInstance(rules, null);
                    if (log.IsInfoEnabled)
                        log.Info("Found server rules for " + serverType + " server type (" + rls.RulesDescription() + ").");
                    return rls;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error("CreateServerRules", e);
                }
            }
            if (log.IsWarnEnabled)
                log.Warn("Rules for " + serverType + " server type not found, using \"normal\" server type rules.");
            return new NormalServerRules();
        }

        /// <summary>
        /// Search for a type by name; first in GameServer assembly then in scripts assemblies
        /// </summary>
        /// <param name="name">The type name</param>
        /// <returns>Found type or null</returns>
        public static Type GetType(string name)
        {
            Type t = typeof(GameServer).Assembly.GetType(name);
            if (t == null)
            {
                foreach (Assembly asm in Scripts)
                {
                    t = asm.GetType(name);
                    if (t == null) continue;
                    return t;
                }
            }
            else
            {
                return t;
            }
            return null;
        }

        /// <summary>
        /// Finds all classes that derive from given type.
        /// First check scripts then GameServer assembly.
        /// </summary>
        /// <param name="baseType">The base class type.</param>
        /// <returns>Array of types or empty array</returns>
        public static Type[] GetDerivedClasses(Type baseType)
        {
            if (baseType == null)
                return new Type[0];

            List<Type> types = new List<Type>();

            foreach (Assembly asm in GameServerScripts)
            {
                foreach (Type t in asm.GetTypes())
                {
                    if (t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
                        types.Add(t);
                }
            }

            return types.ToArray();
        }

        /// <summary>
        /// Create new instance of ClassType, Looking through Assemblies and Scripts with given param
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static C CreateObjectFromClassType<C, T>(string classType, T args)
            where C : class
        {
            foreach (Assembly assembly in AllAssembliesScriptsLast)
            {
                try
                {
                    C instance = assembly.CreateInstance(classType, false, BindingFlags.CreateInstance, null, new object[] { args }, null, null) as C;
                    if (instance != null)
                        return instance;
                }
                catch (Exception)
                {
                }

            }

            return null;
        }

        /// <summary>
        /// Create new instance of ClassType, Looking through Scripts then Assemblies with given param
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static C CreateScriptObjectFromClassType<C, T>(string classType, T args)
            where C : class
        {
            foreach (Assembly assembly in AllAssemblies)
            {
                try
                {
                    C instance = assembly.CreateInstance(classType, false, BindingFlags.CreateInstance, null, new object[] { args }, null, null) as C;
                    if (instance != null)
                        return instance;
                }
                catch (Exception)
                {
                }

            }

            return null;
        }
    }
}
