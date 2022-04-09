/*
 *
 * ATLAS - Hardcore
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using log4net;
using log4net.Repository.Hierarchy;

#region LoginEvent
namespace DOL.GS.GameEvents
{
    public class HardCoreLogin
    {
        
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        [GameServerStartedEvent]
        public static void OnServerStart(DOLEvent e, object sender, EventArgs arguments)
        {
            GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(HCPlayerEntered));
        }

        /// <summary>
        /// Event handler fired when server is stopped
        /// </summary>
        [GameServerStoppedEvent]
        public static void OnServerStop(DOLEvent e, object sender, EventArgs arguments)
        {
            GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(HCPlayerEntered));
        }
        
        /// <summary>
        /// Event handler fired when players enters the game
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private static void HCPlayerEntered(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer player = sender as GamePlayer;
            if (player == null) return;
            if (!player.HCFlag) return;
            
            if (player.DeathCount > 0 && player.HCFlag)
            {
                DOLCharacters cha = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(player.Name));
                if (cha != null)
                {
                    Log.Warn("[HARDCORE] player " + player.Name + " has " + player.DeathCount + " deaths and has been removed from the database.");
                    GameServer.Database.DeleteObject(cha);
                    player.Client.Out.SendPlayerQuit(true);
                }
            }
    
        }
    }
}
#endregion
#region command
namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&hardcore",
        ePrivLevel.Player,
        "Flags a player as Hardcore. Dying after activating Hardcore will result in the character deletion.",
        "/hardcore on")]
    public class HardcoreCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "hardcore"))
                return;

            if (client.Player.RealmPoints > 0)
                return;
            
            if (client.Player.HCFlag){
                client.Out.SendMessage("Your Hardcore flag is ON! Death will result in the character deletion.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].ToLower().Equals("on"))
            {
                if (client.Player.Level != 1)
                {
                    client.Out.SendMessage("You must be level 1 to activate Hardcore.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return;
                }
                client.Out.SendCustomDialog("Do you really want to activate the Hardcore flag? Death will be permanent.", new CustomDialogResponse(HardcoreResponseHandler));
            }
        }
        
        protected virtual void HardcoreResponseHandler(GamePlayer player, byte response)
        {
            if (response == 1)
            {
                if (player.Level > 1)
                {
                    player.Out.SendMessage("You must be level 1 to activate Hardcore.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return;
                }
                
                player.Emote(eEmote.StagFrenzy);
                player.HCFlag = true;
                player.Out.SendMessage("Your HARDCORE flag is ON. Your character will be deleted at death.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                if (player.NoHelp)
                {
                    player.CurrentTitle = new HardCoreSoloTitle();
                }
                else
                {
                    player.CurrentTitle = new HardCoreTitle();
                }
                
            }
            else
            {
                player.Out.SendMessage("Use the command again if you change your mind.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }
        }
    }
    
    [CmdAttribute(
        "&hcladder",
        ePrivLevel.Player,
        "Displays the Hardcore Ladder.",
        "/hcladder")]
    public class HardcoreLadderCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public class HCCharacter : IComparable<HCCharacter>
        {
            public string CharacterName { get; set; }
        
            public int CharacterLevel { get; set; }
        
            public string CharacterClass { get; set; }
            
            public bool isSolo { get; set; }
            
            public override string ToString()
            {
                if (isSolo)
                    return string.Format("{0} the level {1} {2} <solo>", CharacterName, CharacterLevel, CharacterClass);
                
                return string.Format("{0} the level {1} {2}", CharacterName, CharacterLevel, CharacterClass);
                
            }

            public int CompareTo(HCCharacter compareLevel)
            {
                // A null value means that this object is greater.
                if (compareLevel == null)
                    return 1;
                
                return CharacterLevel.CompareTo(compareLevel.CharacterLevel);
            }
        }
        
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "hardcoreladder"))
                return;
            
            IList<string> textList = GetHardcoreLadder();
            client.Out.SendCustomTextWindow("Hardcore Ladder", textList);
            return;

            IList<string> GetHardcoreLadder()
            
        {
            IList<string> output = new List<string>();
            IList<HCCharacter> hcCharacters = new List<HCCharacter>();
            IList<DOLCharacters> characters = GameServer.Database.SelectObjects<DOLCharacters>("HCFlag = '1'").OrderByDescending(x => x.Level).Take(50).ToList();
            
            output.Add("Top 50 Hardcore characters:\n");
            
            foreach (DOLCharacters c in characters)
            {
                if (c == null)
                    continue;

                string className = ((eCharacterClass)c.Class).ToString();
                bool isSolo = false;
                
                const string customKey = "grouped_char";
                var hasGrouped = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(c.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));

                if (hasGrouped == null || c.NoHelp)
                {
                    isSolo = true;
                }
                
                hcCharacters.Add(new HCCharacter() {CharacterName = c.Name, CharacterLevel = c.Level, CharacterClass = className, isSolo = isSolo});

            }

            int position = 0;
            
            foreach (HCCharacter hcCharacter in hcCharacters)
            {
                position++;
                output.Add(position + ". " + hcCharacter);
            }

            return output;
        }

        }
        
    }
    
    
}
#endregion
#region title
namespace DOL.GS.PlayerTitles
{
    public class HardCoreTitle : SimplePlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Hardcore";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Hardcore";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Hardcore title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return player.HCFlag || player.HCCompleted;
        }
    }
    
    public class HardCoreSoloTitle : SimplePlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Hardcore Solo Beetle";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Hardcore Solo Beetle";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Hardcore Solo Beetle title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            const string customKey2 = "solo_to_50";
            var solo_to_50 = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey2)));
            
            return (player.HCFlag || player.HCCompleted) && (player.NoHelp || solo_to_50 != null);
        }
    }
}
#endregion