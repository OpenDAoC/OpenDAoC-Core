using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Logging;
using static DOL.GS.ServerRules.IServerRules;

namespace DOL.GS.ServerRules
{
    public ref struct NpcKillRewardProcessor
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly GamePlayer _player;
        private readonly GameNPC _npc;
        private readonly EntityCountTotalDamagePair _entityStats;
        private readonly double _npcTotalDamage;
        private readonly Dictionary<Group, EntityCountTotalDamagePair> _groupStats;
        private readonly bool _isGrouped;

        private double _damagePercent;
        private long _baseXpReward;
        private long _xpCap;
        private long _campBonus;
        private long _groupBonus;
        private long _guildBonus;
        private long _bafBonus;
        private long _outpostBonus;
        private long _totalReward;
        private bool _modifiedByDamage;

        public NpcKillRewardProcessor(
            GamePlayer player,
            GameNPC npc,
            EntityCountTotalDamagePair entityStats,
            double npcTotalDamage,
            Dictionary<Group, EntityCountTotalDamagePair> groupStats,
            bool isGrouped)
        {
            _player = player;
            _npc = npc;
            _entityStats = entityStats;
            _npcTotalDamage = npcTotalDamage;
            _groupStats = groupStats;
            _isGrouped = isGrouped;

            _damagePercent = 0;
            _baseXpReward = 0;
            _xpCap = 0;
            _campBonus = 0;
            _groupBonus = 0;
            _guildBonus = 0;
            _bafBonus = 0;
            _outpostBonus = 0;
            _totalReward = 0;
            _modifiedByDamage = false;
        }

        public void ProcessRewards()
        {
            _baseXpReward = _isGrouped ? CalculateNpcExperienceModifiedByGroup() : CalculateNpcExperience();

            _damagePercent = CalculateDamagePercent();
            _modifiedByDamage = _damagePercent < 1.0;

            RewardRealmPoints();
            RewardBountyPoints();

            _xpCap = CalculateXpCap();
            _baseXpReward = Math.Min(_baseXpReward, _xpCap);

            if (_baseXpReward <= 0)
                return;

            // This has to be done after capping xp, otherwise a very low level player could simply tag any high level mob and hit the cap.
            _baseXpReward = (long) (_baseXpReward * _damagePercent);

            _campBonus = CalculateCampBonus();
            _groupBonus = CalculateGroupBonus();
            _guildBonus = CalculateGuildBonus();
            _bafBonus = CalculateBafBonus();

            _outpostBonus = GameServer.ServerRules.CalculateOutpostExperienceBonus(_player, _baseXpReward);

            GainedExperienceEventArgs arguments = new(_baseXpReward, _campBonus, _groupBonus, _guildBonus, _bafBonus, _outpostBonus, true, true, eXPSource.NPC);
            _totalReward = arguments.ExpTotal;

            ShowXpStatsToPlayer();
            _player.GainExperience(arguments);
        }

        private readonly double CalculateDamagePercent()
        {
            double damagePercent = _entityStats.Damage / _npcTotalDamage;

            if (damagePercent > 1.0)
            {
                if (log.IsErrorEnabled)
                    log.Error($"damagePercent in AwardPlayerOnNpcKill was superior to 1 ({_entityStats.Damage} / {_npcTotalDamage})");

                damagePercent = 1.0;
            }

            return damagePercent;
        }

        private readonly void RewardRealmPoints()
        {
            int npcRpValue = _npc.RealmPointsValue;
            int realmPoints;

            // Keep and tower captures reward full RP and BP.
            if (_npc is GuardLord)
                realmPoints = npcRpValue;
            else
            {
                int rpCap = _player.RealmPointsValue * 2;
                realmPoints = Math.Min(rpCap, (int) (npcRpValue * _damagePercent));
            }

            if (realmPoints > 0)
                _player.GainRealmPoints(realmPoints);
        }

        private readonly void RewardBountyPoints()
        {
            int npcBpValue = _npc.BountyPointsValue;
            int bountyPoints;

            // Keep and tower captures reward full RP and BP.
            if (_npc is GuardLord)
                bountyPoints = npcBpValue;
            else
            {
                int bpCap = _player.BountyPointsValue * 2;
                bountyPoints = Math.Min(bpCap, (int) (npcBpValue * _damagePercent));
            }

            if (bountyPoints > 0)
                _player.GainBountyPoints(bountyPoints);
        }

        private readonly long CalculateNpcExperience()
        {
            return _npc.ExperienceValue;
        }

        private readonly long CalculateNpcExperienceModifiedByGroup()
        {
            int memberCount = _entityStats.Count;

            if (memberCount <= 1)
                return _npc.ExperienceValue;

            /*
            * http://www.camelotherald.com/more/110.shtml
            * 
            * All group experience is divided evenly amongst group members, if they are in the same level range. What's a level range? One color range.
            * If everyone in the group cons yellow to each other (or high blue, or low orange), experience will be shared out exactly evenly, with no leftover points.
            * How can you determine a color range? Simple - Level divided by ten plus one. So, to a level 40 player (40/10 + 1), 36-40 is yellow, 31-35 is blue,
            * 26-30 is green, and 25-less is gray. But for everyone in the group to get the maximum amount of experience possible, the encounter must be a challenge to
            * the group. If the group has two people, the monster must at least be (con) yellow to the highest level member. If the group has four people, the monster
            * must at least be orange. If the group has eight, the monster must at least be red.
            *
            * If "challenge code" has been activated, then the experience is divided roughly like so in a group of two (adjust the colors up if the group is bigger): If
            * the monster was blue to the highest level player, each lower level group member will ROUGHLY receive experience as if they soloed a blue monster.
            * Ditto for green. As everyone knows, a monster that cons gray to the highest level player will result in no exp for anyone. If the monster was high blue,
            * challenge code may not kick in. It could also kick in if the monster is low yellow to the high level player, depending on the group strength of the pair.
            */

            GamePlayer highestLevelPlayer = _entityStats.HighestLevelPlayer;
            ConColor conColorForHighestLevelPlayerInGroup = ConLevels.GetConColor(highestLevelPlayer.GetConLevel(_npc));

            if (conColorForHighestLevelPlayerInGroup is ConColor.GREY)
                return 0;

            if (_player.XPLogState is eXPLogState.Verbose && memberCount > 1)
                _player.Out.SendMessage($"Base XP divided among {memberCount} members", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            ConColor conColorThreshold;

            // Thresholds according to the comment above. We use the same one for battlegroups.
            if (memberCount >= 8)
                conColorThreshold = ConColor.RED;
            else if (memberCount >= 4)
                conColorThreshold = ConColor.ORANGE;
            else
                conColorThreshold = ConColor.YELLOW;

            // If the con color for the highest level player in the group is above the threshold for "challenge code" to be activated.
            if (conColorForHighestLevelPlayerInGroup >= conColorThreshold)
                return (long) Math.Ceiling((double) _npc.ExperienceValue / memberCount);

            // If we're checking the highest level player, or if the npc is of the same or higher con level for us.
            // We shouldn't try to treat the NPC as if it was of a different con color if it's already of that color to us (this could raise or lower the experience).
            if (highestLevelPlayer == _player || ConLevels.GetConColor(_player.GetConLevel(_npc)) <= conColorForHighestLevelPlayerInGroup)
                return (long) Math.Ceiling((double) _npc.ExperienceValue / memberCount);

            // Find an adequate NPC level so that its con color for the player being handled matches the con color of the highest level player in the group.
            // If it's below yellow, loop downwards; if it's above yellow, loop upwards; if it's yellow, use our own level.
            // We have to check every level starting from the player's. This isn't very efficient but there shouldn't be too many iterations.
            int level = 0;

            if (conColorForHighestLevelPlayerInGroup < ConColor.YELLOW)
            {
                // Downwards loop. Return the first level found.
                for (int i = _player.Level - 1; i > 1; i--)
                {
                    if (ConLevels.GetConColor(ConLevels.GetConLevel(_player.Level, i)) == conColorForHighestLevelPlayerInGroup)
                    {
                        level = i;
                        break;
                    }
                }
            }
            else if (conColorForHighestLevelPlayerInGroup > ConColor.YELLOW)
            {
                level = _player.Level + 1;

                // Upwards loop. Continue until we find the highest level matching this color.
                for (int i = level; i < 51; i++)
                {
                    ConColor color = ConLevels.GetConColor(ConLevels.GetConLevel(_player.Level, i));

                    if (color == conColorForHighestLevelPlayerInGroup)
                        level = i;
                    else if (color > conColorForHighestLevelPlayerInGroup)
                        break;
                }
            }
            else if (conColorForHighestLevelPlayerInGroup is ConColor.YELLOW)
                level = _player.Level;

            if (_player.XPLogState is eXPLogState.Verbose)
                _player.Out.SendMessage($"Base XP set to match the one of a level {level} NPC", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            // If level is still 0 here, something might have gone wrong or the player's level is very low.
            return (long) Math.Ceiling((double) _npc.GetExperienceValueForLevel(level) / memberCount);
        }

        private readonly long CalculateXpCap()
        {
            // http://support.darkageofcamelot.com/kb/article.php?id=438
            // Experience clamps have been raised from 1.1x a same level kill to 1.25x a same level kill.
            // This change has two effects: it will allow lower level players in a group to gain more experience faster (15% faster),
            // and it will also let higher level players (the 35-50s who tend to hit this clamp more often) to gain experience faster.

            long xpCap = GameServer.ServerRules.GetExperienceForLiving(_player.Level);
            return (long) (xpCap * Properties.XP_CAP_PERCENT / 100.0 * _npc.ExceedXPCapAmount);
        }

        private readonly long CalculateCampBonus()
        {
            // 1.49 http://news-daoc.goa.com/view_patchnote_archive.php?id_article=2478
            // "Camp bonuses have been substantially upped in dungeons. Now camp bonuses in dungeons are, on average, 20% higher than outside camp bonuses."
            // Average outside max camp bonus is somewhere between 50 and 60%.

            double fullCampBonus = _npc.CurrentZone.IsDungeon ? Properties.MAX_DUNGEON_CAMP_BONUS : Properties.MAX_CAMP_BONUS;
            double campBonusPerc;

            if (GameLoop.GameLoopTime - _npc.SpawnTick > 1800000)
            {
                campBonusPerc = fullCampBonus;
                _npc.CampBonus = 0.98;
            }
            else
                campBonusPerc = fullCampBonus * _npc.CampBonus;

            return (long) (_baseXpReward * Math.Max(0, campBonusPerc));
        }

        private readonly long CalculateGroupBonus()
        {
            // Maybe this could be disabled in a battlegroup?
            if (_player.Group == null || !_groupStats.TryGetValue(_player.Group, out EntityCountTotalDamagePair value))
                return 0;

            // Group size is reduced by 1 to prevent the bonus from doing more than simply working against the base experience reduction done in `CalculateNpcExperienceValueModifiedByGroup`.
            // For example, a bonus of 100% should nullify that reduction. If the group size wasn't reduced by 1, duos would actually gain more experience than solo players (ignoring other bonuses).
            return (long) (_baseXpReward * (value.Count - 1) * 0.125);
        }

        private readonly long CalculateGuildBonus()
        {
            if (_player.Guild == null || _player.Guild.BonusType is not Guild.eBonusType.Experience)
                return 0;

            return (long) (_baseXpReward * Properties.GUILD_BUFF_XP * 0.01);
        }

        private readonly long CalculateBafBonus()
        {
            if (_npc.Brain is not StandardMobBrain brain)
                return 0;

            return (long) (_baseXpReward * brain.BafAddCount * 0.075);
        }

        private readonly void ShowXpStatsToPlayer()
        {
            if (_player == null || (_player.XPLogState is not eXPLogState.On && _player.XPLogState is not eXPLogState.Verbose))
                return;

            System.Globalization.NumberFormatInfo format = System.Globalization.NumberFormatInfo.InvariantInfo;

            _player.Out.SendMessage($"Base XP: {_baseXpReward.ToString("N0", format)} | Solo Cap : {_xpCap.ToString("N0", format)} | %Cap: {(double)_baseXpReward / _xpCap * 100:0.##}%", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            if (_player.XPLogState is eXPLogState.Verbose)
            {
                long xpNeededForLevel = _player.ExperienceForNextLevel - _player.ExperienceForCurrentLevel;
                double levelPercent = (double) (_player.Experience + _totalReward - _player.ExperienceForCurrentLevel) / xpNeededForLevel * 100.0;
                double campPercent = (double) _campBonus / _baseXpReward * 100.0;
                double groupPercent = (double) _groupBonus / _baseXpReward * 100.0;
                double guildPercent = (double) _guildBonus / _baseXpReward * 100.0;
                double bafPercent = (double) _bafBonus / _baseXpReward * 100.0;
                double outpostPercent = (double) _outpostBonus / _baseXpReward * 100.0;

                _player.Out.SendMessage($"XP needed: {xpNeededForLevel.ToString("N0", format)} | {levelPercent:0.##}% done with current level", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                _player.Out.SendMessage($"# of kills needed to level at this rate: {(double) (_player.ExperienceForNextLevel - _player.Experience) / _totalReward:0.##}", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (_modifiedByDamage && _damagePercent < 1.0)
                    _player.Out.SendMessage($"Damage inflicted: {_damagePercent * 100:0.##}%", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (_campBonus > 0)
                    _player.Out.SendMessage($"Camp: {_campBonus.ToString("N0", format)} | {campPercent:0.##}% bonus", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (_groupBonus > 0)
                    _player.Out.SendMessage($"Group: {_groupBonus.ToString("N0", format)} | {groupPercent:0.##}% bonus", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (_guildBonus > 0)
                    _player.Out.SendMessage($"Guild: {_guildBonus.ToString("N0", format)} | {guildPercent:0.##}% bonus", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (bafPercent > 0)
                    _player.Out.SendMessage($"BaF: {_bafBonus.ToString("N0", format)} | {bafPercent:0.##}% bonus", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (_outpostBonus > 0)
                    _player.Out.SendMessage($"Outpost: {_outpostBonus.ToString("N0", format)} | {outpostPercent:0.##}% bonus", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}
