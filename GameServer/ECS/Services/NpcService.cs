using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using log4net;

namespace Core.GS.ECS;

public static class NpcService
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private const string SERVICE_NAME = nameof(NpcService);

    private static int _nonNullBrainCount;
    private static int _nullBrainCount;

    public static int DebugTickCount { get; set; } // Will print active brain count/array size info for debug purposes if superior to 0.
    private static bool Debug => DebugTickCount > 0;

    public static void Tick(long tick)
    {
        GameLoopMgr.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME);

        if (Debug)
        {
            _nonNullBrainCount = 0;
            _nullBrainCount = 0;
        }

        List<ABrain> list = EntityMgr.UpdateAndGetAll<ABrain>(EEntityType.Brain, out int lastValidIndex);

        Parallel.For(0, lastValidIndex + 1, i =>
        {
            ABrain brain = list[i];

            if (brain?.EntityManagerId.IsSet != true)
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

                    long startTick = GameLoopMgr.GetCurrentTime();
                    brain.Think();
                    long stopTick = GameLoopMgr.GetCurrentTime();

                    if (stopTick - startTick > 25)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {npc.Name}({npc.ObjectID}) Interval: {brain.ThinkInterval} BrainType: {brain.GetType()} Time: {stopTick - startTick}ms");

                    brain.LastThinkTick = tick;

                    // Offset LastThinkTick for non-controlled mobs so that 'Think' ticks are not all "grouped" in one server tick.
                    if (brain is not ControlledNpcBrain)
                        brain.LastThinkTick += Util.Random(-2, 2) * GameLoopMgr.TICK_RATE;
                }

                npc.movementComponent.Tick(tick);

                if (npc.NeedsBroadcastUpdate)
                {
                    ClientService.UpdateObjectForPlayers(npc);
                    npc.NeedsBroadcastUpdate = false;
                }
            }
            catch (Exception e)
            {
                ServiceUtil.HandleServiceException(e, SERVICE_NAME, brain, brain.Body);
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