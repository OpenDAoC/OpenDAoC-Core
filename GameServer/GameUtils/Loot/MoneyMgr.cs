using System.Text;
using Core.GS.Languages;
using Core.GS.ServerProperties;

namespace Core.GS.GameUtils;

/// <summary>
/// Encapsulates money operations
/// currently there is no instance of Money
/// use long instead
/// </summary>
public class MoneyMgr
{
	private MoneyMgr()
	{
	}

	// 11111111111
	// 550000000   // 11111111111

	public static int GetMithril(long money)
	{
		return (int)(money / 100L / 100L / 1000L / 1000L % 1000L);
	}

	public static int GetPlatinum(long money)
	{
		return (int)(money / 100L / 100L / 1000L % 1000L);
	}

	public static int GetGold(long money)
	{
		return (int)(money / 100L / 100L % 1000L);
	}

	public static int GetSilver(long money)
	{
		return (int)(money / 100L % 100L);
	}

	public static int GetCopper(long money)
	{
		return (int)(money % 100L);
	}

	public static long GetMoney(int mithril, int platinum, int gold, int silver, int copper)
	{
		return ((((long)mithril * 1000L + (long)platinum) * 1000L + (long)gold) * 100L + (long)silver) * 100L + (long)copper;
	}

	/// <summary>
	/// return different formatted strings for money
	/// </summary>
	/// <param name="money"></param>
	/// <returns></returns>
	public static string GetString(long money)
	{
		if (money == 0)
			return LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Money.GetString.Text1");

		int copper = GetCopper(money);
		int silver = GetSilver(money);
		int gold = GetGold(money);
		int platin = GetPlatinum(money);
		int mithril = GetMithril(money);

		var res = new StringBuilder();
		if (mithril != 0)
		{
			res.Append(mithril);
			res.Append(" ");
			res.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Money.GetString.Text2"));
			res.Append(" ");
		}
		if (platin != 0)
		{
			res.Append(platin);
			res.Append(" ");
			res.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Money.GetString.Text3"));
			res.Append(" ");
		}
		if (gold != 0)
		{
			res.Append(gold);
			res.Append(" ");
			res.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Money.GetString.Text4"));
			res.Append(" ");
		}
		if (silver != 0)
		{
			res.Append(silver);
			res.Append(" ");
			res.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Money.GetString.Text5"));
			res.Append(" ");
		}
		if (copper != 0)
		{
			res.Append(copper);
			res.Append(" ");
			res.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Money.GetString.Text6"));
			res.Append(" ");
		}

		// remove last comma
		if (res.Length > 1)
			res.Length -= 2;

		return res.ToString();
	}

	public static string GetShortString(long money)
	{
		if (money == 0)
			return LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Money.GetString.Text1");

		int copper = GetCopper(money);
		int silver = GetSilver(money);
		int gold = GetGold(money);
		int platin = GetPlatinum(money);
		int mithril = GetMithril(money);

		var res = new StringBuilder();
		if (mithril != 0)
		{
			res.Append(mithril);
			res.Append("m, ");
		}
		if (platin != 0)
		{
			res.Append(platin);
			res.Append("p, ");
		}
		if (gold != 0)
		{
			res.Append(gold);
			res.Append("g, ");
		}
		if (silver != 0)
		{
			res.Append(silver);
			res.Append("s, ");
		}
		if (copper != 0)
		{
			res.Append(copper);
			res.Append("c, ");
		}

		// remove last comma
		if (res.Length > 1)
			res.Length -= 2;

		return res.ToString();
	}

	/// <summary>
	/// Calculate an approximate price for given level and quality
	/// </summary>
	/// <param name="level"></param>
	/// <param name="quality"></param>
	/// <returns></returns>
	public static long SetAutoPrice(int level, int quality)
	{
		int levelmod = level * level * level;

		double dCopper = (levelmod / 0.6); // level 50, 100 quality; worth aprox 20 gold, sells for 10 gold
		double dQuality = quality / 100.0;

		dCopper = dCopper * dQuality * dQuality * dQuality * dQuality * dQuality * dQuality;

		if (dCopper < 2)
			dCopper = 2;

		return (long)dCopper;
	}
}