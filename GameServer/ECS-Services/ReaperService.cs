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
    
    private static Dictionary<GameLiving, GameObject> KilledToKillerDict; //primary key is living that needs to die, value is the object that killed it

    static ReaperService()
    {
        EntityManager.AddService(typeof(ReaperService));
        KilledToKillerDict = new Dictionary<GameLiving, GameObject>();
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);

        //cant modify the KilledToKillerDict while iteration is in progress, so
        //make a list to store all the dead livings to remove afterwards
        List<GameLiving> DeadLivings = new List<GameLiving>();

        if (KilledToKillerDict.Keys.Count > 0)
        {
            //kill everything on multiple threads
            Parallel.ForEach(KilledToKillerDict, killed =>
            {
                killed.Key.ProcessDeath(killed.Value);
                DeadLivings.Add(killed.Key);
            });

            //remove everything we killed
            foreach (var deadLiving in DeadLivings)
            {
                KilledToKillerDict.Remove(deadLiving);
            }
        }

        Diagnostics.StopPerfCounter(ServiceName);
    }

    public static void KillLiving(GameLiving living, GameObject killer)
    {
        
        if(KilledToKillerDict != null && !KilledToKillerDict.ContainsKey(living))
            KilledToKillerDict.Add(living, killer);
    }
}