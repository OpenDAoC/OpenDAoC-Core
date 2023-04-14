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
    public static class NPCThinkService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = "NPCThinkService";

        // Will print active brain count/array size info for debug purposes if superior to 0.
        public static int DebugTickCount;
        private static int _debugRemainingTicks;
        private static int _npcCount;
        private static int _nullNpcCount;

        private static bool Debug => DebugTickCount > 0;

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            if (Debug)
            {
                _debugRemainingTicks = 0;
                _npcCount = 0;
                _nullNpcCount = 0;
            }

            List<GameNPC> list = EntityManager.GetAll<GameNPC>(EntityManager.EntityType.Npc);

            Parallel.For(0, EntityManager.GetLastNonNullIndex(EntityManager.EntityType.Npc) + 1, i =>
            {
                GameNPC npc = list[i];

                try
                {
                    if (npc == null)
                    {
                        if (Debug)
                            Interlocked.Increment(ref _nullNpcCount);;

                        return;
                    }

                    if (Debug)
                        Interlocked.Increment(ref _npcCount);

                    ABrain brain = npc.Brain;

                    if (brain != null)
                    {
                        if (brain.IsActive && brain.LastThinkTick + brain.ThinkInterval < tick)
                        {
                            if (Debug)
                                Interlocked.Increment(ref _debugRemainingTicks);

                            long startTick = GameTimer.GetTickCount();
                            brain.Think();
                            long stopTick = GameTimer.GetTickCount();
                            if((stopTick - startTick)  > 25 && brain != null)
                                log.Warn($"Long NPCThink for {brain.Body?.Name}({brain.Body?.ObjectID}) Interval: {brain.ThinkInterval} BrainType: {brain.GetType()} Time: {stopTick - startTick}ms");

                            // Set the LastThinkTick. Offset the LastThinkTick interval for non-controlled mobs so NPC Think ticks are not all "grouped" in one tick.
                            if(brain is ControlledNpcBrain)
                                brain.LastThinkTick = tick; // We wamt controlled pets to keep their normal interval.
                            else
                                brain.LastThinkTick = tick + Util.Random(10) * 50; // Offsets the LastThinkTick by 0-500ms.

                        }

                        if (brain.Body is not {NeedsBroadcastUpdate: true}) return;
                        brain.Body.BroadcastUpdate();
                        brain.Body.NeedsBroadcastUpdate = false;
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in NPC Think: {e}");
                }
            });

            // Output debug info.
            if (Debug && _debugRemainingTicks > 0)
            {
                log.Debug($"==== NPCThink Debug - Total ActiveThinkTimers: {_debugRemainingTicks} ====");
                log.Debug($"==== Non-Null NPCs in EntityManager Array: {_npcCount} | Null NPCs: {_nullNpcCount} |  Total Size: {list.Count}====");
                log.Debug("---------------------------------------------------------------------------");

                if (_debugRemainingTicks > 1)
                    _debugRemainingTicks --;
                else
                    _debugRemainingTicks = 0;
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
