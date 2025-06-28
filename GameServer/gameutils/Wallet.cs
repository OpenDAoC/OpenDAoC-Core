using System;
using System.Text;
using System.Threading;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS
{
    public class Wallet
    {
        private readonly GamePlayer _player;
        private readonly Lock _lock = new();
        private long _amount;

        public Wallet(GamePlayer player)
        {
            _player = player;
        }

        public long GetMoney()
        {
            return Volatile.Read(ref _amount);
        }

        public void LoadMoney(long amount)
        {
            if (GetMoney() > 0)
                throw new InvalidOperationException("Money has already been loaded into the wallet.");

            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            if (amount > 0)
                ChangeMoney(amount, null);
        }

        public void PickUpMoney(long amount, bool isSplitMoney)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            long zoneBonus = GetZoneBonus(amount); // Zone bonus is calculated before guild dues are applied.
            amount = ApplyGuildDues(amount);
            long totalMoney = zoneBonus + amount;

            if (zoneBonus > 0)
                AddMoney(zoneBonus, ZoneBonus.GetBonusMessage(_player, zoneBonus, ZoneBonusType.Money), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            if (amount > 0)
                AddMoney(amount, LanguageMgr.GetTranslation(_player.Client.Account.Language, isSplitMoney ? "GamePlayer.PickupObject.YourLootShare" : "GamePlayer.PickupObject.YouPickUp", WalletHelper.ToString(amount)));

            if (totalMoney > 0)
                InventoryLogging.LogInventoryAction("(ground)", _player, eInventoryActionType.Loot, totalMoney);
        }

        public void AddMoney(long money)
        {
            AddMoney(money, null, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public void AddMoney(long money, string messageFormat)
        {
            AddMoney(money, messageFormat, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public void AddMoney(long money, string messageFormat, eChatType chatType, eChatLoc chatLock)
        {
            ChangeMoney(money, messageFormat, chatType, chatLock);
        }

        public bool RemoveMoney(long money)
        {
            return RemoveMoney(money, null, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public bool RemoveMoney(long money, string messageFormat)
        {
            return RemoveMoney(money, messageFormat, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public bool RemoveMoney(long money, string messageFormat, eChatType chatType, eChatLoc chatLoc)
        {
            return ChangeMoney(-money, messageFormat, chatType, chatLoc);
        }

        private bool ChangeMoney(long money, string messageFormat)
        {
            return ChangeMoney(money, messageFormat, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        private bool ChangeMoney(long money, string messageFormat, eChatType chatType, eChatLoc chatLoc)
        {
            lock (_lock)
            {
                long newMoney = _amount + money;

                if (newMoney < 0)
                    return false;

                _amount = newMoney;

                if (_player.DBCharacter != null)
                {
                    var (mithril, platinum, gold, silver, copper) = WalletHelper.ToMoneyParts(newMoney);
                    _player.DBCharacter.Mithril = mithril;
                    _player.DBCharacter.Platinum = platinum;
                    _player.DBCharacter.Gold = gold;
                    _player.DBCharacter.Silver = silver;
                    _player.DBCharacter.Copper = copper;
                }
            }

            _player.Out.SendUpdateMoney();

            if (messageFormat != null && money != 0)
                _player.Out.SendMessage(string.Format(messageFormat, WalletHelper.ToString(money)), chatType, chatLoc);

            return true;
        }

        private long GetZoneBonus(long money)
        {
            return Properties.ENABLE_ZONE_BONUSES ? (long) (money * ZoneBonus.GetCoinBonus(_player) * 0.01) : 0;
        }

        private long ApplyGuildDues(long money)
        {
            Guild guild = _player.Guild;

            if (guild == null || !guild.IsGuildDuesOn())
                return money;

            long moneyToGuild = money * guild.GetGuildDuesPercent() / 100;
            return moneyToGuild <= 0 || !guild.AddToBank(moneyToGuild, false) ? money : money - moneyToGuild;
        }
    }

    public static class WalletHelper
    {
        public static long ToMoney(int mithril, int platinum, int gold, int silver, int copper)
        {
            return (((mithril * 1000L + platinum) * 1000L + gold) * 100L + silver) * 100L + copper;
        }

        public static (ushort mithril, ushort platinum, ushort gold, byte silver, byte copper) ToMoneyParts(long money)
        {
            byte copper = (byte) (money % 100);
            money /= 100;

            byte silver = (byte) (money % 100);
            money /= 100;

            ushort gold = (ushort) (money % 1000);
            money /= 1000;

            ushort platinum = (ushort) (money % 1000);
            money /= 1000;

            ushort mithril = (ushort) (money % 1000);
            return (mithril, platinum, gold, silver, copper);
        }

        public static long CalculateAutoPrice(int level, int quality)
        {
            double qualityMod = quality / 100.0;
            double copper = level * level * level / 0.6; // Level 50, 100 quality; worth approximately 20 gold, sells for 10 gold.
            copper = copper * qualityMod * qualityMod * qualityMod * qualityMod * qualityMod * qualityMod;

            if (copper < 2)
                copper = 2;

            return (long) copper;
        }

        public static string ToString(long money)
        {
            if (money == 0)
                return LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "WalletHelper.GetString.Text1");

            var (mithril, platinum, gold, silver, copper) = ToMoneyParts(money);
            StringBuilder result = new();

            if (mithril != 0)
            {
                result.Append(mithril);
                result.Append(' ');
                result.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "WalletHelper.GetString.Text2"));
                result.Append(' ');
            }

            if (platinum != 0)
            {
                result.Append(platinum);
                result.Append(' ');
                result.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "WalletHelper.GetString.Text3"));
                result.Append(' ');
            }

            if (gold != 0)
            {
                result.Append(gold);
                result.Append(' ');
                result.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "WalletHelper.GetString.Text4"));
                result.Append(' ');
            }

            if (silver != 0)
            {
                result.Append(silver);
                result.Append(' ');
                result.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "WalletHelper.GetString.Text5"));
                result.Append(' ');
            }

            if (copper != 0)
            {
                result.Append(copper);
                result.Append(' ');
                result.Append(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "WalletHelper.GetString.Text6"));
                result.Append(' ');
            }

            // Remove last comma.
            if (result.Length > 1)
                result.Length -= 2;

            return result.ToString();
        }

        public static string ToShortString(long money)
        {
            if (money == 0)
                return LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "WalletHelper.GetString.Text1");

            var (mithril, platinum, gold, silver, copper) = ToMoneyParts(money);
            StringBuilder result = new();

            if (mithril != 0)
            {
                result.Append(mithril);
                result.Append("m, ");
            }

            if (platinum != 0)
            {
                result.Append(platinum);
                result.Append("p, ");
            }

            if (gold != 0)
            {
                result.Append(gold);
                result.Append("g, ");
            }

            if (silver != 0)
            {
                result.Append(silver);
                result.Append("s, ");
            }

            if (copper != 0)
            {
                result.Append(copper);
                result.Append("c, ");
            }

            // Remove last comma.
            if (result.Length > 1)
                result.Length -= 2;

            return result.ToString();
        }
    }
}
