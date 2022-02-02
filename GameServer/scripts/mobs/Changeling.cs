using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class Changeling : GameNPC
    {

        public Changeling() : base()
        {
        }
        
        public override bool AddToWorld()
        {
            var brain = new ChangelingBrain();
            SetOwnBrain(brain);
            return base.AddToWorld();
        }
        
    }
}

namespace DOL.AI.Brain
{
    public class ChangelingBrain : StandardMobBrain
    {
        public ChangelingBrain()
            : base()
        {

        }

        public void SetModel()
        {
            switch (Util.Random(5))
            {
                case 0:
                    Body.Model = 69; // small goblin whelp
                    Body.Size = 35;
                    break;
                case 1:
                    Body.Model = 124; // mud creature
                    Body.Size = 35;
                    break;
                case 2:
                    Body.Model = 391; // vendo
                    Body.Size = 35;
                    break;
                case 3:
                    Body.Model = 47; // small grey wolf
                    Body.Size = 30;
                    break;
                case 4:
                    Body.Model = 123; // worm
                    Body.Size = 35;
                    break;
                case 5:
                    Body.Model = 112; // brownie
                    Body.Size = 35;
                    break;
            }
        }

        public override void Think()
        {
            base.Think();
            SetModel();
        }

    }
}