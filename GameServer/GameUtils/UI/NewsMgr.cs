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
		public static void CreateNews(string message, ERealm realm, eNewsType type, bool sendMessage)
		{
			if (sendMessage)
			{
				foreach (GameClient client in WorldMgr.GetAllClients())
				{
					if (client.Player == null)
						continue;
					if ((client.Account.PrivLevel != 1 || realm == ERealm.None) || client.Player.Realm == realm)
					{
						client.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
				}
			}

			if (ServerProperties.ServerProperties.RECORD_NEWS)
			{
				DbNews news = new DbNews();
				news.Type = (byte)type;
				news.Realm = (byte)realm;
				news.Text = message;
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
					newsList = CoreDb<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(type).And(DB.Column("Realm").IsEqualTo(0).Or(DB.Column("Realm").IsEqualTo(realm))));
				else
					newsList = CoreDb<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(type));

				newsList = newsList.OrderByDescending(it => it.CreationDate).Take(5).ToArray();
				int n = newsList.Count;

				while (n > 0)
				{
					n--;
					DbNews news = newsList[n];
					client.Out.SendMessage(string.Format("N,{0},{1},{2},\"{3}\"", news.Type, index++, RetElapsedTime(news.CreationDate), news.Text), EChatType.CT_SocialInterface, EChatLoc.CL_SystemWindow);
				}
			}
		}

		private static string RetElapsedTime(DateTime dt)
		{
			TimeSpan playerEnterGame = DateTime.Now.Subtract(dt);
			string newsTime;
			if (playerEnterGame.Days > 0)
				newsTime = playerEnterGame.Days.ToString() + " day" + ((playerEnterGame.Days > 1) ? "s" : "");
			else if (playerEnterGame.Hours > 0)
				newsTime = playerEnterGame.Hours.ToString() + " hour" + ((playerEnterGame.Hours > 1) ? "s" : "");
			else if (playerEnterGame.Minutes > 0)
				newsTime = playerEnterGame.Minutes.ToString() + " minute" + ((playerEnterGame.Minutes > 1) ? "s" : "");
			else
				newsTime = playerEnterGame.Seconds.ToString() + " second" + ((playerEnterGame.Seconds > 1) ? "s" : "");
			return newsTime;
		}
	}
}