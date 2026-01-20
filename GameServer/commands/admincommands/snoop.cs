using System.Collections.Generic;
using System.Threading;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&snoop",
        ePrivLevel.Admin,
        "Snoops",
        "/snoop <player>")]
    public class SnoopCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        /*
         * This command allows for the interception of private messages (Whispers/PMs) and
         * system logs. The use of this tool may be subject to data privacy laws such as
         * GDPR (Europe), CCPA (California), and other regional telecommunications acts.
         * 
         * By using this code, the Server Administrator assumes full responsibility for:
         * 1. Informing players that their private communications are not end-to-end encrypted
         *    and may be monitored for moderation or debugging purposes.
         * 2. Ensuring compliance with local wiretapping and data privacy regulations.
         * 
         * The authors of this software provide this tool for administrative debugging
         * purposes only and accept no liability for its misuse.
         */

        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer target = null;

            if (args.Length <= 1)
            {
                target = client.Player.TargetObject as GamePlayer;

                if (target == null)
                {
                    DisplayMessage(client, "Invalid target.");
                    return;
                }
            }
            else
            {
                string targetArg = args[1];

                if (!string.IsNullOrEmpty(targetArg))
                    target = ClientService.Instance.GetPlayerByPartialName(targetArg, out _);

                if (target == null)
                {
                    DisplayMessage(client, $"No player found for '{targetArg}'.");
                    return;
                }
            }

            if (target.Client.Account.PrivLevel >= client.Account.PrivLevel)
            {
                DisplayMessage(client, $"You lack sufficient privileges to snoop this player.");
                return;
            }

            bool enabled = SnoopManager.ToggleSnoop(client.Player, target);
            DisplayMessage(client, enabled ? $"Now snooping {target.Name}." : $"Stopped snooping {target.Name}.");
        }
    }

    public static class SnoopManager
    {
        private static readonly SnoopTimer _snoopTimer = new(null);
        private static readonly Dictionary<GamePlayer, List<GamePlayer>> _activeObserver = new();
        private static readonly Lock _lock = new();

        public static void CheckAndBroadcast(GamePlayer target, string message, eChatType chatType, eChatLoc chatLoc)
        {
            if (_activeObserver.Count == 0)
                return;

            List<GamePlayer> observers = null;

            lock (_lock)
            {
                if (!_activeObserver.TryGetValue(target, out observers))
                    return;
            }

            for (int i = observers.Count - 1; i >= 0; i--)
            {
                GamePlayer observer = observers[i];

                if (observer.ObjectState is GameObject.eObjectState.Deleted)
                    continue;

                // Prevent potential cyclic references (and stack overflows).
                observer.Out.SendRawMessage($"<Snoop: {target.Name}>  {message}", chatType, chatLoc);
            }
        }

        public static bool ToggleSnoop(GamePlayer observer, GamePlayer target)
        {
            lock (_lock)
            {
                if (!_activeObserver.TryGetValue(target, out List<GamePlayer> observerList))
                {
                    observerList = new();
                    _activeObserver[target] = observerList;
                }

                if (observerList.Remove(observer))
                {
                    if (observerList.Count == 0)
                        _activeObserver.Remove(target);

                    return false;
                }
                else
                {
                    observerList.Add(observer);

                    if (!_snoopTimer.IsAlive)
                        _snoopTimer.Start();

                    return true;
                }
            }
        }

        public static bool CheckOnlineStatus()
        {
            lock (_lock)
            {
                foreach (var pair in _activeObserver)
                {
                    GamePlayer player = pair.Key;

                    if (player.ObjectState is GameObject.eObjectState.Deleted)
                    {
                        _activeObserver.Remove(player);
                        continue;
                    }

                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        player = pair.Value[i];

                        if (player.ObjectState is GameObject.eObjectState.Deleted)
                            pair.Value.SwapRemoveAt(i);
                    }

                    if (pair.Value.Count == 0)
                        _activeObserver.Remove(player);
                }

                return _activeObserver.Count > 0;
            }
        }

        private class SnoopTimer : ECSGameTimerWrapperBase
        {
            public SnoopTimer(GameObject owner) : base(owner) { }

            protected override int OnTick(ECSGameTimer timer)
            {
                return CheckOnlineStatus() ? 10000 : 0;
            }
        }
    }
}
