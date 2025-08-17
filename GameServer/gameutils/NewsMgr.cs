using System;
using System.Collections.Generic;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public enum eNewsType : byte
    {
        RvRGlobal = 0,
        RvRLocal = 1,
        PvE = 2,
    }

    public static class NewsMgr
    {
        private const int MAX_NEWS_PER_TYPE = 5;
        private static readonly ReaderWriterLockSlim _lock = new();
        private static LinkedList<DbNews> _rvrGlobalNews;
        private static Dictionary<eRealm, LinkedList<DbNews>> _rvrLocalNews;
        private static Dictionary<eRealm, LinkedList<DbNews>> _pveNews;

        static NewsMgr()
        {
            // Get the most recent news from the database, for each realm and type.
            _rvrGlobalNews = new(DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(eNewsType.RvRGlobal).OrderBy(DB.Column("CreationDate"), true, MAX_NEWS_PER_TYPE)));

            _rvrLocalNews = new()
            {
                [eRealm.Albion] = new(DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(eNewsType.RvRLocal).And(DB.Column("Realm").IsEqualTo(eRealm.Albion)).OrderBy(DB.Column("CreationDate"), true, MAX_NEWS_PER_TYPE))),
                [eRealm.Midgard] = new(DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(eNewsType.RvRLocal).And(DB.Column("Realm").IsEqualTo(eRealm.Midgard)).OrderBy(DB.Column("CreationDate"), true, MAX_NEWS_PER_TYPE))),
                [eRealm.Hibernia] = new(DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(eNewsType.RvRLocal).And(DB.Column("Realm").IsEqualTo(eRealm.Hibernia)).OrderBy(DB.Column("CreationDate"), true, MAX_NEWS_PER_TYPE)))
            };

            _pveNews = new()
            {
                [eRealm.Albion] = new(DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(eNewsType.PvE).And(DB.Column("Realm").IsEqualTo(eRealm.Albion)).OrderBy(DB.Column("CreationDate"), true, MAX_NEWS_PER_TYPE))),
                [eRealm.Midgard] = new(DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(eNewsType.PvE).And(DB.Column("Realm").IsEqualTo(eRealm.Midgard)).OrderBy(DB.Column("CreationDate"), true, MAX_NEWS_PER_TYPE))),
                [eRealm.Hibernia] = new(DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(eNewsType.PvE).And(DB.Column("Realm").IsEqualTo(eRealm.Hibernia)).OrderBy(DB.Column("CreationDate"), true, MAX_NEWS_PER_TYPE)))
            };
        }

        public static void CreateNews(string message, eRealm realm, eNewsType type, bool sendMessage)
        {
            if (sendMessage)
            {
                foreach (GamePlayer player in ClientService.Instance.GetPlayersOfRealm(realm))
                    player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            if (!ServerProperties.Properties.RECORD_NEWS)
                return;

            DbNews news = new()
            {
                Type = (byte) type,
                Realm = (byte) realm,
                Text = message
            };
            GameServer.Database.AddObject(news);

            _lock.EnterWriteLock();

            try
            {
                switch (type)
                {
                    case eNewsType.RvRGlobal:
                    {
                        _rvrGlobalNews.AddFirst(news);

                        if (_rvrGlobalNews.Count > MAX_NEWS_PER_TYPE)
                            _rvrGlobalNews.RemoveLast();

                        break;
                    }
                    case eNewsType.RvRLocal:
                    {
                        LinkedList<DbNews> newsList = _rvrLocalNews[realm];
                        newsList.AddFirst(news);

                        if (newsList.Count > MAX_NEWS_PER_TYPE)
                            newsList.RemoveLast();

                        break;
                    }
                    case eNewsType.PvE:
                    {
                        LinkedList<DbNews> newsList = _pveNews[realm];
                        newsList.AddFirst(news);

                        if (newsList.Count > MAX_NEWS_PER_TYPE)
                            newsList.RemoveLast();

                        break;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public static void DisplayNews(GameClient client)
        {
            LinkedList<DbNews> newsList;
            int index;
            _lock.EnterReadLock();

            try
            {
                foreach (eNewsType type in Enum.GetValues<eNewsType>())
                {
                    index = 0;

                    switch (type)
                    {
                        case eNewsType.RvRGlobal:
                        {
                            newsList = _rvrGlobalNews;
                            break;
                        }
                        case eNewsType.RvRLocal:
                        {
                            newsList = _rvrLocalNews[client.Player.Realm];
                            break;
                        }
                        case eNewsType.PvE:
                        {
                            newsList = _pveNews[client.Player.Realm];
                            break;
                        }
                        default:
                            continue;
                    }

                    for (LinkedListNode<DbNews> node = newsList.Last; node != null; node = node.Previous)
                    {
                        DbNews news = node.Value;
                        client.Out.SendMessage(string.Format("N,{0},{1},{2},\"{3}\"", news.Type, index++, GetElapsedTime(news.CreationDate), news.Text), eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            static string GetElapsedTime(DateTime dt)
            {
                TimeSpan elapsedTime = DateTime.Now.Subtract(dt);
                string formattedElapsedTime;

                if (elapsedTime.Days > 0)
                    formattedElapsedTime = $"{elapsedTime.Days} day{(elapsedTime.Days > 1 ? "s" : "")}";
                else if (elapsedTime.Hours > 0)
                    formattedElapsedTime = $"{elapsedTime.Hours} hour{(elapsedTime.Hours > 1 ? "s" : "")}";
                else if (elapsedTime.Minutes > 0)
                    formattedElapsedTime = $"{elapsedTime.Minutes} minute{(elapsedTime.Minutes > 1 ? "s" : "")}";
                else
                    formattedElapsedTime = $"{elapsedTime.Seconds} second{(elapsedTime.Seconds > 1 ? "s" : "")}";

                return formattedElapsedTime;
            }
        }
    }
}
