using ECS.Debug;

namespace DOL.GS
{
    public class ConquestService
    {
        private const string SERVICE_NAME = "ConquestService";

        public static ConquestManager ConquestManager;
        private static long lastCheckTick;

        static ConquestService()
        {
            ConquestManager = new ConquestManager();
            ConquestManager.StartConquest();
        }

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            long fullCycle = ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION * 60000; //ServerProperties.Properties.MAX_CONQUEST_INTERVAL
            long tallyCycle = ServerProperties.Properties.CONQUEST_TALLY_INTERVAL * 1000; //multiply by 000 to accomodate for second input
            long subCycle = fullCycle / 6;

            var ActiveObjective = ConquestManager.ActiveObjective;

            if (ActiveObjective == null)
            {
                ConquestManager.ConquestTimeout();
                Diagnostics.StopPerfCounter(SERVICE_NAME);
                return;
            }

            if (ConquestManager.LastConquestStartTime + fullCycle < GameLoop.GameLoopTime)
                ConquestManager.BeginNextConquest(); //start a new conquest if we're due

            if (ActiveObjective.LastRolloverTick + tallyCycle < GameLoop.GameLoopTime)
                ConquestManager.ActiveObjective.DoPeriodicReward(); //award anyone who has participated so far

            if (ConquestManager.LastConquestWindowStart + subCycle < GameLoop.GameLoopTime)
                ConquestManager.ResetConquestWindow(); //clear participants of conquest + predator

            ConquestManager.ActiveObjective.CheckNearbyPlayers();

            lastCheckTick = GameLoop.GameLoopTime;
            //Console.WriteLine($"conquest heartbeat {GameLoop.GameLoopTime} countdown {GameLoop.GameLoopTime - (ConquestManager.LastTaskRolloverTick + ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION * 10000)}");

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        public static bool IsOverHalfwayDone()
        {
            long fullCycle = ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION * 60000; //ServerProperties.Properties.MAX_CONQUEST_INTERVAL
            return (ConquestManager.LastConquestStartTime + (fullCycle / 2)) < GameLoop.GameLoopTime;
        }

        public static long GetTicksUntilContributionReset()
        {
            return ConquestManager.LastConquestWindowStart + (ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION * 60000 / 6) - GameLoop.GameLoopTime;
        }

        public static long GetTicksUntilNextAward()
        {
            return ConquestManager.ActiveObjective.LastRolloverTick + ServerProperties.Properties.CONQUEST_TALLY_INTERVAL * 1000 - GameLoop.GameLoopTime;
        }
    }
}
