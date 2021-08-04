using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class DamageComponent
    {
        //entity taking the damage
        public GameLiving owner;

        public GameLiving lastEntityToDamageThisEntity;

        public int DamageToDeal = 0;

        //might be better moved to DamageOverTimeComponent
        public bool isRepeating = false;
        public int damageIntervalInMs = 0;

        public DamageComponent(GameLiving owner)
        {
            this.owner = owner;
        }

    }
}
