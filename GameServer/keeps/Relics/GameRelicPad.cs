using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Logging;
using JNogueira.Discord.Webhook.Client;

namespace DOL.GS
{
    public class GameRelicPad : GameStaticItem
    {
        private const int PAD_AREA_RADIUS = 250;

        private PadArea _area;

        public override ushort Model
        {
            get => 2655;
            set => base.Model = value;
        }

        public override eRealm Realm
        {
            get => Emblem switch
            {
                1 or 11 => eRealm.Albion,
                2 or 12 => eRealm.Midgard,
                3 or 13 => eRealm.Hibernia,
                _ => eRealm.None,
            };
            set => base.Realm = value;
        }

        public virtual eRelicType PadType => Emblem switch
        {
            1 or 2 or 3 => eRelicType.Strength,
            11 or 12 or 13 => eRelicType.Magic,
            _ => eRelicType.Invalid,
        };

        public GameRelic MountedRelic { get; private set; }

        public GameRelicPad() : base() { }

        public override bool AddToWorld()
        {
            _area = new(this);
            CurrentRegion.AddArea(_area);
            bool success = base.AddToWorld();

            if (success)
                RelicMgr.AddRelicPad(this);

            return success;
        }

        public override bool RemoveFromWorld()
        {
            if (_area != null)
                CurrentRegion.RemoveArea(_area);

            return base.RemoveFromWorld();
        }

        public bool IsMountedHere(GameRelic relic)
        {
            return MountedRelic == relic;
        }

        public static void BroadcastDiscordRelic(string message, eRealm realm, string keepName)
        {
            int color;
            string avatarUrl;

            switch (realm)
            {
                case eRealm._FirstPlayerRealm:
                {
                    color = 16711680;
                    avatarUrl = string.Empty;
                    break;
                }
                case eRealm._LastPlayerRealm:
                {
                    color = 32768;
                    avatarUrl = string.Empty;
                    break;
                }
                default:
                {
                    color = 255;
                    avatarUrl = string.Empty;
                    break;
                }
            }

            DiscordWebhookClient client = new(ServerProperties.Properties.DISCORD_RVR_WEBHOOK_ID);

            DiscordMessage discordMessage = new(
                "",
                username: "RvR",
                avatarUrl: avatarUrl,
                tts: false,
                embeds:
                [
                    new(
                        author: new(keepName),
                        color: color,
                        description: message
                    )
                ]
            );

            client.SendToDiscord(discordMessage);
        }

        public void MountRelic(GameRelic relic, bool returning)
        {
            MountedRelic = relic;

            if (relic.CurrentCarrier != null && !returning)
            {
                string message = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameRelicPad.MountRelic.Stored", relic.CurrentCarrier.Name, GlobalConstants.RealmToName(relic.CurrentCarrier.Realm), relic.Name, Name);

                foreach (GamePlayer otherPlayer in ClientService.Instance.GetPlayers())
                {
                    otherPlayer.Out.SendMessage(LanguageMgr.GetTranslation(otherPlayer.Client.Account.Language, "GameRelicPad.MountRelic.Captured", GlobalConstants.RealmToName(relic.CurrentCarrier.Realm), relic.Name), eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
                    otherPlayer.Out.SendMessage($"{message}\n{message}\n{message}", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }

                NewsMgr.CreateNews(message, relic.CurrentCarrier.Realm, eNewsType.RvRGlobal, false);

                if (ServerProperties.Properties.DISCORD_ACTIVE && !string.IsNullOrEmpty(ServerProperties.Properties.DISCORD_RVR_WEBHOOK_ID))
                    BroadcastDiscordRelic(message, relic.CurrentCarrier.Realm, relic.Name);

                BattleGroup relicBG = relic.CurrentCarrier?.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
                List<GamePlayer> targets = new();

                if (relicBG != null)
                {
                    lock (relicBG.Members)
                    {
                        foreach (GamePlayer bgPlayer in relicBG.Members.Keys)
                        {
                            if (bgPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                                targets.Add(bgPlayer);
                        }
                    }
                }
                else if (relic.CurrentCarrier.Group != null)
                {
                    foreach (GamePlayer p in relic.CurrentCarrier.Group.GetPlayersInTheGroup())
                        targets.Add(p);
                }
                else
                    targets.Add(relic.CurrentCarrier);

                foreach (GamePlayer target in targets)
                {
                    target.CapturedRelics++;
                    target.Achieve(AchievementUtils.AchievementNames.Relic_Captures);
                }

                relic.LastCaptureDate = DateTime.Now;
                Notify(RelicPadEvent.RelicMounted, this, new RelicPadEventArgs(relic.CurrentCarrier, relic));
            }
            else
            {
                string message = $"The {relic.Name} has been returned to {Name}.";

                foreach (GamePlayer otherPlayer in ClientService.Instance.GetPlayers())
                    otherPlayer.Out.SendMessage($"{message}\n{message}\n{message}", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }
        }

        public void RemoveRelic(GameRelic relic)
        {
            MountedRelic = null;

            if (relic.CurrentCarrier != null)
            {
                string message = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameRelicPad.RemoveRelic.Removed", relic.CurrentCarrier.Name, GlobalConstants.RealmToName((eRealm)relic.CurrentCarrier.Realm), relic.Name, Name);

                foreach (GamePlayer otherPlayer in ClientService.Instance.GetPlayers())
                    otherPlayer.Out.SendMessage($"{message}\n{message}\n{message}", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                NewsMgr.CreateNews(message, relic.CurrentCarrier.Realm, eNewsType.RvRGlobal, false);

                if (ServerProperties.Properties.DISCORD_ACTIVE && !string.IsNullOrEmpty(ServerProperties.Properties.DISCORD_RVR_WEBHOOK_ID))
                    BroadcastDiscordRelic(message, relic.CurrentCarrier.Realm, relic.Name);

                Notify(RelicPadEvent.RelicStolen, this, new RelicPadEventArgs(relic.CurrentCarrier, relic));
            }
        }

        public int GetEnemiesOnPad()
        {
            ICollection<GamePlayer> players = GetPlayersInRadius(500);
            int enemyNearby = 0;

            foreach (GamePlayer player in players)
            {
                if (player.Realm == Realm)
                    continue;

                enemyNearby++;
            }

            return enemyNearby;
        }

        public void RemoveRelic()
        {
            MountedRelic = null;
        }

        public class PadArea : Area.Circle
        {
            private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

            private readonly GameRelicPad _parent;

            public PadArea(GameRelicPad parentPad) : base("", parentPad.X, parentPad.Y, parentPad.Z, PAD_AREA_RADIUS)
            {
                _parent = parentPad;
            }

            public override void OnPlayerEnter(GamePlayer player)
            {
                GameRelic relicOnPlayer = player.TempProperties.GetProperty<GameRelic>(GameRelic.PLAYER_CARRY_RELIC_WEAK);

                if (relicOnPlayer == null)
                    return;

                if (relicOnPlayer.RelicType != _parent.PadType)
                {
                    player.Client.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameRelicPad.OnPlayerEnter.EmptyRelicPad"), relicOnPlayer.RelicType), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                    if (log.IsDebugEnabled)
                        log.Debug($"Player {player.Name} needs to find an empty {relicOnPlayer.RelicType} relic pad in order to place {relicOnPlayer.Name}");

                    return;
                }

                if (player.Realm == _parent.Realm)
                {
                    if (log.IsDebugEnabled)
                        log.Debug($"Player {player.Name} captured relic {relicOnPlayer.Name}");

                    relicOnPlayer.RelicPadTakesOver(_parent, false);
                }
                else
                {
                    if (log.IsDebugEnabled)
                        log.Debug($"Player realm {GlobalConstants.RealmToName(player.Realm)} wrong realm on attempt to capture relic {relicOnPlayer.Name} of realm {GlobalConstants.RealmToName(relicOnPlayer.Realm)} on pad of realm {GlobalConstants.RealmToName(_parent.Realm)}");
                }
            }
        }
    }
}
