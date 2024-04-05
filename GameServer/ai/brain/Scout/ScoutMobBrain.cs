using System;
using DOL.GS;

namespace DOL.AI.Brain
{
    /// <summary>
    /// Brain for scout mobs. Scout mobs are NPCs that will not aggro
    /// on a player of their own accord, instead, they'll go searching
    /// for adds around the area and make those aggro on a player.
    /// </summary>
    class ScoutMobBrain : StandardMobBrain
    {
        public bool IsScoutingInterrupted { get; set; } // Will be reset by `ScoutMobState_AGGRO`.
        public override int AggroRange => Math.Max(_aggroRange, 1000);
        public override bool CanBAF => false;

        public ScoutMobBrain() : base()
        {
            FSM.Add(new ScoutMobState_AGGRO(this));
            FSM.Add(new ScoutMobState_ROAMING(this));
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            IsScoutingInterrupted = true;
            base.OnAttackedByEnemy(ad);
        }

        public override void AttackMostWanted()
        {
            IsScoutingInterrupted = true;
            base.AttackMostWanted();
        }
    }
}
