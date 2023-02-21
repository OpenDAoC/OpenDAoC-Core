using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECS.Debug;

namespace DOL.GS;

public class ReaperService
{
    private const string SERVICE_NAME = "Reaper Service";

    //primary key is living that needs to die, value is the object that killed it.
    //THIS SHOULD ONLY BE OPERATED ON BY THE TICK() FUNCTION! This keeps it thread safe
    private static Dictionary<GameLiving, GameObject> KilledToKillerDict;
    
    //this is our buffer of things to add to our list before Reaping
    //this can be added to by any number of threads
    private static Dictionary<GameLiving, GameObject> KillsToAdd;

    static ReaperService()
    {
        KilledToKillerDict = new Dictionary<GameLiving, GameObject>();
        KillsToAdd = new Dictionary<GameLiving, GameObject>();
    }

    public static void Tick()
    {
        Diagnostics.StartPerfCounter(SERVICE_NAME);

        //cant modify the KilledToKillerDict while iteration is in progress, so
        //make a list to store all the dead livings to remove afterwards
        List<GameLiving> DeadLivings = new List<GameLiving>();

        if (KillsToAdd != null && KillsToAdd.Count > 0)
        {
            lock (NewKillLock)
            {
                lock (KillerDictLock)
                {
                    Diagnostics.StartPerfCounter(SERVICE_NAME+"-KillsToAdd");
                    foreach (var newDeath in KillsToAdd)
                    {
                        if (!KilledToKillerDict.ContainsKey(newDeath.Key))
                            KilledToKillerDict.Add(newDeath.Key, newDeath.Value);
                    }

                    KillsToAdd.Clear();
                    Diagnostics.StopPerfCounter(SERVICE_NAME+"-KillsToAdd");
                }
            }
        }

        if (KilledToKillerDict.Keys.Count > 0)
        {
            int killsToProcess = KilledToKillerDict.Keys.Count;
            Diagnostics.StartPerfCounter(SERVICE_NAME+"-ProcessDeaths("+killsToProcess+")");
            //kill everything on multiple threads
            Parallel.ForEach(KilledToKillerDict, killed =>
            {
                killed.Key.ProcessDeath(killed.Value);
                //Console.WriteLine($"Dead or Dying set to {killed.Key.isDeadOrDying} for {killed.Key.Name} in reaper");
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME+"-ProcessDeaths("+killsToProcess+")");
            Diagnostics.StartPerfCounter(SERVICE_NAME+"-RemoveKills");

            lock (KillerDictLock)
            {
                //remove everything we killed
                foreach (var deadLiving in KilledToKillerDict.Keys.ToList().Where(x=> x.isDeadOrDying == false))
                {
                    KilledToKillerDict.Remove(deadLiving);
                }
            }
            Diagnostics.StopPerfCounter(SERVICE_NAME+"-RemoveKills");
        }

        Diagnostics.StopPerfCounter(SERVICE_NAME);
    }

    public static object KillerDictLock = new object();
    public static object NewKillLock = new object();

    public static void KillLiving(GameLiving living, GameObject killer)
    {
        lock (NewKillLock)
        {
            if(KillsToAdd != null && living != null && !KillsToAdd.ContainsKey(living))
                KillsToAdd.Add(living, killer);
        }
    }
}