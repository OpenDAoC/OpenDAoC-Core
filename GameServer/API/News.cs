﻿using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class News
{
    private readonly IMemoryCache _cache;

    private readonly int _numNews = 100;

    public News()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    #region News

    public class NewsInfo
    {
        public NewsInfo()
        {
        }

        public NewsInfo(DbNews news)
        {
            if (news == null) return;
            Date = news.CreationDate.ToString("dd-MM-yyyy hh:mm tt");
            Type = NewsTypeToString(Convert.ToInt32(news.Type));
            Realm = RealmIDtoString(Convert.ToInt32(news.Realm));
            Text = news.Text;
        }

        public string Date { get; set; }
        public string Type { get; set; }
        public string Realm { get; set; }
        public string Text { get; set; }
    }

    public List<NewsInfo> GetAllNews()
    {
        var _newsCacheKey = "api_all_news";
        var allNews = new List<NewsInfo>();
        var cache = _cache.Get<List<NewsInfo>>(_newsCacheKey);

        if (cache == null)
        {
            ICollection<DbNews> newsList = DOLDB<DbNews>.SelectAllObjects().OrderByDescending(a => a.CreationDate)
                .Take(_numNews).ToList();

            foreach (var news in newsList) allNews.Add(new NewsInfo(news));
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
        var _newsCacheKey = "api_realm_news_" + realm;
        var allNews = new List<NewsInfo>();
        var cache = _cache.Get<List<NewsInfo>>(_newsCacheKey);

        if (cache == null)
        {
            ICollection<DbNews> newsList = DOLDB<DbNews>.SelectObjects(DB.Column("Realm").IsEqualTo(realm))
                .OrderByDescending(a => a.CreationDate).Take(_numNews).ToList();

            foreach (var news in newsList) allNews.Add(new NewsInfo(news));
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
        var _newsCacheKey = "api_type_news_" + type;
        var allNews = new List<NewsInfo>();
        var cache = _cache.Get<List<NewsInfo>>(_newsCacheKey);

        if (cache == null)
        {
            ICollection<DbNews> newsList = DOLDB<DbNews>.SelectObjects(DB.Column("Type").IsEqualTo(type))
                .OrderByDescending(a => a.CreationDate).Take(_numNews).ToList();

            foreach (var news in newsList) allNews.Add(new NewsInfo(news));
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