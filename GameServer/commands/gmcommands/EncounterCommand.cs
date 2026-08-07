using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands
{
    [Cmd(
        "&encounter",
        ePrivLevel.GM,
        "Inspects and manipulates the raid encounter of the targeted NPC.",
        "/encounter info - encounter state of the target, or every active encounter when nothing is targeted",
        "/encounter list - every active encounter",
        "/encounter roster - roster members of the target's encounter",
        "/encounter add <playerName> - adds an online player to the roster",
        "/encounter remove <playerName> - removes a player from the roster",
        "/encounter setsize <n> - overrides the encounter size and recomputes the scaling",
        "/encounter kill - kills the target through the normal credit and reward flow",
        "/encounter rewards - grants the personal rewards without killing the target",
        "/encounter snapshot - forces the roster snapshot now",
        "/encounter clear - resets the target's encounter")]
    public class EncounterCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {
                case "info":
                    Info(client);
                    break;
                case "list":
                    ListActiveEncounters(client);
                    break;
                case "roster":
                    Roster(client);
                    break;
                case "add":
                    Add(client, args);
                    break;
                case "remove":
                    Remove(client, args);
                    break;
                case "setsize":
                    SetSize(client, args);
                    break;
                case "kill":
                    Kill(client);
                    break;
                case "rewards":
                    Rewards(client);
                    break;
                case "snapshot":
                    SnapshotNow(client);
                    break;
                case "clear":
                    Clear(client);
                    break;
                default:
                    DisplaySyntax(client);
                    break;
            }
        }

        private void Info(GameClient client)
        {
            if (client.Player.TargetObject is not GameNPC npc)
            {
                ListActiveEncounters(client);
                return;
            }

            RaidEncounter encounter = GetEncounter(client, npc);

            if (encounter == null)
                return;

            DisplayMessage(client, $"Raid encounter of {npc.Name}:");

            if (!encounter.Active)
                DisplayMessage(client, "Active: no.");
            else
            {
                DisplayMessage(client, $"Active: yes, size {encounter.Size}, scale size {encounter.ScaleSize}, HP multiplier {encounter.HpMultiplier:0.###}, bonus loot rolls {encounter.BonusLootRolls}.");
                DisplayMessage(client, $"Roster {encounter.RosterCount}, participants {encounter.ParticipantCount}, active attackers {encounter.GetActiveAttackerCount()}.");
            }

            DisplayMessage(client, $"Rewards: {encounter.BountyPointsReward} bounty points, {encounter.CurrencyItemCount} x {encounter.CurrencyItemTemplateId ?? "nothing"}.");
        }

        private void ListActiveEncounters(GameClient client)
        {
            List<RaidEncounter> encounters = RaidEncounter.GetActiveEncounters();

            if (encounters.Count == 0)
            {
                DisplayMessage(client, "There are no active raid encounters.");
                return;
            }

            DisplayMessage(client, $"Active raid encounters: {encounters.Count}.");

            foreach (RaidEncounter encounter in encounters)
            {
                GameNPC body = encounter.Owner.Body;
                Region region = body.CurrentRegion;
                string location = region != null ? $"{region.Description} ({region.ID})" : "no region";
                DisplayMessage(client, $"{body.Name} in {location} - roster {encounter.RosterCount}, scale size {encounter.ScaleSize}, HP multiplier {encounter.HpMultiplier:0.###}.");
            }
        }

        private void Roster(GameClient client)
        {
            RaidEncounter encounter = GetTargetEncounter(client, out GameNPC npc);

            if (encounter == null)
                return;

            List<string> rosterIds = encounter.GetRosterIds();

            if (rosterIds.Count == 0)
            {
                DisplayMessage(client, $"The raid encounter of {npc.Name} has an empty roster.");
                return;
            }

            Dictionary<string, GamePlayer> onlinePlayers = new();

            foreach (GamePlayer player in ClientService.Instance.GetPlayers())
                onlinePlayers[player.InternalID] = player;

            DisplayMessage(client, $"Raid encounter roster of {npc.Name} ({rosterIds.Count}):");

            foreach (string internalId in rosterIds)
            {
                bool hasOnlinePlayer = onlinePlayers.TryGetValue(internalId, out GamePlayer player);
                string name = hasOnlinePlayer ? player.Name : $"{internalId} (offline)";
                DisplayMessage(client, $"{name} - participated: {(encounter.HasParticipated(internalId) ? "yes" : "no")}.");
            }
        }

        private void Add(GameClient client, string[] args)
        {
            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            RaidEncounter encounter = GetActiveTargetEncounter(client, out GameNPC npc);

            if (encounter == null)
                return;

            GamePlayer player = FindPlayer(client, args[2]);

            if (player == null)
                return;

            if (encounter.AddToRoster(player))
                DisplayMessage(client, $"{player.Name} was added to the raid encounter roster of {npc.Name}.");
            else
                DisplayMessage(client, $"{player.Name} is already on the raid encounter roster of {npc.Name}.");
        }

        private void Remove(GameClient client, string[] args)
        {
            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            RaidEncounter encounter = GetActiveTargetEncounter(client, out GameNPC npc);

            if (encounter == null)
                return;

            GamePlayer player = FindPlayer(client, args[2]);

            if (player == null)
                return;

            if (encounter.RemoveFromRoster(player))
            {
                GameNPC ownerBody = encounter.Owner.Body;

                if (ownerBody != null && ownerBody.Health > ownerBody.MaxHealth)
                    ownerBody.Health = ownerBody.MaxHealth;

                DisplayMessage(client, $"{player.Name} was removed from the raid encounter roster of {npc.Name}.");
            }
            else
                DisplayMessage(client, $"{player.Name} is not on the raid encounter roster of {npc.Name}.");
        }

        private void SetSize(GameClient client, string[] args)
        {
            if (args.Length < 3 || !int.TryParse(args[2], out int size) || size < 1)
            {
                DisplaySyntax(client);
                return;
            }

            RaidEncounter encounter = GetActiveTargetEncounter(client, out GameNPC npc);

            if (encounter == null)
                return;

            encounter.Size = size;
            GameNPC ownerBody = encounter.Owner.Body;

            if (ownerBody != null && ownerBody.Health > ownerBody.MaxHealth)
                ownerBody.Health = ownerBody.MaxHealth;

            DisplayMessage(client, $"Raid encounter of {npc.Name} resized: size {encounter.Size}, scale size {encounter.ScaleSize}, HP multiplier {encounter.HpMultiplier:0.###}, bonus loot rolls {encounter.BonusLootRolls}.");
            DisplayMessage(client, $"Max health is computed live and is now {npc.MaxHealth}. Current health is {npc.Health}.");
        }

        private void Kill(GameClient client)
        {
            GameNPC npc = GetTargetNpc(client);

            if (npc == null)
                return;

            if (!npc.IsAlive)
            {
                DisplayMessage(client, $"{npc.Name} is already dead.");
                return;
            }

            string name = npc.Name;
            npc.Die(client.Player);
            DisplayMessage(client, $"{name} was killed through the normal credit and reward flow.");
        }

        private void Rewards(GameClient client)
        {
            RaidEncounter encounter = GetActiveTargetEncounter(client, out GameNPC npc);

            if (encounter == null)
                return;

            encounter.GrantPersonalRewards(npc);
            DisplayMessage(client, $"Personal rewards of the raid encounter of {npc.Name} were granted to {encounter.GetParticipants().Count} participant(s).");
        }

        private void SnapshotNow(GameClient client)
        {
            if (!Properties.RAID_SCALING_ENABLED)
            {
                DisplayMessage(client, "Raid scaling is disabled (server property 'raid_scaling_enabled').");
                return;
            }

            RaidEncounter encounter = GetTargetEncounter(client, out GameNPC npc);

            if (encounter == null)
                return;

            if (encounter.Active)
            {
                DisplayMessage(client, $"The raid encounter of {npc.Name} is already active.");
                return;
            }

            StandardMobBrain owner = encounter.Owner;

            if (!owner.HasAggro)
            {
                DisplayMessage(client, $"The raid encounter of {npc.Name} has no aggro to snapshot.");
                return;
            }

            if (encounter.Snapshot(owner.Body, owner))
                DisplayMessage(client, $"Raid encounter of {owner.Body.Name} snapshotted: roster {encounter.RosterCount}, size {encounter.Size}, scale size {encounter.ScaleSize}, HP multiplier {encounter.HpMultiplier:0.###}.");
            else
                DisplayMessage(client, $"Raid encounter of {npc.Name} could not be snapshotted, no player on the aggro list.");
        }

        private void Clear(GameClient client)
        {
            RaidEncounter encounter = GetTargetEncounter(client, out GameNPC npc);

            if (encounter == null)
                return;

            encounter.Owner?.FSM.SetCurrentState(eFSMStateType.IDLE);
            encounter.Clear();
            DisplayMessage(client, $"Raid encounter of {npc.Name} was cleared.");
        }

        private GameNPC GetTargetNpc(GameClient client)
        {
            if (client.Player.TargetObject is GameNPC npc)
                return npc;

            DisplayMessage(client, "You must target an NPC.");
            return null;
        }

        private RaidEncounter GetEncounter(GameClient client, GameNPC npc)
        {
            RaidEncounter encounter = (npc.Brain as StandardMobBrain)?.RaidEncounter;

            if (encounter == null)
                DisplayMessage(client, $"{npc.Name} has no raid encounter.");

            return encounter;
        }

        private RaidEncounter GetTargetEncounter(GameClient client, out GameNPC npc)
        {
            npc = GetTargetNpc(client);
            return npc == null ? null : GetEncounter(client, npc);
        }

        private RaidEncounter GetActiveTargetEncounter(GameClient client, out GameNPC npc)
        {
            RaidEncounter encounter = GetTargetEncounter(client, out npc);

            if (encounter == null)
                return null;

            if (encounter.Active)
                return encounter;

            DisplayMessage(client, $"The raid encounter of {npc.Name} is not active.");
            return null;
        }

        private GamePlayer FindPlayer(GameClient client, string playerName)
        {
            GamePlayer player = ClientService.Instance.GetPlayerByPartialName(playerName, out ClientService.PlayerGuessResult result);

            if (player == null)
                DisplayMessage(client, result is ClientService.PlayerGuessResult.FOUND_MULTIPLE ? $"Several online players match '{playerName}'." : $"No online player matches '{playerName}'.");

            return player;
        }
    }
}
