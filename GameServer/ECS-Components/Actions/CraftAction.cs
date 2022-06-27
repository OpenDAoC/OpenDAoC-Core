using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameLiving;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    /// <summary>
    /// The attack action of this living
    /// </summary>
    public class CraftAction
    {
        private GameLiving owner;
        private int Interval;
        private long startTime;
        public long StartTime { get { return startTime; } set { startTime = value + GameLoop.GameLoopTime; } }

        /// <summary>
        /// Constructs a new attack action
        /// </summary>
        /// <param name="owner">The action source</param>
        public CraftAction(GameLiving owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Called on every timer tick
        /// </summary>
        public void Tick(long time)
        {

            if (time > StartTime)
            {
                //GameLiving owner = (GameLiving)m_actionSource;

                if (owner.IsMezzed || owner.IsStunned)
                {
                    Interval = 100;
                    return;
                }

                if (owner.IsCasting && !owner.CurrentSpellHandler.Spell.Uninterruptible)
                {
                    Interval = 100;
                    return;
                }

                if (!owner.craftComponent.CraftState)
                {
                    // AttackData ad = owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;
                    // owner.TempProperties.removeProperty(LAST_ATTACK_DATA);
                    // if (ad != null && ad.Target != null)
                    //     ad.Target.attackComponent.RemoveAttacker(owner);
                    //Stop();
                    owner.craftComponent.craftAction?.CleanupCraftAction();
                    return;
                }

            }
        }

        public void CleanupCraftAction()
        {
            owner.craftComponent.craftAction = null;
        }
    }
}
