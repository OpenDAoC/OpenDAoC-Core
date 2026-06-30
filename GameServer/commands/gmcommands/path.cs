using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [Cmd(
        "&path",
        ePrivLevel.GM,
        "There are several path functions",
        "/path create [speedLimit] [waitTime(s)] [triggerName] - creates a new temporary path, deleting any existing temporary path",
        "/path load <pathName> - loads a path from db",
        "/path loadtarget - loads the path currently used by the selected NPC",
        "/path add [speedLimit] [waitTime(s)] [triggerName] - adds a point at the end of the current path",
        "/path insert <after step> [speedLimit] [waitTime(s)] [triggerName] - inserts a point after an existing pathpoint",
        "/path discardpoint - removes the targeted pathpoint from the current path",
        "/path edit [speedLimit] [waitTime(s)] [triggerName] - edits the targeted pathpoint",
        "/path save [pathName] - saves a path to db, using the loaded path name when omitted",
        "/path travel - makes a target npc travel the current path",
        "/path test - spawns a temporary NPC at the start of the current path and runs it once",
        "/path stop - clears the path for a targeted npc and tells npc to walk to spawn",
        "/path assigntaxiroute <destination> - sets the current path as taxiroute on stablemaster",
        "/path hide - hides all path markers but does not delete the path",
        "/path delete - deletes the temporary path",
        "/path type - changes the paths type",
        "/path visualize - toggles path visualization for the selected NPC")]
    public class PathCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private static string TEMP_PATH_FIRST = "TEMP_PATH_FIRST";
        private static string TEMP_PATH_LAST = "TEMP_PATH_LAST";
        private static string TEMP_PATH_OBJS = "TEMP_PATH_OBJS";
        private static string TEMP_PATH_NAME = "TEMP_PATH_NAME";
        private static string TEMP_PATH_TEST_NPC = "TEMP_PATH_TEST_NPC";
        private static string TEMP_PATH_VISUALIZATION_NPC = "TEMP_PATH_VISUALIZATION_NPC";

        private static void CreatePathPointObject(GameClient client, PathPoint pp, int id)
        {
            GameStaticItem obj = new()
            {
                X = pp.X,
                Y = pp.Y,
                Z = pp.Z + 1,
                CurrentRegion = client.Player.CurrentRegion,
                Heading = client.Player.Heading,
                Name = $"PP ({id})",
                Model = 488,
                Emblem = 0
            };

            obj.AddToWorld();

            ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS);
            objs ??= new();
            objs.Add(obj);
            client.Player.TempProperties.SetProperty(TEMP_PATH_OBJS, objs);
        }

        private static void RemoveAllPathPointObjects(GameClient client)
        {
            ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS);
            if (objs != null)
            {
                RemovePathPointObjects(objs);
                objs.Clear();
            }

            client.Player.TempProperties.SetProperty(TEMP_PATH_OBJS, null);
            client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, null);
            client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, null);
            client.Player.TempProperties.SetProperty(TEMP_PATH_NAME, null);
            client.Player.TempProperties.SetProperty(TEMP_PATH_VISUALIZATION_NPC, null);
        }

        private static void RemovePathPointObjects(GameClient client)
        {
            ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS);
            if (objs == null)
                return;

            RemovePathPointObjects(objs);
            objs.Clear();
            client.Player.TempProperties.SetProperty(TEMP_PATH_OBJS, null);
        }

        private static void RemovePathPointObjects(ArrayList objs)
        {
            foreach (GameStaticItem obj in objs)
                obj.Delete();
        }

        private static int RecreatePathPointObjects(GameClient client, PathPoint first)
        {
            RemovePathPointObjects(client);

            int len = 0;
            PathPoint pathPoint = first;

            while (pathPoint != null)
            {
                CreatePathPointObject(client, pathPoint, ++len);
                pathPoint = pathPoint.Next;
            }

            return len;
        }

        private static int CountPathPoints(PathPoint first, out PathPoint last)
        {
            int len = 0;
            last = null;
            HashSet<PathPoint> visited = new();
            PathPoint pathPoint = first;

            while (pathPoint != null && visited.Add(pathPoint))
            {
                last = pathPoint;
                len++;
                pathPoint = pathPoint.Next;
            }

            return len;
        }

        private bool TryParsePathPointArgs(GameClient client, string[] args, int speedArgIndex, ref short speedLimit, ref int waitTime, ref string triggerName)
        {
            if (args.Length <= speedArgIndex)
                return true;

            if (!short.TryParse(args[speedArgIndex], out speedLimit))
            {
                DisplayMessage(client, $"No valid speedLimit '{args[speedArgIndex]}'!");
                return false;
            }

            int waitArgIndex = speedArgIndex + 1;
            if (args.Length <= waitArgIndex)
                return true;

            if (!int.TryParse(args[waitArgIndex], out waitTime))
            {
                DisplayMessage(client, $"No valid wait time '{args[waitArgIndex]}'!");
                return false;
            }

            int triggerArgIndex = waitArgIndex + 1;
            if (args.Length > triggerArgIndex)
                triggerName = args[triggerArgIndex];

            return true;
        }

        private static PathPoint GetPathPointByStep(PathPoint first, int step)
        {
            int currentStep = 1;
            PathPoint pathPoint = first;

            while (pathPoint != null)
            {
                if (currentStep == step)
                    return pathPoint;

                currentStep++;
                pathPoint = pathPoint.Next;
            }

            return null;
        }

        private static int GetTargetedPathPointStep(GameClient client)
        {
            ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS);
            if (objs == null || client.Player.TargetObject == null)
                return 0;

            for (int i = 0; i < objs.Count; i++)
            {
                if (ReferenceEquals(objs[i], client.Player.TargetObject))
                    return i + 1;
            }

            return 0;
        }

        private static ushort GetPathHeading(PathPoint first)
        {
            return first?.Next == null ? (ushort) 0 : first.GetHeading(first.Next);
        }

        private static PathPoint ClonePathForTest(PathPoint first, short testSpeed)
        {
            PathPoint firstClone = null;
            PathPoint previousClone = null;
            PathPoint pathPoint = first;

            while (pathPoint != null)
            {
                PathPoint clone = new(
                    pathPoint.X,
                    pathPoint.Y,
                    pathPoint.Z,
                    pathPoint.MaxSpeed <= 0 ? testSpeed : pathPoint.MaxSpeed,
                    EPathType.Once,
                    pathPoint.WaitTime,
                    string.Empty);

                firstClone ??= clone;

                if (previousClone != null)
                {
                    previousClone.Next = clone;
                    clone.Prev = previousClone;
                }

                previousClone = clone;
                pathPoint = pathPoint.Next;
            }

            return firstClone;
        }

        private static void PathHide(GameClient client)
        {
            ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS);
            if (objs == null)
                return;

            foreach (GameStaticItem obj in objs)
                obj.Delete();
        }

        private void PathCreate(GameClient client, string[] args)
        {
            short maxSpeed = 1000;
            int waitTime = 0;
            string triggerName = string.Empty;

            if (!TryParsePathPointArgs(client, args, 2, ref maxSpeed, ref waitTime, ref triggerName))
                return;

            RemoveAllPathPointObjects(client);

            PathPoint startpoint = new(client.Player.X, client.Player.Y, client.Player.Z, maxSpeed, EPathType.Once, waitTime * 10, triggerName);
            client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, startpoint);
            client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, startpoint);
            client.Player.TempProperties.SetProperty(TEMP_PATH_NAME, null);
            client.Player.Out.SendMessage("Path creation started! You can add new pathpoints via /path add now!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            CreatePathPointObject(client, startpoint, 1);
        }

        private void PathAdd(GameClient client, string[] args)
        {
            PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST);
            if (path == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create first!");
                return;
            }

            short maxSpeed = 1000;
            int waitTime = 0;
            string triggerName = string.Empty;

            if (!TryParsePathPointArgs(client, args, 2, ref maxSpeed, ref waitTime, ref triggerName))
                return;

            PathPoint newpp = new(client.Player.X, client.Player.Y, client.Player.Z, maxSpeed, path.Type, waitTime * 10, triggerName);
            path.Next = newpp;
            newpp.Prev = path;
            client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, newpp);

            int len = 0;
            while (path.Prev != null)
            {
                len++;
                path = path.Prev;
            }

            len += 2;
            CreatePathPointObject(client, newpp, len);
            DisplayMessage(client, $"Pathpoint added. Current path length = {len}");
        }

        private void PathInsert(GameClient client, string[] args)
        {
            if (args.Length < 3)
            {
                DisplayMessage(client, "Usage: /path insert <after step> [speedLimit] [wait time in second] [trigger name]");
                return;
            }

            PathPoint first = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_FIRST);
            PathPoint last = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST);
            if (first == null || last == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create or /path load first!");
                return;
            }

            if (!int.TryParse(args[2], out int afterStep) || afterStep < 1)
            {
                DisplayMessage(client, $"No valid path step '{args[2]}'!");
                return;
            }

            short speedLimit = 1000;
            int waitTime = 0;
            string triggerName = string.Empty;

            if (!TryParsePathPointArgs(client, args, 3, ref speedLimit, ref waitTime, ref triggerName))
                return;

            PathPoint after = GetPathPointByStep(first, afterStep);
            if (after == null)
            {
                DisplayMessage(client, $"Path step {afterStep} does not exist!");
                return;
            }

            PathPoint next = after.Next;
            PathPoint newpp = new(client.Player.X, client.Player.Y, client.Player.Z, speedLimit, after.Type, waitTime * 10, triggerName)
            {
                Prev = after,
                Next = next
            };
            after.Next = newpp;

            if (next != null)
                next.Prev = newpp;
            else
                client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, newpp);

            int len = RecreatePathPointObjects(client, first);
            DisplayMessage(client, $"Pathpoint inserted after step {afterStep}. Current path length = {len}");
            RefreshPathVisualization(client);
        }

        private void PathEdit(GameClient client, string[] args)
        {
            PathPoint first = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_FIRST);
            if (first == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create or /path load first!");
                return;
            }

            int step = GetTargetedPathPointStep(client);
            if (step < 1)
            {
                DisplayMessage(client, "You need to target one of the current pathpoint markers first!");
                return;
            }

            PathPoint targeted = GetPathPointByStep(first, step);
            if (targeted == null)
            {
                DisplayMessage(client, "Targeted pathpoint marker no longer matches the current path.");
                return;
            }

            short speedLimit = targeted.MaxSpeed;
            int waitTime = targeted.WaitTime / 10;
            string triggerName = targeted.TriggerName;

            if (!TryParsePathPointArgs(client, args, 2, ref speedLimit, ref waitTime, ref triggerName))
                return;

            targeted.MaxSpeed = speedLimit;
            targeted.WaitTime = waitTime * 10;
            targeted.TriggerName = triggerName;

            DisplayMessage(client, $"Pathpoint {step}: MaxSpeed={targeted.MaxSpeed}, WaitTime={targeted.WaitTime / 10}s, Trigger='{targeted.TriggerName}'");
        }

        private void PathDiscardPoint(GameClient client)
        {
            PathPoint first = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_FIRST);
            PathPoint last = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST);
            if (first == null || last == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create or /path load first!");
                return;
            }

            int step = GetTargetedPathPointStep(client);
            if (step < 1)
            {
                DisplayMessage(client, "You need to target one of the current pathpoint markers first!");
                return;
            }

            PathPoint discarded = GetPathPointByStep(first, step);
            if (discarded == null)
            {
                DisplayMessage(client, "Targeted pathpoint marker no longer matches the current path.");
                return;
            }

            PathPoint previous = discarded.Prev;
            PathPoint next = discarded.Next;

            if (previous != null)
                previous.Next = next;
            else
                first = next;

            if (next != null)
                next.Prev = previous;
            else
                last = previous;

            discarded.Prev = null;
            discarded.Next = null;

            client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, first);
            client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, last);

            if (first == null)
            {
                RemovePathPointObjects(client);
                DisplayMessage(client, $"Pathpoint {step} discarded. Current path is empty.");
                return;
            }

            int len = RecreatePathPointObjects(client, first);
            DisplayMessage(client, $"Pathpoint {step} discarded. Current path length = {len}");
            RefreshPathVisualization(client);
        }

        private void PathTravel(GameClient client)
        {
            PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST);
            if (client.Player.TargetObject is not GameNPC targetNpc)
            {
                DisplayMessage(client, "You need to select a mob first!");
                return;
            }

            if (path == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create first!");
                return;
            }

            short speed = Math.Min(targetNpc.MaxSpeedBase, path.MaxSpeed);
            targetNpc.CurrentPathPoint = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_FIRST);
            targetNpc.MoveOnPath(speed);
            DisplayMessage(client, $"{client.Player.TargetObject.Name} told to travel path!");
        }

        private void PathTest(GameClient client)
        {
            PathPoint first = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_FIRST);
            if (first == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create or /path load first!");
                return;
            }

            PathTestNpc previousNpc = client.Player.TempProperties.GetProperty<PathTestNpc>(TEMP_PATH_TEST_NPC);
            previousNpc?.Delete();

            short testSpeed = 350;
            PathPoint testPath = ClonePathForTest(first, testSpeed);
            if (testPath == null)
            {
                DisplayMessage(client, "Current path has no pathpoints!");
                return;
            }

            PathTestNpc npc = new()
            {
                Name = "Path Test Runner",
                GuildName = "Temporary path test",
                CurrentRegion = client.Player.CurrentRegion,
                X = testPath.X,
                Y = testPath.Y,
                Z = testPath.Z,
                Heading = GetPathHeading(testPath),
                Realm = eRealm.None,
                Level = 1,
                Model = 408,
                Size = 50,
                Flags = GameNPC.eFlags.PEACE,
                MaxSpeedBase = testSpeed,
                RespawnInterval = 0,
                CurrentPathPoint = testPath
            };

            if (!npc.AddToWorld())
            {
                DisplayMessage(client, "Failed to spawn path test NPC.");
                return;
            }

            client.Player.TempProperties.SetProperty(TEMP_PATH_TEST_NPC, npc);
            npc.StartPathTest(testSpeed);
            DisplayMessage(client, "Path test NPC spawned at step 1. It will die 10 seconds after reaching the end.");
        }

        private void PathStop(GameClient client)
        {
            if (client.Player.TargetObject is not GameNPC targetNpc)
            {
                DisplayMessage(client, "You need to select a mob first!");
                return;
            }

            // clear any current path
            targetNpc.CurrentPathPoint = null;
            targetNpc.ReturnToSpawnPoint(targetNpc.MaxSpeed);
            DisplayMessage(client, $"{client.Player.TargetObject.Name} told to walk to spawn!");
        }

        private void PathType(GameClient client, string[] args)
        {
            PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST);
            if (args.Length < 3)
            {
                DisplayMessage(client, "Usage: /path type <pathtype>");
                if (path != null)
                    DisplayMessage(client, $"Current path type is '{path.Type}'");
                DisplayMessage(client, "Possible pathtype values are:");
                DisplayMessage(client, string.Join(", ", Enum.GetNames<EPathType>()));
                return;
            }

            if (path == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create or /path load first!");
                return;
            }

            EPathType pathType;
            try
            {
                pathType = Enum.Parse<EPathType>(args[2], true);
            }
            catch
            {
                DisplayMessage(client, "Usage: /path type <pathtype>");
                DisplayMessage(client, $"Current path type is '{path.Type}'");
                DisplayMessage(client, "PathType must be one of the following:");
                DisplayMessage(client, string.Join(", ", Enum.GetNames<EPathType>()));
                return;
            }

            path.Type = pathType;
            PathPoint temp = path.Prev;

            while ((temp != null) && (temp != path))
            {
                temp.Type = pathType;
                temp = temp.Prev;
            }

            DisplayMessage(client, $"Current path type set to '{path.Type}'");
        }

        private void PathLoad(GameClient client, string[] args)
        {
            if (args.Length < 3)
            {
                DisplayMessage(client, "Usage: /path load <pathName>");
                return;
            }

            string pathName = string.Join(" ", args, 2, args.Length - 2);
            PathPoint pathPoint = MovementMgr.LoadPath(pathName);

            if (pathPoint == null)
            {
                DisplayMessage(client, $"Path '{pathName}' not found!");
                return;
            }

            RemoveAllPathPointObjects(client);
            DisplayMessage(client, $"Path '{pathName}' loaded.");
            client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, pathPoint);
            client.Player.TempProperties.SetProperty(TEMP_PATH_NAME, pathName);
            int len = 0;
            PathPoint lastPathPoint;

            do
            {
                lastPathPoint = pathPoint;
                CreatePathPointObject(client, pathPoint, ++len);
                pathPoint = pathPoint.Next;
            } while (pathPoint != null);

            client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, lastPathPoint);
        }

        private void PathLoadTarget(GameClient client)
        {
            if (client.Player.TargetObject is not GameNPC npc)
            {
                DisplayMessage(client, "You need to select a mob first!");
                return;
            }

            GameNPC pathNpc = npc;

            PathPoint pathPoint = pathNpc.CurrentPathPoint;
            if (pathPoint == null)
            {
                DisplayMessage(client, $"{pathNpc.Name} is not currently using a path!");
                return;
            }

            PathPoint firstPathPoint = MovementMgr.FindFirstPathPoint(pathPoint);
            if (firstPathPoint == null)
            {
                DisplayMessage(client, $"Failed to find the first pathpoint for {pathNpc.Name}'s current path.");
                return;
            }

            RemoveAllPathPointObjects(client);
            client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, firstPathPoint);
            client.Player.TempProperties.SetProperty(TEMP_PATH_NAME, string.IsNullOrEmpty(pathNpc.PathID) ? null : pathNpc.PathID);

            int len = RecreatePathPointObjects(client, firstPathPoint);
            PathPoint lastPathPoint = GetPathPointByStep(firstPathPoint, len);
            client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, lastPathPoint);

            if (pathNpc == npc)
                DisplayMessage(client, $"Loaded {npc.Name}'s current path. Current path length = {len}");
            else
                DisplayMessage(client, $"Loaded {npc.Name}'s leader {pathNpc.Name}'s current path. Current path length = {len}");
        }

        private void PathSave(GameClient client, string[] args)
        {
            PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST);
            if (path == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create first!");
                return;
            }

            string pathName = args.Length >= 3 ? string.Join(" ", args, 2, args.Length - 2) : client.Player.TempProperties.GetProperty<string>(TEMP_PATH_NAME);
            if (string.IsNullOrEmpty(pathName))
            {
                DisplayMessage(client, "Usage: /path save <pathName>");
                DisplayMessage(client, "No loaded path name is available for /path save.");
                return;
            }

            MovementMgr.SavePath(pathName, path);
            client.Player.TempProperties.SetProperty(TEMP_PATH_NAME, pathName);
            DisplayMessage(client, $"Path saved as '{pathName}'");
        }

        private void DisplayPathInfo(GameClient client)
        {
            PathPoint first = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_FIRST);
            string pathName = client.Player.TempProperties.GetProperty<string>(TEMP_PATH_NAME);

            if (first == null)
            {
                DisplayMessage(client, "No temporary path loaded.");

                if (client.Player.TargetObject is GameNPC target)
                {
                    GameNPC pathNpc = target;

                    string targetPathName = string.IsNullOrEmpty(pathNpc.PathID) ? "none" : pathNpc.PathID;
                    string currentPath = pathNpc.CurrentPathPoint == null ? "not moving on a path" : "currently has a pathpoint";

                    if (pathNpc == target)
                        DisplayMessage(client, $"Target {target.Name}: PathID={targetPathName}, {currentPath}.");
                    else
                        DisplayMessage(client, $"Target {target.Name} uses leader {pathNpc.Name}: PathID={targetPathName}, {currentPath}.");
                }

                DisplaySyntax(client);
                return;
            }

            PathPoint root = MovementMgr.FindFirstPathPoint(first) ?? first;
            int len = CountPathPoints(root, out PathPoint last);
            string loadedName = string.IsNullOrEmpty(pathName) ? "unsaved temporary path" : pathName;

            DisplayMessage(client, $"Current path: {loadedName}");
            DisplayMessage(client, $"Pathpoints: {len}, Type: {root.Type}");
            DisplayMessage(client, $"Start: {root.X}, {root.Y}, {root.Z}");

            if (last != null && last != root)
                DisplayMessage(client, $"End: {last.X}, {last.Y}, {last.Z}");

            if (!string.IsNullOrEmpty(pathName))
                DisplayMessage(client, $"Use /path save to save back to '{pathName}'.");
            else
                DisplayMessage(client, "Use /path save <pathName> to save this path.");

            DisplaySyntax(client);
        }

        private void PathAssignTaxiRoute(GameClient client, string[] args)
        {
            PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST);

            if (args.Length < 3)
            {
                DisplayMessage(client, "Usage: /path assigntaxiroute <destination>");
                return;
            }

            if (path == null)
            {
                DisplayMessage(client, "No path created yet! Use /path create first!");
                return;
            }

            GameMerchant merchant = null;
            if (client.Player.TargetObject is GameStableMaster)
                merchant = client.Player.TargetObject as GameStableMaster;

            if (client.Player.TargetObject is GameBoatStableMaster)
                merchant = client.Player.TargetObject as GameBoatStableMaster;

            if (merchant == null)
            {
                DisplayMessage(client, "You must select a stable master to assign a taxi route!");
                return;
            }

            string target = string.Join(" ", args, 2, args.Length - 2);
            bool ticketFound = false;
            string ticket = $"Ticket to {target}";

            // With the new horse system, the stablemasters are using the item.Id_nb to find the horse route in the database
            // So we have to save a path in the database with the Id_nb as a PathID
            // The following string will contain the item Id_nb if it is found in the merchant list
            string pathName = string.Empty;
            if (merchant.TradeItems != null)
            {
                foreach (DbItemTemplate template in merchant.TradeItems.GetAllItems().Values)
                {
                    if (template != null && template.Name.Equals(ticket, StringComparison.OrdinalIgnoreCase))
                    {
                        ticketFound = true;
                        pathName = template.Id_nb;
                        break;
                    }
                }
            }

            if (!ticketFound)
            {
                DisplayMessage(client, $"Stablemaster has no {ticket}!");
                return;
            }

            MovementMgr.SavePath(pathName, path);
            DisplayMessage(client, $"Taxi route set to path '{pathName}'!");
        }

        private void TogglePathVisualization(GameClient client)
        {
            GameNPC npc = GetPathVisualizationNpc(client);
            if (npc == null)
                return;

            client.Player.TempProperties.SetProperty(TEMP_PATH_VISUALIZATION_NPC, npc);
            npc.movementComponent.TogglePathVisualization();
            if (client.Player.TargetObject == npc)
                DisplayMessage(client, $"Toggling path visualization for {npc.Name} ({npc.ObjectID})");
            else
                DisplayMessage(client, $"Toggling path visualization for {client.Player.TargetObject.Name}'s leader {npc.Name} ({npc.ObjectID})");
        }

        private void RefreshPathVisualization(GameClient client)
        {
            GameNPC npc = GetPathVisualizationNpc(client);
            if (npc == null)
                return;

            if (npc.movementComponent.IsPathVisualizationActive)
                npc.movementComponent.TogglePathVisualization();

            npc.movementComponent.TogglePathVisualization();
            DisplayMessage(client, $"Refreshing path visualization for {npc.Name} ({npc.ObjectID})");
        }

        private static GameNPC GetPathVisualizationNpc(GameClient client)
        {
            if (client.Player.TargetObject is not GameNPC npc)
                return client.Player.TempProperties.GetProperty<GameNPC>(TEMP_PATH_VISUALIZATION_NPC);

            return npc;
        }

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplayPathInfo(client);
                return;
            }

            switch (args[1].ToLower())
            {
                case "create":
                {
                    PathCreate(client, args);
                    break;
                }
                case "add":
                {
                    PathAdd(client, args);
                    break;
                }
                case "insert":
                {
                    PathInsert(client, args);
                    break;
                }
                case "discardpoint":
                {
                    PathDiscardPoint(client);
                    break;
                }
                case "edit":
                {
                    PathEdit(client, args);
                    break;
                }
                case "travel":
                {
                    PathTravel(client);
                    break;
                }
                case "test":
                {
                    PathTest(client);
                    break;
                }
                case "stop":
                {
                    PathStop(client);
                    break;
                }
                case "type":
                {
                    PathType(client, args);
                    break;
                }
                case "save":
                {
                    PathSave(client, args);
                    break;
                }
                case "load":
                {
                    PathLoad(client, args);
                    break;
                }
                case "loadtarget":
                {
                    PathLoadTarget(client);
                    break;
                }
                case "assigntaxiroute":
                {
                    PathAssignTaxiRoute(client, args);
                    break;
                }
                case "hide":
                {
                    PathHide(client);
                    break;
                }
                case "delete":
                {
                    RemoveAllPathPointObjects(client);
                    break;
                }
                case "visualize":
                {
                    TogglePathVisualization(client);
                    break;
                }
                default:
                {
                    DisplaySyntax(client);
                    break;
                }
            }
        }

        private class PathTestNpc : GameNPC
        {
            private ECSGameTimer _pathEndCheckTimer;
            private ECSGameTimer _deathTimer;

            public PathTestNpc() : base(new BlankBrain()) { }

            public void StartPathTest(short speed)
            {
                MoveOnPath(speed);
                _pathEndCheckTimer = new(this, CheckPathEnd, 500);
            }

            public override void Delete()
            {
                _pathEndCheckTimer?.Stop();
                _deathTimer?.Stop();
                base.Delete();
            }

            private int CheckPathEnd(ECSGameTimer timer)
            {
                if (ObjectState is not eObjectState.Active || !IsAlive)
                    return 0;

                if (IsMovingOnPath || CurrentPathPoint != null)
                    return 500;

                _deathTimer = new(this, KillPathTestNpc, 10000);
                Say("Path test complete.");
                return 0;
            }

            private int KillPathTestNpc(ECSGameTimer timer)
            {
                if (ObjectState is eObjectState.Active && IsAlive)
                    Die(null);

                return 0;
            }
        }
    }
}
