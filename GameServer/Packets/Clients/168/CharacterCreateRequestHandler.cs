using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Core.Database;
using Core.Events;
using Core.GS.ServerProperties;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Character Create and Customization handler.  Please maintain all commented debug statements
    /// in order to support future debugging. - Tolakram
    /// </summary>
    [PacketHandler(EPacketHandlerType.TCP, EClientPackets.CharacterCreateRequest, "Handles character creation requests", EClientStatus.LoggedIn)]
    public class CharacterCreateRequestHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Max Points to allow on player creation
        /// </summary>
        private const int MaxStartingBonusPoints = 30;

        /// <summary>
        /// Client Operation Value.
        /// </summary>
        public enum eOperation: uint
        {
            Delete = 0x12345678,
            Create = 0x23456789,
            Customize = 0x3456789A,
            Unknown = 0x456789AB,
        }

        public void HandlePacket(GameClient client, GsPacketIn packet)
        {
            bool needRefresh = false;

            if (client.Version > GameClient.eClientVersion.Version1124) // 1125 support
            {
                var pakdata = new CreationCharacterData(packet, client);

                // Graveen: changed the following to allow GMs to have special chars in their names (_,-, etc..)
                var nameCheck = new Regex("^[A-Z][a-zA-Z]");
                if (!string.IsNullOrEmpty(pakdata.CharName) && (pakdata.CharName.Length < 3 || !nameCheck.IsMatch(pakdata.CharName)))
                {
                    if ((EPrivLevel)client.Account.PrivLevel == EPrivLevel.Player)
                    {
                        if (Properties.BAN_HACKERS)
                        {
                            client.BanAccount(string.Format("Autoban bad CharName '{0}'", pakdata.CharName));
                        }

                        client.Disconnect();
                        return;
                    }
                }

                switch (pakdata.Operation)
                {
                    case 3: // delete request
                        if (string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Deletion in 1.104+ check for removed character.
                            needRefresh |= CheckForDeletedCharacter(client.Account.Name, client, pakdata.CharacterSlot);
                        }
                        break;
                    case 2: // Customize face or stats
                        if (!string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Candidate for Customizing ?
                            var character = client.Account.Characters != null ? client.Account.Characters.FirstOrDefault(ch => ch.Name.Equals(pakdata.CharName, StringComparison.OrdinalIgnoreCase)) : null;
                            if (character != null)
                            {
                                needRefresh |= CheckCharacterForUpdates1125(pakdata, client, character, pakdata.CustomizeType);
                            }
                        }
                        break;
                    case 1: // create request
                        if (!string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Candidate for Creation ?
                            var character = client.Account.Characters != null ? client.Account.Characters.FirstOrDefault(ch => ch.Name.Equals(pakdata.CharName, StringComparison.OrdinalIgnoreCase)) : null;
                            if (character == null)
                            {
                                needRefresh |= CreateCharacter(pakdata, client, pakdata.CharacterSlot);
                            }
                        }
                        break;
                    default:
                        break;
                }

                if (needRefresh)
                {
                    client.Out.SendLoginGranted();
                    // live actually just sends server login request response... i think this sets the realm select button again too
                }
                return;
            }

            string accountName = packet.ReadString(24);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CharacterCreateRequestHandler for account {accountName} using version {client.Version}");
            }

            if (!accountName.StartsWith(client.Account.Name)) // TODO more correctly check, client send accountName as account-S, -N, -H (if it not fit in 20, then only account)
            {
                if (Properties.BAN_HACKERS)
                {
                    client.BanAccount($"Autoban wrong Account '{accountName}'");
                }

                client.Disconnect();
                return;
            }

            // Realm
            ERealm currentRealm = ERealm.None;
            if (accountName.EndsWith("-S"))
            {
                currentRealm = ERealm.Albion;
            }
            else if (accountName.EndsWith("-N"))
            {
                currentRealm = ERealm.Midgard;
            }
            else if (accountName.EndsWith("-H"))
            {
                currentRealm = ERealm.Hibernia;
            }

            // Client character count support
            int charsCount = 10;

            for (int i = 0; i < charsCount; i++)
            {
                var pakdata = new CreationCharacterData(packet, client);

                // Graveen: changed the following to allow GMs to have special chars in their names (_,-, etc..)
                var nameCheck = new Regex("^[A-Z][a-zA-Z]");
                if (!string.IsNullOrEmpty(pakdata.CharName) && (pakdata.CharName.Length < 3 || !nameCheck.IsMatch(pakdata.CharName)))
                {
                    if ((EPrivLevel)client.Account.PrivLevel == EPrivLevel.Player)
                    {
                        if (Properties.BAN_HACKERS)
                        {
                            client.BanAccount($"Autoban bad CharName '{pakdata.CharName}'");
                        }

                        client.Disconnect();
                        return;
                    }
                }

                switch ((eOperation) pakdata.Operation)
                {
                    case eOperation.Delete:
                        if (string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Deletion in 1.104+ check for removed character.
                            needRefresh |= CheckForDeletedCharacter(accountName, client, i);
                        }

                        break;
                    case eOperation.Customize:
                        if (!string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Candidate for Customizing ?
                            var character = client.Account.Characters?.FirstOrDefault(ch =>
                                ch.Name.Equals(pakdata.CharName, StringComparison.OrdinalIgnoreCase));
                            if (character != null)
                            {
                                needRefresh |= CheckCharacterForUpdates(pakdata, client, character);
                            }
                        }

                        break;
                    case eOperation.Create:
                        if (!string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Candidate for Creation ?
                            var character = client.Account.Characters?.FirstOrDefault(ch =>
                                ch.Name.Equals(pakdata.CharName, StringComparison.OrdinalIgnoreCase));
                            if (character == null)
                            {
                                needRefresh |= CreateCharacter(pakdata, client, i);
                            }
                        }

                        break;
                }
            }

            if (needRefresh)
            {
                client.Out.SendCharacterOverview(currentRealm);
            }
        }

        class CreationCharacterData
        {
            public string CharName { get; }

            public int CustomMode { get; }

            public int EyeSize { get; }

            public int LipSize { get; }

            public int EyeColor { get; }

            public int HairColor { get; }

            public int FaceType { get; }

            public int HairStyle { get; }

            public int MoodType { get; }

            public uint Operation { get; }

            public int Class { get; }

            public int Realm { get; }

            public int Race { get; }

            public int Gender { get; }

            public ushort CreationModel { get; }

            public int Region { get; }

            public int Strength { get; }

            public int Dexterity { get; }

            public int Constitution { get; }

            public int Quickness { get; }

            public int Intelligence { get; }

            public int Piety { get; }

            public int Empathy { get; }

            public int Charisma { get; }

            public int NewConstitution { get; }

            public int CharacterSlot { get; set; } // 1125 support

            public int CustomizeType { get; set; } // 1125 support

            /// <summary>
            /// Reads up ONE character iteration on the packet stream
            /// </summary>
            /// <param name="packet"></param>
            /// <param name="client"></param>
            public CreationCharacterData(GsPacketIn packet, GameClient client)
            {
                if (client.Version > GameClient.eClientVersion.Version1124) // 1125+ support
                {
                    CharacterSlot = packet.ReadByte();
                    CharName = packet.ReadIntPascalStringLowEndian();
                    packet.Skip(4); // 0x18 0x00 0x00 0x00                   
                    CustomMode = packet.ReadByte();
                    EyeSize = packet.ReadByte();
                    LipSize = packet.ReadByte();
                    EyeColor = packet.ReadByte();
                    HairColor = packet.ReadByte();
                    FaceType = packet.ReadByte();
                    HairStyle = packet.ReadByte();
                    packet.Skip(3);
                    MoodType = packet.ReadByte();
                    packet.Skip(9); // one extra byte skipped?
                    Operation = (uint)packet.ReadByte(); // probably low end int, but im just gonna read the first byte
                    CustomizeType = packet.ReadByte(); // 1 = face 2 = attributes 3 = both
                    packet.Skip(2); // last two bytes in the supposed int
                    // the following are now low endian int pascal strings
                    packet.Skip(5); //Location string
                    packet.Skip(5); //Skip class name
                    packet.Skip(5); //Skip race name

                    if (client.Version >= GameClient.eClientVersion.Version1126)
                    {
                        Class = packet.ReadByte();
                        Realm = packet.ReadByte();
                    }
                    else // 1125
                    {
                        packet.Skip(1); // level
                        Class = packet.ReadByte();
                        Realm = packet.ReadByte();
                    }
                    if (Realm > 0) // put inside this, as when a char is deleted, there is no realm sent TODO redo how account slot is stored in DB perhaps
                    {
                        CharacterSlot -= (Realm - 1) * 10; // calc to get character slot into same format used in database.
                    }
                    
                    byte startRaceGender1 = (byte)packet.ReadByte();                    
                    Race = startRaceGender1 & 0x1F;
                    Gender = ((startRaceGender1 >> 7) & 0x01);
                    //SIStartLocation = ((startRaceGender1 >> 7) != 0);
                    CreationModel = packet.ReadShortLowEndian();

                    if (client.Version == GameClient.eClientVersion.Version1125)
                    {
                        Region = packet.ReadByte();
                        packet.Skip(5);                       
                    }

                    Strength = packet.ReadByte();
                    Dexterity = packet.ReadByte();
                    Constitution = packet.ReadByte();
                    Quickness = packet.ReadByte();
                    Intelligence = packet.ReadByte();
                    Piety = packet.ReadByte();
                    Empathy = packet.ReadByte();
                    Charisma = packet.ReadByte();

                    packet.Skip(43);
                    
                    NewConstitution = packet.ReadByte();
                    // trailing 0x00
                    return;
                }
                // 1124
                // unk - probably indicates customize or create (these are moved from 1.99 4 added bytes)
                packet.ReadIntLowEndian();

                CharName = packet.ReadString(24);
                CustomMode = packet.ReadByte();
                EyeSize = packet.ReadByte();
                LipSize = packet.ReadByte();
                EyeColor = packet.ReadByte();
                HairColor = packet.ReadByte();
                FaceType = packet.ReadByte();
                HairStyle = packet.ReadByte();
                packet.Skip(3);
                MoodType = packet.ReadByte();
                packet.Skip(8);

                Operation = packet.ReadInt();
                packet.ReadByte();

                packet.Skip(24); // Location String
                packet.Skip(24); // Skip class name
                packet.Skip(24); // Skip race name

                packet.ReadByte(); // not safe! level
                Class = packet.ReadByte();
                Realm = packet.ReadByte();

                // The following byte contains
                // 1bit=start location ... in ShroudedIsles you can choose ...
                // 1bit=first race bit
                // 1bit=unknown
                // 1bit=gender (0=male, 1=female)
                // 4bit=race
                byte startRaceGender = (byte)packet.ReadByte();
                Race = (startRaceGender & 0x0F) + ((startRaceGender & 0x40) >> 2);
                Gender = (startRaceGender >> 4) & 0x01;

                CreationModel = packet.ReadShortLowEndian();
                Region = packet.ReadByte();
                packet.Skip(1); // TODO second byte of region unused currently
                packet.Skip(4); // TODO Unknown Int / last used?

                Strength = packet.ReadByte();
                Dexterity = packet.ReadByte();
                Constitution = packet.ReadByte();
                Quickness = packet.ReadByte();
                Intelligence = packet.ReadByte();
                Piety = packet.ReadByte();
                Empathy = packet.ReadByte();
                Charisma = packet.ReadByte();

                packet.Skip(40); // TODO equipment

                packet.ReadByte(); // 0x9C activeRightSlot
                packet.ReadByte(); // 0x9D activeLeftSlot
                packet.ReadByte(); // 0x9E siZone

                // New constitution must be read before skipping 4 bytes
                NewConstitution = packet.ReadByte(); // 0x9F
            }
        }

        private bool CreateCharacter(CreationCharacterData pdata, GameClient client, int accountSlot)
        {
            DbAccount account = client.Account;
            var ch = new DbCoreCharacter
            {
                AccountName = account.Name,
                Name = pdata.CharName
            };

            if (pdata.CustomMode == 0x01)
            {
                ch.EyeSize = (byte)pdata.EyeSize;
                ch.LipSize = (byte)pdata.LipSize;
                ch.EyeColor = (byte)pdata.EyeColor;
                ch.HairColor = (byte)pdata.HairColor;
                ch.FaceType = (byte)pdata.FaceType;
                ch.HairStyle = (byte)pdata.HairStyle;
                ch.MoodType = (byte)pdata.MoodType;
                ch.CustomisationStep = 2; // disable config button

                if (log.IsDebugEnabled)
                {
                    log.Debug("Disable Config Button");
                }
            }

            ch.Level = 1;

            // Set Realm and Class
            ch.Realm = pdata.Realm;
            ch.Class = pdata.Class;

            // Set Account Slot, Gender
            ch.AccountSlot = accountSlot + ch.Realm * 100;
            ch.Gender = pdata.Gender;

            // Set Race
            ch.Race = pdata.Race;

            ch.CreationModel = pdata.CreationModel;
            ch.CurrentModel = ch.CreationModel;
            ch.Region = pdata.Region;

            ch.Strength = pdata.Strength;
            ch.Dexterity = pdata.Dexterity;
            ch.Constitution = pdata.Constitution;
            ch.Quickness = pdata.Quickness;
            ch.Intelligence = pdata.Intelligence;
            ch.Piety = pdata.Piety;
            ch.Empathy = pdata.Empathy;
            ch.Charisma = pdata.Charisma;

            // defaults
            ch.CreationDate = DateTime.Now;

            ch.Endurance = 100;
            ch.MaxEndurance = 100;
            ch.Concentration = 100;
            ch.MaxSpeed = GamePlayer.PLAYER_BASE_SPEED;

            if (log.IsDebugEnabled)
            {
                log.Debug($"Creation {client.Version} character, class:{ch.Class}, realm:{ch.Realm}");
            }

            // Is class disabled ?
            List<string> disabledClasses = Util.SplitCSV(Properties.DISABLED_CLASSES);
            var occurences =
                (from j in disabledClasses
                    where j == ch.Class.ToString()
                    select j)
                .Count();

            if (occurences > 0 && (EPrivLevel)client.Account.PrivLevel == EPrivLevel.Player)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"Client {client.Account.Name} tried to create a disabled classe: {(EPlayerClass)ch.Class}");
                }

                return true;
            }

            // check if race disabled
            List<string> disabledRaces = Util.SplitCSV(Properties.DISABLED_RACES);
            occurences =
                (from j in disabledRaces
                    where j == ch.Race.ToString()
                    select j)
                .Count();

            if (occurences > 0 && (EPrivLevel)client.Account.PrivLevel == EPrivLevel.Player)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"Client {client.Account.Name} tried to create a disabled race: {(ERace)ch.Race}");
                }

                return true;
            }

            // If sending invalid Class ID
            if (!Enum.IsDefined(typeof(EPlayerClass), (EPlayerClass)ch.Class))
            {
                if (log.IsErrorEnabled)
                {
                    log.Error($"{client.Account.Name} tried to create a character with wrong class ID: {ch.Class}, realm:{ch.Realm}");
                }

                if (Properties.BAN_HACKERS)
                {
                    client.BanAccount($"Autoban character create class: id:{ch.Class} realm:{ch.Realm} name:{ch.Name} account:{account.Name}");
                    client.Disconnect();
                    return false;
                }

                return true;
            }

            // check if client tried to create invalid char
            if (!IsCharacterValid(ch))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{ch.AccountName} tried to create invalid character:\nchar name={ch.Name}, gender={ch.Gender}, race={ch.Race}, realm={ch.Realm}, class={ch.Class}, region={ch.Region}\nstr={ch.Strength}, con={ch.Constitution}, dex={ch.Dexterity}, qui={ch.Quickness}, int={ch.Intelligence}, pie={ch.Piety}, emp={ch.Empathy}, chr={ch.Charisma}");
                }

                return true;
            }

            // Save the character in the database
            GameServer.Database.AddObject(ch);

            // Fire the character creation event
            // This is Where Most Creation Script should take over to update any data they would like !
            GameEventMgr.Notify(DatabaseEvent.CharacterCreated, null, new CharacterEventArgs(ch, client));

            // write changes
            GameServer.Database.SaveObject(ch);

            // Log creation
            AuditMgr.AddAuditEntry(client, EAuditType.Account, EAuditSubType.CharacterCreate, string.Empty, pdata.CharName);

            client.Account.Characters = null;

            if (log.IsInfoEnabled)
            {
                log.Info($"Character {pdata.CharName} created on Account {account}!");
            }

            // Reload Account Relations
            GameServer.Database.FillObjectRelations(client.Account);

            return true;
        }

        /// <summary>
        /// Check if a Character Needs update based to packet data
        /// </summary>
        /// <param name="pdata">packet data</param>
        /// <param name="client">client</param>
        /// <param name="character">db character</param>
        /// <returns>True if character need refreshment false if no refresh needed.</returns>
        private bool CheckCharacterForUpdates(CreationCharacterData pdata, GameClient client, DbCoreCharacter character)
        {
            int newModel = character.CurrentModel;

            if (pdata.CustomMode == 1 || pdata.CustomMode == 2 || pdata.CustomMode == 3)
            {
                if (Properties.ALLOW_CUSTOMIZE_FACE_AFTER_CREATION)
                {
                    character.EyeSize = (byte)pdata.EyeSize;
                    character.LipSize = (byte)pdata.LipSize;
                    character.EyeColor = (byte)pdata.EyeColor;
                    character.HairColor = (byte)pdata.HairColor;
                    character.FaceType = (byte)pdata.FaceType;
                    character.HairStyle = (byte)pdata.HairStyle;
                    character.MoodType = (byte)pdata.MoodType;
                }

                if (pdata.CustomMode != 3)
                {
                    var stats = new Dictionary<EStat, int>
                    {
                        [EStat.STR] = pdata.Strength,
                        [EStat.DEX] = pdata.Dexterity,
                        [EStat.CON] = pdata.NewConstitution,
                        [EStat.QUI] = pdata.Quickness,
                        [EStat.INT] = pdata.Intelligence,
                        [EStat.PIE] = pdata.Piety,
                        [EStat.EMP] = pdata.Empathy,
                        [EStat.CHR] = pdata.Charisma
                    };

                    // check for changed stats.
                    bool flagChangedStats = false;
                    flagChangedStats |= stats[EStat.STR] != character.Strength;
                    flagChangedStats |= stats[EStat.CON] != character.Constitution;
                    flagChangedStats |= stats[EStat.DEX] != character.Dexterity;
                    flagChangedStats |= stats[EStat.QUI] != character.Quickness;
                    flagChangedStats |= stats[EStat.INT] != character.Intelligence;
                    flagChangedStats |= stats[EStat.PIE] != character.Piety;
                    flagChangedStats |= stats[EStat.EMP] != character.Empathy;
                    flagChangedStats |= stats[EStat.CHR] != character.Charisma;

                    if (flagChangedStats)
                    {
                        IPlayerClass charClass = ScriptMgr.FindCharacterClass(character.Class);

                        if (charClass != null)
                        {
                            bool valid = IsCustomPointsDistributionValid(character, stats, out var points);

                            // Hacking attemp ?
                            if (points > MaxStartingBonusPoints)
                            {
                                if ((EPrivLevel)client.Account.PrivLevel == EPrivLevel.Player)
                                {
                                    if (Properties.BAN_HACKERS)
                                    {
                                        client.BanAccount($"Autoban Hack char update : Wrong allowed points:{points}");
                                    }

                                    client.Disconnect();
                                    return false;
                                }
                            }

                            // Error in setting points
                            if (!valid)
                            {
                                return true;
                            }

                            if (Properties.ALLOW_CUSTOMIZE_STATS_AFTER_CREATION)
                            {
                                // Set Stats, valid is ok.
                                character.Strength = stats[EStat.STR];
                                character.Constitution = stats[EStat.CON];
                                character.Dexterity = stats[EStat.DEX];
                                character.Quickness = stats[EStat.QUI];
                                character.Intelligence = stats[EStat.INT];
                                character.Piety = stats[EStat.PIE];
                                character.Empathy = stats[EStat.EMP];
                                character.Charisma = stats[EStat.CHR];

                                if (log.IsInfoEnabled)
                                {
                                    log.Info($"Character {character.Name} Stats updated in cache!");
                                }

                                if (client.Player != null)
                                {
                                    foreach (var stat in stats.Keys)
                                    {
                                        client.Player.ChangeBaseStat(stat, (short)(stats[stat] - client.Player.GetBaseStat(stat)));
                                    }

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info($"Character {character.Name} Player Stats updated in cache!");
                                    }
                                }
                            }
                        }
                        else if (log.IsErrorEnabled)
                        {
                            log.Error($"No CharacterClass with ID {character.Class} found");
                        }
                    }
                }

                if (pdata.CustomMode == 2) // change player customization
                {
                    if (client.Account.PrivLevel == 1 && ((pdata.CreationModel >> 11) & 3) == 0)
                    {
                        if (Properties.BAN_HACKERS) // Player size must be > 0 (from 1 to 3)
                        {
                            client.BanAccount($"Autoban Hack char update : zero character size in model:{newModel}");
                            client.Disconnect();
                            return false;
                        }

                        return true;
                    }

                    character.CustomisationStep = 2; // disable config button

                    if (Properties.ALLOW_CUSTOMIZE_FACE_AFTER_CREATION)
                    {
                        if (pdata.CreationModel != character.CreationModel)
                        {
                            character.CurrentModel = newModel;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Character {character.Name} face properties configured by account {client.Account.Name}!");
                        }
                    }
                }
                else if (pdata.CustomMode == 3) // auto config -- seems someone thinks this is not possible?
                {
                    character.CustomisationStep = 3; // enable config button to player
                }

                // Save the character in the database
                GameServer.Database.SaveObject(character);
            }

            return false;
        }

        // 1125 support
        private bool CheckCharacterForUpdates1125(CreationCharacterData pdata, GameClient client, DbCoreCharacter character, int type)
        {
            int newModel = character.CurrentModel;

            if (pdata.CustomMode == 1 || pdata.CustomMode == 2 || pdata.CustomMode == 3)
            {
                bool flagChangedStats = false;

                if (type == 1 || type == 3) // face changes
                {
                    if (Properties.ALLOW_CUSTOMIZE_FACE_AFTER_CREATION || character.CustomisationStep == 3)
                    {
                        character.EyeSize = (byte)pdata.EyeSize;
                        character.LipSize = (byte)pdata.LipSize;
                        character.EyeColor = (byte)pdata.EyeColor;
                        character.HairColor = (byte)pdata.HairColor;
                        character.FaceType = (byte)pdata.FaceType;
                        character.HairStyle = (byte)pdata.HairStyle;
                        character.MoodType = (byte)pdata.MoodType;

                    }
                }
                if (type == 2 || type == 3) // attributes changes
                {
                    if (pdata.CustomMode != 3)//patch 0042 // TODO check out these different custommodes
                    {
                        var stats = new Dictionary<EStat, int>
                        {
                            [EStat.STR] = pdata.Strength, // Strength
                            [EStat.DEX] = pdata.Dexterity, // Dexterity
                            [EStat.CON] = pdata.NewConstitution, // New Constitution
                            [EStat.QUI] = pdata.Quickness, // Quickness
                            [EStat.INT] = pdata.Intelligence, // Intelligence
                            [EStat.PIE] = pdata.Piety, // Piety
                            [EStat.EMP] = pdata.Empathy, // Empathy
                            [EStat.CHR] = pdata.Charisma // Charisma
                        };

                        // check for changed stats.
                        flagChangedStats |= stats[EStat.STR] != character.Strength;
                        flagChangedStats |= stats[EStat.CON] != character.Constitution;
                        flagChangedStats |= stats[EStat.DEX] != character.Dexterity;
                        flagChangedStats |= stats[EStat.QUI] != character.Quickness;
                        flagChangedStats |= stats[EStat.INT] != character.Intelligence;
                        flagChangedStats |= stats[EStat.PIE] != character.Piety;
                        flagChangedStats |= stats[EStat.EMP] != character.Empathy;
                        flagChangedStats |= stats[EStat.CHR] != character.Charisma;

                        if (flagChangedStats)
                        {
                            IPlayerClass charClass = ScriptMgr.FindCharacterClass(character.Class);

                            if (charClass != null)
                            {
                                bool valid = IsCustomPointsDistributionValid(character, stats, out int points);

                                // Hacking attemp ?
                                if (points > MaxStartingBonusPoints)
                                {
                                    log.InfoFormat("Stats above MaxStartingBonusPoints for {0}", character.Name);
                                    if ((EPrivLevel)client.Account.PrivLevel == EPrivLevel.Player && character.Level == 1)
                                    {
                                        if (Properties.BAN_HACKERS)
                                        {
                                            client.BanAccount(string.Format("Autoban Hack char update : Wrong allowed points:{0}", points));
                                        }
                                        log.InfoFormat("Disconnecting {0} because the stats  are above expected", character.Name);
                                        client.Disconnect();
                                        return false;
                                    }
                                }

                                // Error in setting points
                                if (!valid)
                                {
                                    return true;
                                }

                                if (Properties.ALLOW_CUSTOMIZE_STATS_AFTER_CREATION || character.CustomisationStep == 3)
                                {
                                    // Set Stats, valid is ok.
                                    character.Strength = stats[EStat.STR];
                                    character.Constitution = stats[EStat.CON];
                                    character.Dexterity = stats[EStat.DEX];
                                    character.Quickness = stats[EStat.QUI];
                                    character.Intelligence = stats[EStat.INT];
                                    character.Piety = stats[EStat.PIE];
                                    character.Empathy = stats[EStat.EMP];
                                    character.Charisma = stats[EStat.CHR];

                                    if (log.IsInfoEnabled)
                                    {
                                        log.InfoFormat("Character {0} Stats updated in cache!", character.Name);
                                    }

                                    if (client.Player != null)
                                    {
                                        foreach (var stat in stats.Keys)
                                        {
                                            client.Player.ChangeBaseStat(stat, (short)(stats[stat] - client.Player.GetBaseStat(stat)));
                                        }

                                        if (log.IsInfoEnabled)
                                        {
                                            log.InfoFormat("Character {0} Player Stats updated in cache!", character.Name);
                                        }
                                    }
                                    character.CustomisationStep = 2;
                                }
                            }
                            else if (log.IsErrorEnabled)
                            {
                                log.ErrorFormat("No CharacterClass with ID {0} found", character.Class);
                            }
                        }
                    }
                }

                if (pdata.CustomMode == 2) // change player customization // is this changing starting race? im not sure TODO check
                {
                    if (client.Account.PrivLevel == 1 && ((pdata.CreationModel >> 11) & 3) == 0)
                    {
                        if (Properties.BAN_HACKERS) // Player size must be > 0 (from 1 to 3)
                        {
                            client.BanAccount(string.Format("Autoban Hack char update : zero character size in model:{0}", newModel));
                            client.Disconnect();
                            return false;
                        }
                        return true;
                    }

                    character.CustomisationStep = 2; // disable config button

                    if (Properties.ALLOW_CUSTOMIZE_FACE_AFTER_CREATION)
                    {
                        if (pdata.CreationModel != character.CreationModel)
                        {
                            character.CurrentModel = newModel;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.InfoFormat("Character {0} face properties configured by account {1}!", character.Name, client.Account.Name);
                        }
                    }
                }
                else if (pdata.CustomMode == 3) //auto config -- seems someone thinks this is not possible?
                {
                    character.CustomisationStep = 3; // enable config button to player
                }

                //Save the character in the database
                GameServer.Database.SaveObject(character);
            }

            return false;
        }

        public static bool CheckForDeletedCharacter(string accountName, GameClient client, int slot)
        {
            int charSlot = slot;
            if (client.Version > GameClient.eClientVersion.Version1124) // 1125 support
            {
                charSlot = client.Account.Realm * 100 + (slot - (client.Account.Realm - 1) * 10);
            }
            else // 1124
            {
                if (accountName.EndsWith("-S"))
                {
                    charSlot = 100 + slot;
                }
                else if (accountName.EndsWith("-N"))
                {
                    charSlot = 200 + slot;
                }
                else if (accountName.EndsWith("-H"))
                {
                    charSlot = 300 + slot;
                }
            }            

            DbCoreCharacter[] allChars = client.Account.Characters;

            if (allChars != null)
            {
                foreach (DbCoreCharacter character in allChars.ToArray())
                {
                    if (character.AccountSlot == charSlot && client.ClientState == GameClient.eClientState.CharScreen)
                    {
                        if (log.IsWarnEnabled)
                        {
                            log.Warn($"DB Character Delete:  Account {accountName}, Character: {character.Name}, slot position: {character.AccountSlot}, client slot {slot}");
                        }

                        if (allChars.Length < client.ActiveCharIndex && client.ActiveCharIndex > -1 && allChars[client.ActiveCharIndex] == character)
                        {
                            client.ActiveCharIndex = -1;
                        }

                        GameEventMgr.Notify(DatabaseEvent.CharacterDeleted, null, new CharacterEventArgs(character, client));

						if (Properties.BACKUP_DELETED_CHARACTERS)
						{
							var backupCharacter = new DbCoreCharacterBackup(character);
							Util.ForEach(backupCharacter.CustomParams, param => GameServer.Database.AddObject(param));
							GameServer.Database.AddObject(backupCharacter);

                            if (log.IsWarnEnabled)
                            {
                                log.Warn($"DB Character {character.ObjectId} backed up to DOLCharactersBackup and no associated content deleted.");
                            }
                        }
                        else
                        {
                            // delete associated data
                            try
                            {
                                var objs = GameServer.Database.SelectObjects<DbInventoryItem>(DB.Column("OwnerID").IsEqualTo(character.ObjectId));
                                GameServer.Database.DeleteObject(objs);
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                {
                                    log.Error($"Error deleting char items, char OID={character.ObjectId}, Exception:{e}");
                                }
                            }

                            // delete quests
                            try
                            {
                                var objs = GameServer.Database.SelectObjects<DbQuest>(DB.Column("Character_ID").IsEqualTo(character.ObjectId));
                                GameServer.Database.DeleteObject(objs);
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                {
                                    log.Error($"Error deleting char quests, char OID={character.ObjectId}, Exception:{e}");
                                }
                            }

                            // delete ML steps
                            try
                            {
                                var objs = GameServer.Database.SelectObjects<DbCharacterXMasterLevel>(DB.Column("Character_ID").IsEqualTo(character.ObjectId));
                                GameServer.Database.DeleteObject(objs);
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                {
                                    log.Error($"Error deleting char ml steps, char OID={character.ObjectId}, Exception:{e}");
                                }
                            }
                        }

                        string deletedChar = character.Name;

                        GameServer.Database.DeleteObject(character);
                        client.Account.Characters = null;
                        client.Player = null;
                        GameServer.Database.FillObjectRelations(client.Account);

                        if (client.Account.Characters == null || client.Account.Characters.Length == 0)
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info($"Account {client.Account.Name} has no more chars. Realm reset!");
                            }

                            // Client has no more characters, so the client can choose the realm again!
                            client.Account.Realm = 0;
                        }

                        GameServer.Database.SaveObject(client.Account);

                        // Log deletion
                        AuditMgr.AddAuditEntry(client, EAuditType.Character, EAuditSubType.CharacterDelete, string.Empty, deletedChar);

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if Custom Creation Points Distribution is Valid.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="stats"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static bool IsCustomPointsDistributionValid(DbCoreCharacter character, IDictionary<EStat, int> stats, out int points)
        {
            IPlayerClass charClass = ScriptMgr.FindCharacterClass(character.Class);

            if (charClass != null)
            {
                points = 0;

                // check if each stat is valid.
                foreach (var stat in stats.Keys)
                {
                    int raceAmount = GlobalConstants.STARTING_STATS_DICT[(ERace)character.Race][stat];

                    int classAmount = 0;

                    for (int level = character.Level; level > 5; level--)
                    {
                        if (charClass.PrimaryStat != EStat.UNDEFINED && charClass.PrimaryStat == stat)
                        {
                            classAmount++;
                        }

                        if (charClass.SecondaryStat != EStat.UNDEFINED && charClass.SecondaryStat == stat && (level - 6) % 2 == 0)
                        {
                            classAmount++;
                        }

                        if (charClass.TertiaryStat != EStat.UNDEFINED && charClass.TertiaryStat == stat && (level - 6) % 3 == 0)
                        {
                            classAmount++;
                        }
                    }

                    int above = stats[stat] - raceAmount - classAmount;

                    // Miss Some points...
                    if (above < 0 && character.Level == 1)
                    {
                        return false;
                    }

                    points += above;
                    points += Math.Max(0, above - 10); // two points used
                    points += Math.Max(0, above - 15); // three points used
                }

                var validPoints = points == MaxStartingBonusPoints;

                if (character.Level > 1)
                    return true;

                return validPoints;
            }

            points = -1;
            return false;
        }

        /// <summary>
        /// Verify whether created character is valid
        /// </summary>
        /// <param name="ch">The character to check</param>
        /// <returns>True if valid</returns>
        public static bool IsCharacterValid(DbCoreCharacter ch)
        {
            bool valid = true;
            try
            {
                if ((ERealm)ch.Realm < ERealm._FirstPlayerRealm || (ERealm)ch.Realm > ERealm._LastPlayerRealm)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong realm: {ch.Realm} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                if (ch.Level != 1)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong level: {ch.Level} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                if (!GlobalConstants.STARTING_CLASSES_DICT.ContainsKey((ERealm)ch.Realm) || !GlobalConstants.STARTING_CLASSES_DICT[(ERealm)ch.Realm].Contains((EPlayerClass)ch.Class))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong class: {ch.Class}, realm:{ch.Realm} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }
                
                IPlayerClass charClass = ScriptMgr.FindCharacterClass(ch.Class);

				if(!charClass.EligibleRaces.Exists(s => (int)s.ID == ch.Race))
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("Wrong race: {0}, class:{1} on character creation from Account: {2}", ch.Race, ch.Class, ch.AccountName);
					valid = false;
				}
                
				// int pointsUsed;
				var stats = new Dictionary<EStat, int>{{EStat.STR, ch.Strength},{EStat.CON, ch.Constitution},{EStat.DEX, ch.Dexterity},{EStat.QUI, ch.Quickness},
					{EStat.INT, ch.Intelligence},{EStat.PIE, ch.Piety},{EStat.EMP, ch.Empathy},{EStat.CHR, ch.Charisma},};
    
                valid &= IsCustomPointsDistributionValid(ch, stats, out var pointsUsed);

                if (pointsUsed != MaxStartingBonusPoints)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Points used: {pointsUsed} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                EGender gender = ch.Gender == 0 ? EGender.Male : EGender.Female;

                if (GlobalConstants.RACE_GENDER_CONSTRAINTS_DICT.ContainsKey((ERace)ch.Race) && GlobalConstants.RACE_GENDER_CONSTRAINTS_DICT[(ERace)ch.Race] != gender)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong Race gender: {ch.Gender}, race: {ch.Race} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                if (GlobalConstants.CLASS_GENDER_CONSTRAINTS_DICT.ContainsKey((EPlayerClass)ch.Class) && GlobalConstants.CLASS_GENDER_CONSTRAINTS_DICT[(EPlayerClass)ch.Class] != gender)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong class gender: {ch.Gender}, class:{ch.Class} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error($"CharacterCreation error on account {ch.AccountName}, slot {ch.AccountSlot}. Exception:{e}");
                }

                valid = false;
            }

            return valid;
        }
    }
}