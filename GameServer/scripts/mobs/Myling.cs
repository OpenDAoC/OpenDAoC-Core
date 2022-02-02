using DOL.AI;

namespace DOL.GS.Scripts
{
    public class Myling : GameNPC
    {
        
        public Myling() : base() { }
        public Myling(ABrain defaultBrain) : base(defaultBrain) { }
        public Myling(INpcTemplate template) : base(template) { }
        
        public bool IsRevealed = false;

        protected const ushort mylingModel = 929;

        /// <summary>
        /// Starts a melee or ranged attack on a given target.
        /// </summary>
        /// <param name="attackTarget">The object to attack.</param>
        public override void StartAttack(GameObject attackTarget)
        {
            Reveal();
            attackComponent.StartAttack(attackTarget);
        }
        
        public override void StopAttack()
        {
            Flags = 0;
            if (Model != mylingModel)
            {
                Model = mylingModel;
                IsRevealed = false;
                BroadcastLivingEquipmentUpdate();
            }
            base.StopAttack();
        }

        /// <summary>
        /// Reveal the true Myling form.
        /// </summary>
        protected void Reveal()
        {

            Flags = eFlags.GHOST;

            if (!IsRevealed)
            {
                switch (Util.Random(8))
                {
                    case 0:
                        Model = 138; // troll male
                        break;
                    case 1:
                        Model = 148; // troll female
                        break;
                    case 2:
                        Model = 169; // kobold male
                        break;
                    case 3:
                        Model = 180; // kobold female
                        break;
                    case 4:
                        Model = 160; // norse male
                        break;
                    case 5:
                        Model = 162; // norse female
                        break;
                    case 6:
                        Model = 185; // dwarf male
                        break;
                    case 7:
                        Model = 200; // dwarf female
                        break;
                    case 8:
                        Model = 24; // skeleton
                        break;
                }
                IsRevealed = true;
            }
        }
    }
}
