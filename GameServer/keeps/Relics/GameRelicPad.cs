using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Logging;
using JNogueira.Discord.WebhookClient;

namespace DOL.GS
{
    public class GameRelicPad : GameStaticItem
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

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

        public HashSet<GameRelic> MountedRelics { get; private set; } = new(3);

        public GameRelicPad() : base() { }

        public override bool AddToWorld()
        {
            bool success = base.AddToWorld();

            if (success)
            {
                _area = new(this);
                CurrentRegion.AddArea(_area);
                RelicMgr.AddRelicPad(this);
            }

            return success;
        }

        public override bool RemoveFromWorld()
        {
            bool success = base.RemoveFromWorld();

            if (success)
            {
                if (_area != null)
                    CurrentRegion.RemoveArea(_area);

                RelicMgr.RemoveRelicPad(this);
            }

            return success;
        }

        public bool IsMountedHere(GameRelic relic)
        {
            return MountedRelics.Contains(relic);
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

            if (DiscordClientManager.TryGetClient(WebhookType.RvR, out var client))
            {
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

                client.SendToDiscordAsync(discordMessage);
            }
        }

        public bool MountRelic(GameRelic relic, bool returning)
        {
            if (MountedRelics.Count >= 3)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Pad {Name} is full (3 relics) {relic.Name}");

                return false;
            }

            if (!MountedRelics.Add(relic))
            {
                if (log.IsErrorEnabled)
                    log.Error($"Relic {relic.Name} is already mounted on pad {Name} (Returning: {returning})");

                return false;
            }

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

                foreach (GamePlayer player in relic.CurrentCarrier.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE * 5))
                {
                    if (player.Realm == relic.CurrentCarrier.Realm)
                        player.CapturedRelics++;
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

            relic.Realm = Realm;
            relic.CurrentRegionID = CurrentRegionID;
            UpdateRelicPositions();
            return true;
        }

        public void RemoveRelic(GameRelic relic)
        {
            if (!MountedRelics.Remove(relic))
            {
                if (log.IsErrorEnabled)
                    log.Error($"Relic {relic.Name} is not mounted on pad {Name}");

                return;
            }

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

            UpdateRelicPositions();
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

        public void RemoveRelics()
        {
            MountedRelics.Clear();
        }

        private void UpdateRelicPositions()
        {
            int count = MountedRelics.Count;

            if (count == 0)
                return;

            if (count == 1)
            {
                GameRelic singleRelic = MountedRelics.First();
                singleRelic.X = X;
                singleRelic.Y = Y;
                singleRelic.Z = Z;
                singleRelic.Heading = Heading;
                return;
            }

            double baseRotation = (Heading - 1024) * Math.PI / 2048.0;
            double angleIncrement = 2 * Math.PI / count;
            int i = 0;

            foreach (GameRelic relic in MountedRelics)
            {
                const int Radius = 50;

                double angle = baseRotation + angleIncrement * i++;
                relic.X = (int) (X + Radius * Math.Cos(angle));
                relic.Y = (int) (Y + Radius * Math.Sin(angle));
                relic.Z = Z;
                relic.Heading = (ushort) (2048 * angle / Math.PI + 3072);
            }
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
