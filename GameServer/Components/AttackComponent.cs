using DOL.Database;
using DOL.Events;
using DOL.GS.Spells;
using DOL.GS.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameLiving;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class AttackComponent
    {
        public GameLiving owner;
        public WeaponAction weaponAction;
        public AttackAction attackAction;
        
        public AttackComponent(GameLiving owner)
        {
            this.owner = owner;
        }

        public void Tick(long time)
        {
            if (weaponAction != null)
            {
                if (weaponAction.AttackFinished)
                    weaponAction = null;
                else
                    weaponAction.Tick(time);
            }
            if (attackAction != null)
            {
                attackAction.Tick(time);
            }
        }
        public void ExecuteWeaponStyle(Style style)
        {
            //styleHandler = ScriptMgr.CreateStyleHandler(owner, spell, line);
            StyleProcessor.TryToUseStyle(owner, style);
        }
  
    }
}
