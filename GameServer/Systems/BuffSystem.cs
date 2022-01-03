using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Systems { 
    public class BuffSystem
    {
        public void OnTick(GameLiving[] entities)
        {
            foreach(var entity in entities)
            {
                //idea is to iterate through all StatBuffComponents on the entity,
                //if any StatBuffComponents isApplied=false, apply their effects 
                //  to the entity and set isApplied=true
                //else, increment the buff timers

                //needs to be able to handle multiple StatBuffComponents on entity
                // if (entity.buffComponent.isApplied)
                // {
                //     entity.buffComponent.UpdateTimeLeft();
                //     if(entity.buffComponent.timeSinceApplication > entity.buffComponent.maxDuration)
                //     {
                //         entity.statsComponent.DecreaseStat(entity.buffComponent.statToModify, entity.buffComponent.buffValue);
                //         entity.buffComponent.isApplied = false;
                //     }
                // } else
                // {
                //     entity.statsComponent.IncreaseStat(entity.buffComponent.statToModify, entity.buffComponent.buffValue);
                //     entity.buffComponent.isApplied = true;
                //     entity.buffComponent.UpdateTimeLeft();
                // }
            }
        }
    }
}
