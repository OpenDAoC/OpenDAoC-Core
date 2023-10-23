using Core.GS.Enums;

namespace Core.GS.AI
{
    public class BotonidBrain : StandardMobBrain
    {
        public BotonidBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        private bool transformed;

        public override int ThinkInterval => 1000;

        public override void Think()
        {
            if (HasAggro)
            {
                if (!Body.IsWithinRadius(Body.TargetObject, 150)) return;
                if (transformed) return;
                foreach (GamePlayer player in Body.GetPlayersInRadius(400))
                {
                    player.Out.SendMessage("The lure dissapears and a scourgin jumps out at " + player.Name + ".",
                        EChatType.CT_Say,
                        EChatLoc.CL_ChatWindow);
                    Transform(transformed); // scourgin
                    transformed = true;
                }
            }
            else if (!Body.InCombatInLast(30 * 1000) && !HasAggro)
            {
                if (!transformed) return;
                Transform(transformed); //seedling
                transformed = false;
            }

            base.Think();
        }

        private void Transform(bool transformed)
        {
            if (transformed)
            {
                Body.Size = 9;
                Body.Model = 818;
                Body.Name = "botonid seedling";
            }
            else
            {
                Body.Size = 50;
                Body.Model = 914;
                Body.Name = "scourgin";
            }
        }
    }
}