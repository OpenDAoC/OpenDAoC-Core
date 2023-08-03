﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DOL.AI;
using DOL.AI.Brain;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class NpcService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = "NpcService";

        private static int _nonNullBrainCount;
        private static int _nullBrainCount;

        public static int DebugTickCount { get; set; } // Will print active brain count/array size info for debug purposes if superior to 0.
        private static bool Debug => DebugTickCount > 0;

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            if (Debug)
            {
                _nonNullBrainCount = 0;
                _nullBrainCount = 0;
            }

            List<ABrain> list = EntityMgr.UpdateAndGetAll<ABrain>(EntityMgr.EntityType.Brain, out int lastNonNullIndex);

            Parallel.For(0, lastNonNullIndex + 1, i =>
            {
                ABrain brain = list[i];

                if (brain == null)
                {
                    if (Debug)
                        Interlocked.Increment(ref _nullBrainCount);

                    return;
                }

                if (Debug)
                    Interlocked.Increment(ref _nonNullBrainCount);

                try
                {
                    GameNpc npc = brain.Body;

                    if (brain.LastThinkTick + brain.ThinkInterval < tick)
                    {
                        if (!brain.IsActive)
                        {
                            brain.Stop();
                            return;
                        }

                        long startTick = GameLoop.GetCurrentTime();
                        brain.Think();
                        long stopTick = GameLoop.GetCurrentTime();

                        if ((stopTick - startTick) > 25)
                            log.Warn($"Long Think for {npc.Name}({npc.ObjectID}) Interval: {brain.ThinkInterval} BrainType: {brain.GetType()} Time: {stopTick - startTick}ms");

                        brain.LastThinkTick = tick;

                        // Offset LastThinkTick for non-controlled mobs so that 'Think' ticks are not all "grouped" in one server tick.
                        if (brain is not ControlledNpcBrain)
                            brain.LastThinkTick += UtilCollection.Random(-2, 2) * GameLoop.TICK_RATE;
                    }

                    npc.movementComponent.Tick(tick);

                    if (npc.NeedsBroadcastUpdate)
                    {
                        npc.BroadcastUpdate();
                        npc.NeedsBroadcastUpdate = false;
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered: {e}");
                }
            });

            // Output debug info.
            if (Debug)
            {
                log.Debug($"==== Non-null NCs in EntityManager array: {_nonNullBrainCount} | Null NPCs: {_nullBrainCount} | Total size: {list.Count} ====");
                DebugTickCount--;
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}