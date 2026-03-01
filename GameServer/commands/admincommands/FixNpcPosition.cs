using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using DOL.Logging;
using static DOL.GS.GameNPC;

namespace DOL.GS.Commands
{
    [Cmd(
        "&fixnpcspawn",
        ePrivLevel.Admin,
        "Fix NPC positions and spawn points using navmesh.",
        "/fixnpcspawn [target|zone|region|world] <SnapDown> <SnapUp> <DryRun(true(default)/false)>",
        "/fixnpcspawn goto <index> - Teleport to a fixed NPC from the last run",
        "/fixnpcspawn result <page|clear> - Print results (2k per page) or clear memory")]
    public class FixNpcSpawnCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const int PAGE_SIZE = 2000;

        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static List<FixResult> _results;

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            string subCommand = args[1].ToLower();

            if (subCommand is "goto")
            {
                ProcessTeleportSubCommand(client, args);
                return;
            }

            if (subCommand is "result")
            {
                ProcessResultSubCommand(client, args);
                return;
            }

            string scope = subCommand;
            float snapDown = 512f;
            float snapUp = 512f;
            bool dryRun = true;

            if (args.Length > 2)
                _ = float.TryParse(args[2], out snapDown);

            if (args.Length > 3)
                _ = float.TryParse(args[3], out snapUp);

            if (args.Length > 4)
                _ = bool.TryParse(args[4], out dryRun);

            if (_results == null)
                _results = new();
            else
                _results.Clear();

            Log(client, $"==== FixNpcSpawn: {scope.ToUpper()} ({nameof(snapDown)}: {snapDown}, {nameof(snapUp)}: {snapUp}, {nameof(dryRun)}: {dryRun}) ====");

            List<GameNPC> npcsToProcess = GetNpcsByScope(client, scope);

            if (npcsToProcess == null || npcsToProcess.Count == 0)
            {
                DisplayMessage(client, "No NPCs found for the selected scope.");
                return;
            }

            ProcessNpcs(client, npcsToProcess, snapDown, snapUp, dryRun);
        }

        private void ProcessTeleportSubCommand(GameClient client, string[] args)
        {
            if (_results == null || _results.Count == 0)
            {
                DisplayMessage(client, "No fix results in memory.");
                return;
            }

            if (args.Length < 3 || !int.TryParse(args[2], out int index))
            {
                DisplayMessage(client, "Invalid index.");
                return;
            }

            if (index < 0 || index >= _results.Count)
            {
                DisplayMessage(client, $"Index out of range. Valid range: 0 - {_results.Count - 1}");
                return;
            }

            FixResult result = _results[index];

            client.Player.MoveTo(
                result.Zone.ZoneRegion.ID,
                (int) Math.Round(result.NewPos.X),
                (int) Math.Round(result.NewPos.Y),
                (int) Math.Round(result.NewPos.Z),
                result.Npc.Heading);

            DisplayMessage(client, $"Teleported to result #{index}: {result.Npc.Name} in {result.Zone.Description}.");
        }

        private void ProcessResultSubCommand(GameClient client, string[] args)
        {
            if (_results == null || _results.Count == 0)
            {
                DisplayMessage(client, "No fix results in memory.");
                return;
            }

            string param = args.Length > 2 ? args[2].ToLower() : "1";

            if (param is "clear")
            {
                _results = null;
                DisplayMessage(client, "Cleared results.");
                return;
            }

            if (!int.TryParse(param, out int page))
                page = 1;

            int totalCount = _results.Count;
            int totalPages = (int) Math.Ceiling((double) totalCount / PAGE_SIZE);
            page = Math.Clamp(page, 1, totalPages);
            int startIndex = (page - 1) * PAGE_SIZE;
            int endIndex = Math.Min(startIndex + PAGE_SIZE, totalCount);

            DisplayMessage(client, $"==== Results Page {page}/{totalPages} (Total: {totalCount}) ====");

            for (int i = startIndex; i < endIndex; i++)
            {
                FixResult result = _results[i];
                DisplayResult(client, i, result);
            }

            if (page < totalPages)
                DisplayMessage(client, $"Type '/fixnpcspawn result {page + 1}' for the next page.");
        }

        private List<GameNPC> GetNpcsByScope(GameClient client, string scope)
        {
            List<GameNPC> list = GameLoop.GetListForTick<GameNPC>();

            switch (scope)
            {
                case "target":
                {
                    if (client.Player.TargetObject is GameNPC targetNpc)
                        list.Add(targetNpc);
                    else
                        DisplayMessage(client, "Your target is not an NPC.");

                    break;
                }
                case "zone":
                {
                    if (client.Player.CurrentZone != null)
                        list.AddRange(GetZoneNpcs(client.Player.CurrentZone));

                    break;
                }
                case "region":
                {
                    if (client.Player.CurrentRegion != null)
                    {
                        foreach (Zone zone in client.Player.CurrentRegion.Zones)
                            list.AddRange(GetZoneNpcs(zone));
                    }

                    break;
                }
                case "world":
                {
                    foreach (Region region in WorldMgr.GetAllRegions())
                    {
                        foreach (Zone zone in region.Zones)
                            list.AddRange(GetZoneNpcs(zone));
                    }

                    break;
                }
                default:
                {
                    DisplayMessage(client, $"Unknown scope '{scope}'. Use: target, zone, region, world.");
                    return null;
                }
            }

            return list;
        }

        private static List<GameNPC> GetZoneNpcs(Zone zone)
        {
            return zone.GetNPCsOfZone([eRealm.None, eRealm.Albion, eRealm.Midgard, eRealm.Hibernia], 0, 0, 0, 0, false);
        }

        private void ProcessNpcs(GameClient client, List<GameNPC> npcs, float snapDown, float snapUp, bool dryRun)
        {
            int processed = 0;
            int fixedCount = 0;

            foreach (GameNPC npc in npcs)
            {
                processed++;

                if (ShouldNpcBeExcluded(npc))
                    continue;

                Zone zone = npc.CurrentZone;

                if (zone == null || !zone.IsPathfindingEnabled)
                    continue;

                FixResult result = CheckAndFixNpc(zone, npc, snapDown, snapUp, dryRun);

                if (result != null)
                {
                    if (fixedCount < PAGE_SIZE)
                        DisplayResult(client, fixedCount, result);

                    _results.Add(result);
                    fixedCount++;
                }
            }

            Log(client, $"Scanned {processed} NPCs. Issues found: {fixedCount}.");

            if (fixedCount > 0)
                DisplayMessage(client, $"Total results: {_results.Count}. Type '/fixnpcspawn result <page>' to view details or '/fixnpcspawn goto <index>' to teleport.");
        }

        private static FixResult CheckAndFixNpc(
            Zone zone,
            GameNPC npc,
            float snapMaxDistanceDown,
            float snapMaxDistanceUp,
            bool dryRun)
        {
            Point3D spawnPoint = npc.SpawnPoint;
            Vector3 pos = new(spawnPoint.X, spawnPoint.Y, spawnPoint.Z);
            EDtPolyFlags[] filters = PathfindingProvider.Instance.DefaultFilters;

            // Check if already valid.
            if (PathfindingProvider.Instance.GetClosestPoint(zone, pos, filters).HasValue)
                return null;

            Vector3? fixedPos = CalculateFixedPosition(zone, pos, snapMaxDistanceDown, snapMaxDistanceUp, filters);

            if (!fixedPos.HasValue)
                return null;

            float distance = (pos - fixedPos.Value).Length();

            if (!dryRun)
            {
                npc.MoveInRegion(
                    zone.ZoneRegion.ID,
                    (int) Math.Round(fixedPos.Value.X),
                    (int) Math.Round(fixedPos.Value.Y),
                    (int) Math.Round(fixedPos.Value.Z),
                    npc.SpawnHeading,
                    true);
                npc.SpawnPoint = new(npc);
                npc.SaveIntoDatabase();
            }

            return new()
            {
                Npc = npc,
                OriginalPos = pos,
                NewPos = fixedPos.Value,
                Zone = zone,
                Distance = distance
            };
        }

        private void DisplayResult(GameClient client, int index, FixResult result)
        {
            DisplayMessage(client, $"[{index}]: {result.Npc.Name} (Dist: {result.Distance:F1}) in {result.Zone.Description}");
        }

        private static Vector3? CalculateFixedPosition(
            Zone zone,
            Vector3 pos,
            float snapMaxDistanceDown,
            float snapMaxDistanceUp,
            EDtPolyFlags[] filters)
        {
            Vector3? fixedPos = null;

            if (snapMaxDistanceDown > 0)
                fixedPos = PathfindingProvider.Instance.GetFloorBeneath(zone, pos, snapMaxDistanceDown, filters);

            if (!fixedPos.HasValue && snapMaxDistanceUp > 0)
                fixedPos = PathfindingProvider.Instance.GetRoofAbove(zone, pos, snapMaxDistanceUp, filters);

            return fixedPos;
        }

        private static bool ShouldNpcBeExcluded(GameNPC npc)
        {
            return npc == null ||
                npc.Flags.HasFlag(eFlags.FLYING) ||
                npc is GameConsignmentMerchant ||
                npc.InHouse ||
                npc.RespawnInterval == 0;
        }

        private void Log(GameClient client, string message)
        {
            if (log.IsInfoEnabled)
                log.Info(message);

            DisplayMessage(client, message);
        }

        private class FixResult
        {
            public GameNPC Npc;
            public Vector3 OriginalPos;
            public Vector3 NewPos;
            public Zone Zone;
            public float Distance;
        }
    }
}
