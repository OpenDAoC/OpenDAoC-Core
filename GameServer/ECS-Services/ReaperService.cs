using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECS.Debug;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DOL.GS;

public class ReaperService
{
    private const string ServiceName = "Reaper Service";
    
    //primary key is living that needs to die, value is the object that killed it.
    //THIS SHOULD ONLY BE OPERATED ON BY THE TICK() FUNCTION! This keeps it thread safe
    private static Dictionary<GameLiving, GameObject> KilledToKillerDict;
    
    //this is our buffer of things to add to our list before Reaping
    //this can be added to by any number of threads
    private static Dictionary<GameLiving, GameObject> KillsToAdd;

    static ReaperService()
    {
        EntityManager.AddService(typeof(ReaperService));
        KilledToKillerDict = new Dictionary<GameLiving, GameObject>();
        KillsToAdd = new Dictionary<GameLiving, GameObject>();
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);

        //cant modify the KilledToKillerDict while iteration is in progress, so
        //make a list to store all the dead livings to remove afterwards
        List<GameLiving> DeadLivings = new List<GameLiving>();
        
        
        if (KillsToAdd != null && KillsToAdd.Count > 0)
        {
            lock (NewKillLock)
            {
                lock (KillerDictLock)
                {
                    Diagnostics.StartPerfCounter(ServiceName+"-KillsToAdd");
                    foreach (var newDeath in KillsToAdd)
                    {
                        if (!KilledToKillerDict.Keys.Contains(newDeath.Key))
                            KilledToKillerDict.Add(newDeath.Key, newDeath.Value);
                        KillsToAdd.Remove(newDeath.Key);
                    }
                    KillsToAdd.Clear();
                    Diagnostics.StopPerfCounter(ServiceName+"-KillsToAdd");
                }
            }
        }
        
        
        
        if (KilledToKillerDict.Keys.Count > 0)
        {
            Diagnostics.StartPerfCounter(ServiceName+"-ProcessKills");
            //kill everything on multiple threads
            Parallel.ForEach(KilledToKillerDict, killed =>
            {
                killed.Key.ProcessDeath(killed.Value);
                DeadLivings.Add(killed.Key);
            });

            Diagnostics.StopPerfCounter(ServiceName+"-ProcessKills");
            Diagnostics.StartPerfCounter(ServiceName+"-RemoveKills");
            lock (KillerDictLock)
            {
                //remove everything we killed
                foreach (var deadLiving in DeadLivings)
                {
                    if(deadLiving != null && KilledToKillerDict.Keys.Contains(deadLiving))
                        KilledToKillerDict.Remove(deadLiving);
                }
            }
            Diagnostics.StopPerfCounter(ServiceName+"-RemoveKills");
        }
        
        

        Diagnostics.StopPerfCounter(ServiceName);
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