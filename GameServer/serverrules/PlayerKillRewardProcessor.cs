using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Logging;
using static DOL.GS.ServerRules.IServerRules;

namespace DOL.GS
{
    public ref struct PlayerKillRewardProcessor
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly GamePlayer _playerToAward;
        private readonly GameObject _killer;
        private readonly GamePlayer _killedPlayer;
        private readonly EntityCountTotalDamagePair _entityStats;
        private readonly double _playerTotalDamageReceived;
        private readonly Dictionary<Group, EntityCountTotalDamagePair> _groupCountAndDamage;

        public PlayerKillRewardProcessor(
            GamePlayer playerToAward,
            GameObject killer,
            GamePlayer killedPlayer,
            EntityCountTotalDamagePair entityStats,
            double playerTotalDamageReceived,
            Dictionary<Group, EntityCountTotalDamagePair> groupCountAndDamage)
        {
            _playerToAward = playerToAward;
            _killer = killer;
            _killedPlayer = killedPlayer;
            _entityStats = entityStats;
            _playerTotalDamageReceived = playerTotalDamageReceived;
            _groupCountAndDamage = groupCountAndDamage;
        }

        public void ProcessRewards(out bool isWorthAnything)
        {
            isWorthAnything = _killedPlayer.DeathTime + Properties.RP_WORTH_SECONDS <= _killedPlayer.PlayedTime;
            double damagePercent = CalculateDamagePercent();
            int realmPointsEarned = 0;

            if (isWorthAnything)
            {
                int entityCount = _entityStats.Count;

                // Calculate base rewards.
                int baseRpReward = _killedPlayer.RealmPointsValue / entityCount;
                int baseBpReward = (!Properties.ALLOW_BPS_IN_BGS && _killedPlayer.CurrentZone.IsBG ? 0 : _killedPlayer.BountyPointsValue) / entityCount;
                long baseXpReward = _killedPlayer.ExperienceValue / entityCount;
                long baseMoneyReward = _killedPlayer.MoneyValue / entityCount;

                // Apply caps.
                baseRpReward = Math.Min(baseRpReward, CalculateRpCap());
                baseBpReward = Math.Min(baseBpReward, CalculateBpCap());
                baseXpReward = Math.Min(baseXpReward, CalculateXpCap());
                baseMoneyReward = Math.Min(baseMoneyReward, CalculateMoneyCap());

                // Reward player.
                realmPointsEarned = RewardRealmPoints(baseRpReward, damagePercent);
                RewardBountyPoints(baseBpReward, damagePercent);
                RewardExperience(baseXpReward, damagePercent);
                RewardMoney(baseMoneyReward, damagePercent);
            }
            else
                SendNotWorthRewardMessage();

            // Update stats.
            ProcessPlayerToAwardStats(realmPointsEarned, damagePercent);
        }

        private double CalculateDamagePercent()
        {
            double damagePercent = _entityStats.Damage / _playerTotalDamageReceived;

            if (damagePercent > 1.0)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(damagePercent)} was superior to 1 ({_entityStats.Damage} / {_playerTotalDamageReceived})");

                damagePercent = 1.0;
            }

            return damagePercent;
        }

        private int CalculateRpCap()
        {
            return _playerToAward.RealmPointsValue * 2;
        }

        private int CalculateBpCap()
        {
            return _playerToAward.BountyPointsValue * 2;
        }

        private long CalculateXpCap()
        {
            return _playerToAward.ExperienceValue * Properties.XP_PVP_CAP_PERCENT / 100;
        }

        private long CalculateMoneyCap()
        {
            return _playerToAward.MoneyValue * 2;
        }

        private int RewardRealmPoints(int baseRpReward, double damagePercent)
        {
            int realmPoints = (int) (baseRpReward * damagePercent);
            DbBattleground battleground = GameServer.KeepManager.GetBattleground(_playerToAward.CurrentRegionID);

            // Only award RPs if the player is under the battleground's cap.
            if (battleground == null || (_playerToAward.RealmLevel < battleground.MaxRealmLevel))
                realmPoints = (int) (realmPoints * (1.0 + 2.0 * (_killedPlayer.RealmLevel - _playerToAward.RealmLevel) / 900.0));

            realmPoints += CalculateGroupBonus(realmPoints);

            if (realmPoints > 0)
                _playerToAward.GainRealmPoints(realmPoints, true);

            return realmPoints;
        }

        private int CalculateGroupBonus(int realmPoints)
        {
            if (_playerToAward.Group == null || !_groupCountAndDamage.TryGetValue(_playerToAward.Group, out EntityCountTotalDamagePair value))
                return 0;

            return (int) (realmPoints * (value.Count - 1) * 0.125);
        }

        private void RewardBountyPoints(int baseBpReward, double damagePercent)
        {
            int bountyPoints = (int) (baseBpReward * damagePercent);
            bountyPoints += CalculateOutpostBonus(bountyPoints);

            if (bountyPoints > 0)
                _playerToAward.GainBountyPoints(bountyPoints);
        }

        private int CalculateOutpostBonus(int bountyPoints)
        {
            if (KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_5, _playerToAward.Realm))
                return (int) (bountyPoints / 100.0 * 5);

            if (KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_3, _playerToAward.Realm))
                return (int) (bountyPoints / 100.0 * 3);

            return 0;
        }

        private void RewardExperience(long baseXpReward, double damagePercent)
        {
            long experience = (long) (baseXpReward * damagePercent);
            experience += GameServer.ServerRules.CalculateOutpostExperienceBonus(_playerToAward, baseXpReward);

            if (experience > 0)
                _playerToAward.GainExperience(eXPSource.Player, experience);
        }

        private void RewardMoney(long baseMoneyReward, double damagePercent)
        {
            long money = (long) (baseMoneyReward * damagePercent);

            if (money > 0)
            {
                _playerToAward.AddMoney(money, "You receive {0}");
                InventoryLogging.LogInventoryAction(_killedPlayer, _playerToAward, eInventoryActionType.Other, money);
            }
        }

        private void ProcessPlayerToAwardStats(int realmPointsEarned, double damagePercent)
        {
            GameObject killerToUse = _killer is GameNPC petKiller && petKiller.Brain is IControlledBrain petKillerBrain ? petKillerBrain.GetPlayerOwner() : _killer;
            _playerToAward.UpdateKillStatsOnPlayerKill(_killedPlayer.Realm, _playerToAward == killerToUse, damagePercent >= 1.0 && _entityStats.Count == 1, realmPointsEarned);
        }

        private void SendNotWorthRewardMessage()
        {
            _playerToAward.Out.SendMessage($"{_killedPlayer.Name} has been killed recently and is worth no realm points!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            _playerToAward.Out.SendMessage($"{_killedPlayer.Name} has been killed recently and is worth no experience!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }
    }
}
