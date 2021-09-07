/*
* Shutdown.cs: Originally coded by DOL Team (Etaew)
* Updated By: Krusck on 3/13/2011
* */
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;
using DOL.Database;

namespace DOL.GS.Commands
{
    [CmdAttribute(
       "&shutdown",
       ePrivLevel.Admin,
       "Shutdown the server in next minute",
       "/shutdown on <hour>:<min>  - shutdown on this time",
       "/shutdown <secs>  - shutdown in seconds")]
    public class ShutdownCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static long m_counter = 0;
        private static Timer m_timer;
        private static int m_time = 5;

        public static long GetShutdownCounter()
        {
            return m_counter;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            DOL.Events.GameEventMgr.AddHandler(GameServerEvent.WorldSave, new DOLEventHandler(AutomaticShutdown));
        }

        public static void AutomaticShutdown(DOLEvent e, object sender, EventArgs args)
        {
            if (m_timer != null)
                return;

            TimeSpan uptime = TimeSpan.FromTicks(GameServer.Instance.TickCount);
        }

           

        public static void CountDown(object param)
        {
            if (m_counter <= 0)
            {
                m_timer.Dispose();
                new Thread(new ThreadStart(ShutDownServer)).Start();
                return;
            }
            else
            {
                log.Info("Server reboot in " + m_counter + " seconds!");
                long secs = m_counter;
                long mins = secs / 60;
                long hours = mins / 60;

                foreach (GameClient client in WorldMgr.GetAllPlayingClients())
                {
                    if (hours > 3) //hours...
                    {
                        if (mins % 60 == 0 && secs % 60 == 0) //every hour..
                            client.Out.SendMessage("Server reboot in " + hours + " hours!", eChatType.CT_Broadcast,
                                              eChatLoc.CL_ChatWindow);
                    }
                    else if (hours > 0) //hours
                    {
                        if (mins % 30 == 0 && secs % 60 == 0) //every 30 mins..
                            client.Out.SendMessage("Server reboot in " + hours + " hours and " + (mins - (hours * 60)) + "mins!", eChatType.CT_Staff,
                                              eChatLoc.CL_ChatWindow);
                    }
                    else if (mins >= 10)
                    {
                        if (mins % 15 == 0 && secs % 60 == 0) //every 15 mins..
                            client.Out.SendMessage("Server reboot in " + mins + " mins!", eChatType.CT_Staff,
                                              eChatLoc.CL_ChatWindow);
                    }
                    else if (mins >= 3)
                    {
                        if (secs % 60 == 0) //every min...
                            client.Out.SendMessage("Server reboot in " + mins + " mins!", eChatType.CT_Staff,
                                              eChatLoc.CL_ChatWindow);
                    }
                    else if (secs >= 30)
                    {
                        if (secs % 10 == 0) //every 10
                            client.Out.SendMessage("Server reboot in " + secs + " seconds!", eChatType.CT_Staff,
                                eChatLoc.CL_ChatWindow);
                    }
                    else
                        client.Out.SendMessage("Server reboot in " + secs + " seconds! Please logout!", eChatType.CT_Staff,
                                             eChatLoc.CL_ChatWindow);
                }

                if (mins <= 5 && GameServer.Instance.ServerStatus != eGameServerStatus.GSS_Closed) // 5 mins remaining
                {
                    GameServer.Instance.Close();
                    string msg = "Server is now closed (reboot in " + mins + " mins)";
                   
                }
            }
            m_counter -= 5;
        }

        public static void ShutDownServer()
        {
            if (GameServer.Instance.IsRunning)
            {
                GameServer.Instance.Stop();
                log.Info("Automated server shutdown!");
                Thread.Sleep(2000);
                Environment.Exit(0);
            }
        }

        public void OnCommand(GameClient client, string[] args)
        {
            DateTime date;
            //if (m_counter > 0) return 0;
            if (args.Length >= 2)
            {
                if (args.Length == 2)
                {
                    try
                    {
                        m_counter = System.Convert.ToInt32(args[1]);
                    }
                    catch (Exception)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                }
               
                else
                {
                    if ((args.Length == 3) && (args[1] == "on"))
                    {
                        string[] shutdownsplit = args[2].Split(':');

                        if ((shutdownsplit == null) || (shutdownsplit.Length < 2))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        int hour = Convert.ToInt32(shutdownsplit[0]);
                        int min = Convert.ToInt32(shutdownsplit[1]);
                        // found next date with hour:min

                        date = DateTime.Now;

                        if ((date.Hour > hour) ||
                            (date.Hour == hour && date.Minute > min)
                           )
                            date = new DateTime(date.Year, date.Month, date.Day + 1);

                        if (date.Minute > min)
                            date = new DateTime(date.Year, date.Month, date.Day, date.Hour + 1, 0, 0);

                        date = date.AddHours(hour - date.Hour);
                        date = date.AddMinutes(min - date.Minute + 2);
                        date = date.AddSeconds(-date.Second);

                        m_counter = (date.ToFileTime() - DateTime.Now.ToFileTime()) / TimeSpan.TicksPerSecond;

                        if (m_counter < 60) m_counter = 60;

                    }
                    else
                    {
                        DisplaySyntax(client);
                        return;
                    }

                }
            }
            else
            {
                DisplaySyntax(client);
                return;
            }

            if (m_counter % 5 != 0)
                m_counter = (m_counter / 5 * 5);

            if (m_counter == 0)
                m_counter = m_time * 60;

            date = DateTime.Now;
            date = date.AddSeconds(m_counter);

            foreach (GameClient m_client in WorldMgr.GetAllPlayingClients())
            {
                m_client.Out.SendMessage("Server Shutdown in " + m_counter / 60 + " minutes. (Reboot on " + date.ToString("HH:mm \"GMT\" zzz") + ")", eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }

            string msg = "Server Shutdown in " + m_counter / 60 + " minutes. (Reboot on " + date.ToString("HH:mm \"GMT\" zzz") + ")";
            log.Info(msg);


            m_timer = new Timer(new TimerCallback(CountDown), null, 0, 5000);
        }
    }
}