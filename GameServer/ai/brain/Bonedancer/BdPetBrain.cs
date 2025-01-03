using System;
using DOL.GS;

namespace DOL.AI.Brain
{
    public abstract class BdPetBrain : ControlledMobBrain
    {
        protected const int BASEFORMATIONDIST = 50;

        public BdPetBrain(GameLiving Owner) : base(Owner)
        {
            IsMainPet = false;
        }

        /// <summary>
        /// Are minions assisting the commander?
        /// </summary>
        public bool MinionsAssisting => Owner is CommanderPet commander && commander.MinionsAssisting;

        public override void SetAggressionState(eAggressionState state)
        {
            if (MinionsAssisting)
                base.SetAggressionState(state);
            else
                base.SetAggressionState(eAggressionState.Passive);

            // Attack immediately rather than waiting for the next Think()
            if (AggressionState is not eAggressionState.Passive)
                Attack(Owner.TargetObject);
        }

        /// <summary>
        /// This method is called at the end of the attack sequence to
        /// notify objects if they have been attacked/hit by an attack
        /// </summary>
        /// <param name="ad">information about the attack</param>
        public override void OnAttackedByEnemy(AttackData ad)
        {
            // Any attack on a subpet is handled as if it was the commander that was attacker.
            // This will propagate the event to every subpet.
            if (ad.CausesCombat && Owner is CommanderPet owner && owner.Brain is CommanderBrain ownerBrain)
                ownerBrain.OnAttackedByEnemy(ad);
        }

        /// <summary>
        /// Updates the pet window
        /// </summary>
        public override void UpdatePetWindow() { }

        /// <summary>
        /// Start following the owner
        /// </summary>
        public override void FollowOwner()
        {
            if (Body.IsAttacking)
                Disengage();

            Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
        }

        /// <summary>
        /// Checks for the formation position of the BD pet
        /// </summary>
        public override bool CheckFormation(ref int x, ref int y, ref int z)
        {
            if (!Body.IsCasting && !Body.attackComponent.AttackState && Body.attackComponent.Attackers.IsEmpty)
            {
                GameNPC commander = (GameNPC)Owner;
                double heading = commander.Heading * Point2D.HEADING_TO_RADIAN;
                //Get which place we should put minion
                int i = 0;
                //How much do we want to slide back and left/right
                int perp_slide = 0;
                int par_slide = 0;

                for (; i < commander.ControlledNpcList.Length; i++)
                {
                    if (commander.ControlledNpcList[i] == this)
                        break;
                }

                switch (commander.Formation)
                {
                    case GameNPC.eFormationType.Triangle:
                        par_slide = BASEFORMATIONDIST;
                        perp_slide = BASEFORMATIONDIST;
                        if (i != 0)
                            par_slide = BASEFORMATIONDIST * 2;
                        break;
                    case GameNPC.eFormationType.Line:
                        par_slide = BASEFORMATIONDIST * (i + 1);
                        break;
                    case GameNPC.eFormationType.Protect:
                        switch (i)
                        {
                            case 0:
                                par_slide = -BASEFORMATIONDIST * 2;
                                break;
                            case 1:
                            case 2:
                                par_slide = -BASEFORMATIONDIST;
                                perp_slide = BASEFORMATIONDIST;
                                break;
                        }

                        break;
                }
                //Slide backwards - every pet will need to do this anyways
                x += (int)((double)commander.FormationSpacing * par_slide * Math.Cos(heading - Math.PI / 2));
                y += (int)((double)commander.FormationSpacing * par_slide * Math.Sin(heading - Math.PI / 2));

                //In addition with sliding backwards, slide the other two pets sideways
                switch (i)
                {
                    case 1:
                        x += (int)((double)commander.FormationSpacing * perp_slide * Math.Cos(heading - Math.PI));
                        y += (int)((double)commander.FormationSpacing * perp_slide * Math.Sin(heading - Math.PI));
                        break;
                    case 2:
                        x += (int)((double)commander.FormationSpacing * perp_slide * Math.Cos(heading));
                        y += (int)((double)commander.FormationSpacing * perp_slide * Math.Sin(heading));
                        break;
                }

                return true;
            }

            return false;
        }

        public override eWalkState WalkState
        {
            get => eWalkState.Follow;
            set { }
        }
    }
}
