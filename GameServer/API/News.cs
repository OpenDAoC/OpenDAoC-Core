using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class News
{
    private IMemoryCache _cache;

    private int _numNews = 100;
    public News()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }
    
    #region News

    public class NewsInfo
    {
        public string Date { get; set; }
        public string Type { get; set; }
        public string Realm { get; set; }
        public string Text { get; set; }

        public NewsInfo()
        {
        }
        public NewsInfo(DBNews news)
        {
            if (news == null) return;
            Date = news.CreationDate.ToString("dd-MM-yyyy hh:mm tt");
            Type = NewsTypeToString(Convert.ToInt32(news.Type));
            Realm = RealmIDtoString(Convert.ToInt32(news.Realm));
            Text = news.Text;
        }
    }

    public List<NewsInfo> GetAllNews()
    {
        string _newsCacheKey = "api_all_news";
        var allNews = new List<NewsInfo>();
        var cache = _cache.Get<List<NewsInfo>>(_newsCacheKey);
        
        if (cache == null)
        {
            ICollection<DBNews> newsList = DOLDB<DBNews>.SelectAllObjects().OrderByDescending(a => a.CreationDate).Take(_numNews).ToList();
            
            foreach (DBNews news in newsList)
            {
                allNews.Add(new NewsInfo(news));
            }
            _cache.Set(_newsCacheKey, allNews, DateTime.Now.AddMinutes(2));
        }
        else
        {
            allNews = cache;
        }
        
        return allNews;
    }
    
    public List<NewsInfo> GetRealmNews(string realm)
    {
        string _newsCacheKey = "api_realm_news_"+ realm;
        var allNews = new List<NewsInfo>();
        var cache = _cache.Get<List<NewsInfo>>(_newsCacheKey);
        
        if (cache == null)
        {
            ICollection<DBNews> newsList = DOLDB<DBNews>.SelectObjects(DB.Column("Realm").IsEqualTo(realm)).OrderByDescending(a => a.CreationDate).Take(_numNews).ToList();
            
            foreach (DBNews news in newsList)
            {
                allNews.Add(new NewsInfo(news));
            }
            _cache.Set(_newsCacheKey, allNews, DateTime.Now.AddMinutes(2));
        }
        else
        {
            allNews = cache;
        }
        
        return allNews;
    }
    
    public List<NewsInfo> GetTypeNews(string type)
    {
        string _newsCacheKey = "api_type_news_"+ type;
        var allNews = new List<NewsInfo>();
        var cache = _cache.Get<List<NewsInfo>>(_newsCacheKey);
        
        if (cache == null)
        {
            ICollection<DBNews> newsList = DOLDB<DBNews>.SelectObjects(DB.Column("Type").IsEqualTo(type)).OrderByDescending(a => a.CreationDate).Take(_numNews).ToList();
            
            foreach (DBNews news in newsList)
            {
                allNews.Add(new NewsInfo(news));
            }
            _cache.Set(_newsCacheKey, allNews, DateTime.Now.AddMinutes(2));
        }
        else
        {
            allNews = cache;
        }
        
        return allNews;
    }
    
    private static string RealmIDtoString(int realm)
    {
        return realm switch
        {
            0 => "None",
            1 => "Albion",
            2 => "Midgard",
            3 => "Hibernia",
            _ => "None"
        };
    }
    
    private static string NewsTypeToString(int type)
    {
        return type switch
        {
            0 => "RvR",
            1 => "PlayerRP",
            2 => "PlayerXP",
            _ => "undefined"
        };
    }

    #endregion
    
}