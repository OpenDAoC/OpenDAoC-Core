using DOL.AI.Brain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECS.Debug;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.Buffers;
using log4net;
using System.Reflection;

namespace DOL.GS
{
    public class TaskStats
    {
        public long CreationTime;
        public int Name;
        public int ThreadNum;
        public int Completed;
        public int Unthinking;
    }

    public static class NPCThinkService
    {
        static int _segmentsize = 5000;
        static List<Task> _tasks = new List<Task>();
        static int completed;
        static int unthinking;
        static long interval = 2000;
        static long last_interval = 0;

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string ServiceName = "NPCThinkService";
        
        //thinkTimer is for outputting active brain count/array size info for debug purposes
        public static bool thinkTimer = false;
        
        //Number of ticks to debug the Timer
        public static int ActiveThinkTimerTickCount = 0;
        public static int NumOfNPCs = 0;
        public static int NumNullSlots = 0;
        
        
        //Number of ticks to debug the Timer
        public static int debugTimerTickCount = 0;

        static NPCThinkService()
        {
            EntityManager.AddService(typeof(NPCThinkService));
        }

        public static void Tick(long tick)
        {

            Diagnostics.StartPerfCounter(ServiceName);

            GameLiving[] arr = EntityManager.GetAllNpcsArrayRef();
            
            if (thinkTimer)
            {
                ActiveThinkTimerTickCount = 0;
                NumOfNPCs = 0;
                NumNullSlots = 0;
            }

            Parallel.ForEach(arr, npc =>
            {
                try
                {
                    if (npc == null)
                    {
                        if(thinkTimer)
                            Interlocked.Increment(ref NumNullSlots);;
                        
                        return;
                    }

                    if (thinkTimer)
                    {
                        Interlocked.Increment(ref NumOfNPCs);
                    }
                    
                    
                    if (npc is GameNPC && (npc as GameNPC).Brain != null)
                    {
                        var brain = (npc as GameNPC).Brain;

                        if (brain.IsActive && brain.LastThinkTick + brain.ThinkInterval < tick)
                        {
                            if (thinkTimer)
                            {
                                Interlocked.Increment(ref ActiveThinkTimerTickCount);
                            }
                            
                            long startTick = GameTimer.GetTickCount();
                            brain.Think();
                            long stopTick = GameTimer.GetTickCount();
                            if((stopTick - startTick)  > 25 && brain != null)
                                log.Warn($"Long NPCThink for {brain.Body?.Name}({brain.Body?.ObjectID}) Interval: {brain.ThinkInterval} BrainType: {brain.GetType().ToString()} Time: {stopTick - startTick}ms");

                            //Set the LastThinkTick. Offset the LastThinkTick interval for non-controlled mobs so NPC Think ticks are not all "grouped" in one tick.
                            if(brain is ControlledNpcBrain)
                                brain.LastThinkTick = tick; //We wamt controlled pets to keep their normal interval.
                            else
                                brain.LastThinkTick = tick + (Util.Random(10) * 50); //Offsets the LastThinkTick by 0-500ms

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
            
            //Output Debug info
            if(thinkTimer && ActiveThinkTimerTickCount > 0)
            {
                log.Debug($"==== NPCThink Debug - Total ActiveThinkTimers: {ActiveThinkTimerTickCount} ====");

                log.Debug($"==== Non-Null NPCs in EntityManager Array: {NumOfNPCs} | Null NPCs: {NumNullSlots} |  Total Size: {arr.Length}====");
                
             

                log.Debug("---------------------------------------------------------------------------");

                if(debugTimerTickCount > 1)
                    debugTimerTickCount --;
                else
                {
                    thinkTimer = false;
                    debugTimerTickCount = 0;
                }
            }

            Diagnostics.StopPerfCounter(ServiceName);
        }

    }
}
