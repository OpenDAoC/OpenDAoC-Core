using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public enum eNewsType : byte
    {
        RvRGlobal = 0,
        RvRLocal = 1,
        PvE = 2,
    }

    public class NewsMgr
    {
        public static void CreateNews(string message, eRealm realm, eNewsType type, bool sendMessage)
        {
            if (sendMessage)
            {
                foreach (GamePlayer player in ClientService.GetPlayersOfRealm(realm))
                    player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            if (ServerProperties.Properties.RECORD_NEWS)
            {
                DbNews news = new()
                {
                    Type = (byte) type,
                    Realm = (byte) realm,
                    Text = message
                };
                GameServer.Database.AddObject(news);
                GameEventMgr.Notify(DatabaseEvent.NewsCreated, new NewsEventArgs(news));
            }
        }

        public static void DisplayNews(GameClient client)
        {
            // N,chanel(0/1/2),index(0-4),string time,\"news\"

            for (int type = 0; type <= 2; type++)
            {
                int index = 0;
                string realm = "";
                //we can see all captures
                IList<DbNews> newsList;

                if (type > 0)
                    newsList = DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(type).And(DB.Column("Realm").IsEqualTo(0).Or(DB.Column("Realm").IsEqualTo(realm))));
                else
                    newsList = DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(type));

                newsList = newsList.OrderByDescending(it => it.CreationDate).Take(5).ToArray();
                int n = newsList.Count;

                while (n > 0)
                {
                    n--;
                    DbNews news = newsList[n];
                    client.Out.SendMessage(string.Format("N,{0},{1},{2},\"{3}\"", news.Type, index++, RetElapsedTime(news.CreationDate), news.Text), eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
                }
            }
        }

        private static string RetElapsedTime(DateTime dt)
        {
            TimeSpan playerEnterGame = DateTime.Now.Subtract(dt);
            string newsTime;

            if (playerEnterGame.Days > 0)
                newsTime = $"{playerEnterGame.Days} day{(playerEnterGame.Days > 1 ? "s" : "")}";
            else if (playerEnterGame.Hours > 0)
                newsTime = $"{playerEnterGame.Hours} hour{(playerEnterGame.Hours > 1 ? "s" : "")}";
            else if (playerEnterGame.Minutes > 0)
                newsTime = $"{playerEnterGame.Minutes} minute{(playerEnterGame.Minutes > 1 ? "s" : "")}";
            else
                newsTime = $"{playerEnterGame.Seconds} second{(playerEnterGame.Seconds > 1 ? "s" : "")}";

            return newsTime;
        }
    }
}
