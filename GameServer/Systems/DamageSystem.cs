using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Systems
{
    class DamageSystem
    {
        public void OnTick(GameLiving[] entities)
        {
            foreach(GameLiving e in entities)
            {
                //only operate on entities needing to take damage
                //in the future this check will probably be if a damage component exists rather than a value check
                if(e.damageComponent?.DamageToDeal == 0)
                {
                    continue;
                }

                HealthComponent hp = e.healthComponent;
                DamageComponent dmg = e.damageComponent;

                hp.Health -= dmg.DamageToDeal;
                dmg.DamageToDeal = 0;
                Console.WriteLine("Dealing " + dmg.DamageToDeal.ToString() + " damage to entity: " + e.ToString());
            }
        }
    }
}
