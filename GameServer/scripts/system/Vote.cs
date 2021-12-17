//Author : Unknown
using System;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using DOL.Events;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.AI.Brain;

using DOL.GS;
using DOL.GS.Scripts;
using DOL.GS.PacketHandler;

using log4net;

/*Example for making/creating/stopping a voting session.
 * /gmvote create - /gmvote add 1vs1 (first choice will be 1vs1) /gmvote add 2vs2 (second choice will ve 2vs2) etc.
 * /gmvote cancel - ends the voting session with ease
 * player command help:
 * Using the example above: /vote 1 for 1vs1, /vote 2 for 2vs2
 */

#region database table and object Voting

namespace DOL.Database
{
    [DataTable(TableName = "Voting")]
    public class DBVoting : DataObject
    {
        private string m_VoteID;
        private string[] m_Options = new string[0];
        private string m_Description = string.Empty;

        [PrimaryKey]
        public string VoteID
        {
            get { return m_VoteID; }
            set
            {
                Dirty = true;
                m_VoteID = value;
            }
        }

        [DataElement(AllowDbNull = true)]
        public string OptionStr
        {
            get { return string.Join(";", Options); }
            set
            {
                if (value == null || value.Length == 0)
                    m_Options = new string[0];
                else
                    m_Options = value.Split(';');
            }
        }

        public string[] Options
        {
            get { return m_Options; }
            set
            {
                Dirty = true;
                m_Options = (value == null || value.Length == 0) ? new string[0] : value;
            }
        }

        public bool AddOption(string choice)
        {
            if (choice != null && choice.Trim().Length == 0)
                return false;

            string[] array = new string[m_Options.Length + 1];
            m_Options.CopyTo(array, 0);
            array[m_Options.Length] = choice;
            m_Options = array;
            Dirty = true;

            return true;
        }

        [DataElement(AllowDbNull = true)]
        public string Description
        {
            get { return m_Description; }
            set
            {
                Dirty = true;
                m_Description = value;
            }
        }

        public void AddDescription(string line)
        {
            if (Description.Length > 0)
                Description = Description + "\n" + line;
            else
                Description = line;
        }

        public bool AutoSave
        {
            get { return false; }
            set { }
        }

    }
}

#endregion

#region PS Voting Manager

namespace DOL.GS.Scripts
{
    public class VotingMgr
    {
        #region consts, members, properties

        private static readonly uint STD_VOTING_DURATION = 86400; //24h
        private static Timer m_Timer = null;
        private static uint m_Dura = 0;

        public static readonly string PLY_TEMP_PROP_KEY = "Player.TempProp.Voting.Key";
        public static readonly string GM_TEMP_PROP_KEY = "GM.TempProp.Voting.Key";

        private static DBVoting m_Current = null;
        public static DBVoting CurrentVotingInProgress
        {
            get { return m_Current; }
            //set { m_Current = value; }
        }
        private static string m_Result = string.Empty;
        public static string LastVotingResult
        {
            get { return m_Result; }
            //set { m_Result = value; }
        }
        public static bool IsVotingInProgress
        {
            get { return CurrentVotingInProgress != null; }
        }

        #endregion

        public static void BeginVoting(GamePlayer aGM, DBVoting aVoting)
        {
            m_Current = aVoting;
            m_Dura = STD_VOTING_DURATION;
            string msg1 = "Voting in progress... type /vote";
            string msg2 = aGM.Name + " starts a new voting for " + m_Dura + "sec ... Use /vote";
            foreach (GameClient client in WorldMgr.GetAllPlayingClients())
            {
                client.Player.TempProperties.removeProperty(PLY_TEMP_PROP_KEY);
                client.Out.SendMessage(msg1, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(msg2, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                client.Out.SendPlaySound(eSoundType.Craft, 0x04);
            }
            //NewsMgr.CreateNews(aGM.Name+" starts a new voting!", (byte)eRealm.None, eNewsType.RvRGlobal, false);
            if (m_Timer != null)
                m_Timer.Dispose();
            m_Timer = new Timer(new TimerCallback(OnTimer), m_Timer, 1000, 1000);

        }

        public static void CancelVoting(GamePlayer aGM)
        {
            if (m_Timer != null)
                m_Timer.Dispose();
            m_Timer = null;
            m_Current = null;
            string msg = aGM.Name + " cancels the voting!";
            foreach (GameClient client in WorldMgr.GetAllPlayingClients())
            {
                client.Player.TempProperties.removeProperty(PLY_TEMP_PROP_KEY);
                client.Out.SendMessage(msg, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(msg, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                client.Out.SendPlaySound(eSoundType.Craft, 0x02);
            }
            //NewsMgr.CreateNews(msg, (byte)eRealm.None, eNewsType.RvRGlobal, false);
        }

        protected static void OnTimer(object state)
        {
            if (m_Dura <= 0)
            {
                EndVoting();
                return;
            }
            else
                --m_Dura;

            if (m_Dura == 60 || m_Dura == 30 || m_Dura == 10 || m_Dura == 5)
            {
                string msg = "Voting ends in " + m_Dura + "sec...";
                foreach (GameClient client in WorldMgr.GetAllPlayingClients())
                {
                    client.Out.SendMessage(msg, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
                    client.Out.SendMessage(msg, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                }
            }
        }

        protected static void EndVoting()
        {
            if (m_Timer != null)
                m_Timer.Dispose();
            m_Timer = null;
            DBVoting voting = m_Current;
            m_Current = null;

            string msg = "Voting ended!";

            int listStart = 1;
            ArrayList filters = null;
            ArrayList clients = new ArrayList();

            // get list of clients depending on server type
            foreach (GameClient serverClient in WorldMgr.GetAllPlayingClients())
                lock (clients)
            {
                // counting the votes for each option
                uint count = 0;
                ArrayList votes = new ArrayList();
                for (int i = 0; i < voting.Options.Length; i++)
                    votes.Add(new KVP(voting.Options[i], 0));

                foreach (GameClient client in clients)
                {
                    client.Out.SendMessage(msg, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
                    client.Out.SendMessage(msg, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                    client.Out.SendPlaySound(eSoundType.Craft, 0x04);

                    int vote = client.Player.TempProperties.getProperty(PLY_TEMP_PROP_KEY, -1);
                    client.Player.TempProperties.removeProperty(PLY_TEMP_PROP_KEY);
                    if (vote >= 0 && vote < voting.Options.Length)
                    {
                        ((KVP)votes[vote]).Value++;
                        ++count;
                    }
                }

                // generating result
                StringBuilder msg1 = new StringBuilder(); // for PrivLevel < GM
                StringBuilder msg2 = new StringBuilder(); // for PrivLevel >= GM
                string tmp = string.Format(
                    "Altogether {0} of {1} players voted ({2}%).\n",
                    count, clients.Count, PDivide(count, clients.Count));
                msg1.Append(tmp);
                msg2.Append("VoteID: ").Append(voting.VoteID).Append("\n").Append(tmp);
                if (voting.Description != string.Empty)
                    msg2.Append("\n").Append(voting.Description).Append("\n");

                votes.Sort(new SortByKVP());
                votes.Reverse();
                foreach (KVP kvp in votes)
                {
                    tmp = string.Format(
                        "- {0}: {1} ({2}%)\n",
                        kvp.Key, kvp.Value, PDivide(kvp.Value, clients.Count));
                    msg1.Append(tmp);
                    msg2.Append(tmp);
                }
                tmp = string.Format(
                    "{0} players did not vote ({1}%).\n",
                    (clients.Count - count), PDivide(clients.Count - count, clients.Count));
                msg1.Append(tmp);
                msg2.Append(tmp);
                string str1 = msg1.ToString();
                string str2 = msg2.ToString();
                string[] array1 = str1.Split('\n');
                string[] array2 = str2.Split('\n');


                foreach (GameClient client in clients)
                {
                    if (client.Account.PrivLevel <= (uint)ePrivLevel.GM)
                    {
                        client.Out.SendCustomTextWindow("Voting", array1);
                        client.Out.SendMessage(str1, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        client.Out.SendCustomTextWindow("Voting", array2);
                        client.Out.SendMessage(str2, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                    }
                }
                m_Result = str2;
            }
        }

        public static void ShowVoting(GamePlayer player, DBVoting voting)
        {
            if (voting == null || player == null) return;
            StringBuilder sb = new StringBuilder();
            if (player.Client.Account.PrivLevel >= (uint)ePrivLevel.GM)
                sb.Append("VoteID: ").Append(voting.VoteID).Append("\n");
            if (voting.Description != string.Empty)
                sb.Append("\n").Append(voting.Description).Append("\n");
            sb.Append("\nOptions:\n");
            int i = 0;
            foreach (string option in voting.Options)
                sb.Append("- ").Append(++i).Append(": ").Append(option).Append("\n");
            if (voting == CurrentVotingInProgress)
            {
                int vote = player.TempProperties.getProperty(PLY_TEMP_PROP_KEY, -1);
                if (vote >= 0 && vote < voting.Options.Length)
                    sb.Append("\nYou vote for: ").Append(voting.Options[vote]).Append("\n");
                else
                    sb.Append("\nYou did not vote yet!\nUse /vote 1 | 2 | ... x to vote for an option.\n");
                sb.Append("\nVoting ends in " + m_Dura + " seconds...");
            }
            player.Out.SendCustomTextWindow("Voting", sb.ToString().Split('\n'));
        }

        #region utility functions / classes

        public static int Divide(int dividend, int divisor)
        {
            if (divisor == 0)
                return dividend;
            else if (dividend == 0)
                return 0;
            else
                return (dividend / divisor);
        }

        public static string DDivide(double dividend, double divisor)
        {
            double D;
            if (divisor == 0D)
                D = dividend;
            else if (dividend == 0D)
                D = 0D;
            else
                D = (dividend / divisor);
            return D.ToString("0.0");
        }

        public static string PDivide(float dividend, float divisor)
        {
            double D;
            if (divisor == 0D)
                D = dividend;
            else if (dividend == 0D)
                D = 0D;
            else
                D = (dividend / divisor * 100);
            return D.ToString("0.0");
        }

        public static float ValuePerHour(int value, TimeSpan time)
        {
            if (value == 0)
                return 0f;

            float days = (float)time.Days;
            float hours = (float)time.Hours;
            float minutes = (float)time.Minutes;
            float seconds = (float)time.Seconds;

            return (float)value / (days * 24 + hours + minutes / 60 + seconds / (60 * 60));
        }

        protected class KVP
        {
            public string Key = string.Empty;
            public uint Value = 0;
            public KVP(string key, uint value)
            {
                Key = key;
                Value = value;
            }
        }

        // Sort my ArrayList
        protected class SortByKVP : IComparer
        {
            public int Compare(object x, object y)
            {
                KVP kvpX = (KVP)x;
                KVP kvpY = (KVP)y;
                return kvpX.Value.CompareTo(kvpY.Value);
            }
        }

        #endregion

    }

}

#endregion

#region register/handling of events and callbacks

namespace DOL.GS.GameEvents
{
    public class VotingEvents
    {
        [ScriptLoadedEvent]
        public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {

        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
        }
    }
}

#endregion

#region player command /vote

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&vote",
        ePrivLevel.Player,
        "Displays the current poll or let you vote",
        "/vote  shows you the current voting",
        "/vote 1|2|...|x  vote for your choice with the given number")]
    public class VoteCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;
            if (!VotingMgr.IsVotingInProgress)
            {
                DisplaySyntax(client);
                player.Out.SendMessage("There is no voting in progress.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (args.Length > 1)
                try
                {

                    int vote = int.Parse(args[1]) - 1;
                    player.Out.SendMessage("You vote for '" + VotingMgr.CurrentVotingInProgress.Options[vote] + "'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.TempProperties.setProperty(VotingMgr.PLY_TEMP_PROP_KEY, vote);
                }
                catch (Exception)
                {
                    DisplaySyntax(client);
                    player.Out.SendMessage("Choose a valid option!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
            VotingMgr.ShowVoting(player, VotingMgr.CurrentVotingInProgress);
            return;
        }
    }
}

#endregion

#region gm-command /gmvote

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&gmvote",
       ePrivLevel.GM,
        "Various voting commands!",
        "/gmvote create ... creates a new empty voting",
        "/gmvote add choice 1 ... adds 'choice 1' to the newly created voting",
        "/gmvote desc use this several times ... adds the textline 'use this several times' to the newly created voting",

        "/gmvote start ... starts the newly created voting",
        "/gmvote start NAME ... loads and starts the voting saved with NAME",
        "/gmvote cancel ... cancels the voting currently in progress",
        "/gmvote last ... shows you the statistics/result of the last voting",
        "/gmvote list ... shows you a list of all saved votings",
        "/gmvote list NAME ... shows you a list of saved votings with NAME as a part in their name",
        "/gmvote info ... shows you the details of the newly created voting",
        "/gmvote info NAME ... loads the voting given by name and shows you the details",
        "/gmvote remove ... resets/clears the newly created voting (u have to use '/gmvote create' to make new)")]
    public class GMVoteCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length == 1)
            {
                DisplaySyntax(client);
                return;
            }
            GamePlayer player = client.Player;
            string command = args[1].ToLower();
            string param = (args.Length > 2) ? String.Join(" ", args, 2, args.Length - 2) : string.Empty;

            if (VotingMgr.IsVotingInProgress && command != "cancel")
            {
                player.Out.SendMessage("A voting is in progress. You cannot use any commands except /gmvote cancel!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            switch (command)
            {
                #region /gmvote create
                case "create":
                    {
                        player.TempProperties.setProperty(VotingMgr.GM_TEMP_PROP_KEY, new DBVoting());
                        player.Out.SendMessage("You created an empty voting. Please customize it.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    break;
                #endregion
                #region /gmvote add
                case "add":
                    {
                        DBVoting voting = (DBVoting)player.TempProperties.getProperty<object>(VotingMgr.GM_TEMP_PROP_KEY, null);
                        if (voting == null)
                        {
                            player.Out.SendMessage("You didnt created an empty voting. Please use /gmvote create before!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        voting.AddOption(param);
                        player.Out.SendMessage("You added the choice: '" + param + "'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    break;
                #endregion
                #region /gmvote desc
                case "desc":
                    {
                        DBVoting voting = (DBVoting)player.TempProperties.getProperty<object>(VotingMgr.GM_TEMP_PROP_KEY, null);
                        if (voting == null)
                        {
                            player.Out.SendMessage("You didnt created an empty voting. Please use /gmvote create before!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        voting.AddDescription(param);
                        player.Out.SendMessage("You added to the description: '" + param + "'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    break;
                #endregion

                #region /gmvote start
                case "start":
                    {
                        DBVoting voting;
                        if (param != string.Empty)
                            voting = (DBVoting)GameServer.Database.FindObjectByKey<DBVoting> (GameServer.Database.Escape(param));
                        else
                            voting = (DBVoting)player.TempProperties.getProperty<object>(VotingMgr.GM_TEMP_PROP_KEY, null);

                        if (voting == null)
                        {
                            player.Out.SendMessage("You didnt specify any voting. Please create one first or give me a name!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        VotingMgr.BeginVoting(player, voting);
                        player.Out.SendMessage("You started the voting: '" + param + "'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    break;
                #endregion
                #region /gmvote cancel
                case "cancel":
                    {
                        if (!VotingMgr.IsVotingInProgress)
                        {
                            player.Out.SendMessage("There is no voting in progress. What you want to cancel?!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        VotingMgr.CancelVoting(player);
                        player.Out.SendMessage("You canceled the voting!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    break;
                #endregion
                #region /gmvote last
                case "last":
                    {
                        string[] text;
                        if (VotingMgr.LastVotingResult == string.Empty)
                            text = "Couldnt find last voting result.\nPossible none voting since last server start?".Split('\n');
                        else
                            text = VotingMgr.LastVotingResult.Split('\n');
                        player.Out.SendCustomTextWindow("voting result", text);
                    }
                    break;
                #endregion
                #region /gmvote list
                case "list":
                    {
                        DBVoting[] votings;
                        if (param != string.Empty)
                            votings = (DBVoting[])GameServer.Database.SelectObjects<DBVoting> ("VoteID LIKE '%" + GameServer.Database.Escape(param) + "%'");
                        else
                            votings = (DBVoting[])GameServer.Database.SelectAllObjects<DBVoting>();

                        if (votings == null || votings.Length == 0)
                        {
                            player.Out.SendMessage("No saved votings found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        else
                            player.Out.SendMessage("Found " + votings.Length + " votings.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        foreach (DBVoting voting in votings)
                            player.Out.SendMessage("Voting: '" + voting.VoteID + "'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    break;
                #endregion
                #region /gmvote info
                case "info":
                    {
                        DBVoting voting;
                        if (param != string.Empty)
                            voting = (DBVoting)GameServer.Database.FindObjectByKey<DBVoting> (GameServer.Database.Escape(param));
                        else
                            voting = (DBVoting)player.TempProperties.getProperty<object>(VotingMgr.GM_TEMP_PROP_KEY, null);

                        if (voting == null)
                        {
                            player.Out.SendMessage("You didnt specify any voting. Please create one first or give me a name!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        VotingMgr.ShowVoting(player, voting);
                        player.Out.SendMessage("You looks into the details of the voting: '" + param + "'.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    break;
                #endregion

                default: DisplaySyntax(client); break;
            }
            return;
        }
    }
}

#endregion
