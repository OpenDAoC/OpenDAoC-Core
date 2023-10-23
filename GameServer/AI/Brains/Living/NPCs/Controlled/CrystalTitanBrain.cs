using System.Collections;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI
{
    public class CrystalTitanBrain : ControlledNpcBrain
    {
        private GameLiving m_target;

        public CrystalTitanBrain(GameLiving owner)
            : base(owner)
        {
        }

        public GameLiving Target
        {
            get { return m_target; }
            set { m_target = value; }
        }

        #region AI

        public override bool Start()
        {
            if (!base.Start()) return false;
            return true;
        }

        public override bool Stop()
        {
            if (!base.Stop()) return false;
            return true;
        }

        private IList FindTarget()
        {
            ArrayList list = new ArrayList();

            foreach (GamePlayer o in Body.GetPlayersInRadius((ushort)Body.AttackRange))
            {
                GamePlayer p = o as GamePlayer;

                if (GameServer.ServerRules.IsAllowedToAttack(Body, p, true))
                    list.Add(p);
            }
            return list;
        }

        public override void Think()
        {
            if (Body.TargetObject is GameNpc)
                Body.TargetObject = null;

            if (Body.attackComponent.AttackState)
                return;

            IList enemies = new ArrayList();
            if (Target == null)
                enemies = FindTarget();
            else if (!Body.IsWithinRadius(Target, Body.AttackRange))
                enemies = FindTarget();
            else if (!Target.IsAlive)
                enemies = FindTarget();
            if (enemies.Count > 0 && Target == null)
            {
                //pick a random target...
                int targetnum = Util.Random(0, enemies.Count - 1);

                //Choose a random target.
                Target = enemies[targetnum] as GameLiving;
            }
            else if (enemies.Count < 1)
            {
                WalkState = EWalkState.Stay;
                enemies = FindTarget();
            }

            if (Target != null)
            {
                if (!Target.IsAlive)
                {
                    Target = null;
                }
                else if (Body.IsWithinRadius(Target, Body.AttackRange))
                {
                    Body.TargetObject = Target;
                    Goto(Target);
                    Body.StartAttack(Target);
                }
                else
                {
                    Target = null;
                }
            }
        }
        #endregion
    }
}