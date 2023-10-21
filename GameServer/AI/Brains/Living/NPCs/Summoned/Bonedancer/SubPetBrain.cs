using System;
using Core.Events;
using Core.GS;

namespace Core.AI.Brain
{
    public abstract class SubPetBrain : ControlledNpcBrain
    {
        protected const int BASEFORMATIONDIST = 50;

        public SubPetBrain(GameLiving Owner) : base(Owner)
        {
            IsMainPet = false;
        }

        /// <summary>
        /// Are minions assisting the commander?
        /// </summary>
        public bool MinionsAssisting => Owner is CommanderPet commander && commander.MinionsAssisting;

        public override void OnOwnerAttacked(AttackData ad)
        {
            // react only on these attack results
            switch (ad.AttackResult)
            {
                case EAttackResult.Blocked:
                case EAttackResult.Evaded:
                case EAttackResult.Fumbled:
                case EAttackResult.HitStyle:
                case EAttackResult.HitUnstyled:
                case EAttackResult.Missed:
                case EAttackResult.Parried:
                    AddToAggroList(ad.Attacker, ad.Attacker.EffectiveLevel + ad.Damage + ad.CriticalDamage);
                    break;
            }

            if (FiniteStateMachine.GetState(EFSMStateType.AGGRO) != FiniteStateMachine.GetCurrentState()) { FiniteStateMachine.SetCurrentState(EFSMStateType.AGGRO); }
            AttackMostWanted();
        }

        public override void SetAggressionState(EAggressionState state)
        {
            if (MinionsAssisting)
                base.SetAggressionState(state);
            else
                base.SetAggressionState(EAggressionState.Passive);

            // Attack immediately rather than waiting for the next Think()
            if (AggressionState != EAggressionState.Passive)
                Attack(Owner.TargetObject);
        }

        /// <summary>
        /// This method is called at the end of the attack sequence to
        /// notify objects if they have been attacked/hit by an attack
        /// </summary>
        /// <param name="ad">information about the attack</param>
        public override void OnAttackedByEnemy(AttackData ad)
        {
            base.OnAttackedByEnemy(ad);

            // Get help from the commander and other minions
            if (ad.CausesCombat && Owner is GameSummonedPet own && own.Brain is CommanderPetBrain ownBrain)
                ownBrain.DefendMinion(ad.Attacker);
        }

        /// <summary>
        /// Updates the pet window
        /// </summary>
        public override void UpdatePetWindow() { }

        /// <summary>
        /// Stops the brain thinking
        /// </summary>
        /// <returns>true if stopped</returns>
        public override bool Stop()
        {
            if (!base.Stop())
                return false;

            GameEventMgr.Notify(GameLivingEvent.PetReleased, Body);
            return true;
        }

        /// <summary>
        /// Start following the owner
        /// </summary>
        public override void FollowOwner()
        {
            if (Body.attackComponent.AttackState)
                Body.StopAttack();

            Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
        }

        /// <summary>
        /// Checks for the formation position of the BD pet
        /// </summary>
        public override bool CheckFormation(ref int x, ref int y, ref int z)
        {
            if (!Body.IsCasting && !Body.attackComponent.AttackState && Body.attackComponent.Attackers.IsEmpty)
            {
                GameNpc commander = (GameNpc)Owner;
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
                    case EPetFormationType.Triangle:
                        par_slide = BASEFORMATIONDIST;
                        perp_slide = BASEFORMATIONDIST;
                        if (i != 0)
                            par_slide = BASEFORMATIONDIST * 2;
                        break;
                    case EPetFormationType.Line:
                        par_slide = BASEFORMATIONDIST * (i + 1);
                        break;
                    case EPetFormationType.Protect:
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

        /// <summary>
        /// Lost follow target event
        /// </summary>
        /// <param name="target"></param>
        protected override void OnFollowLostTarget(GameObject target)
        {
            if (target == Owner)
            {
                GameEventMgr.Notify(GameLivingEvent.PetReleased, Body);
                return;
            }

            FollowOwner();
        }

        /// <summary>
        /// Standard think method for all the pets
        /// </summary>
        public override void Think()
        {
            CheckAbilities();
            CheckSpells(ECheckSpellType.Defensive);
            base.Think();
        }

        public override void Attack(GameObject target)
        {
            base.Attack(target);
            CheckAbilities();
        }

        public override void Disengage()
        {
            m_orderAttackTarget = null;
            ClearAggroList();
            Body.StopAttack();
            Body.StopCurrentSpellcast();
            Body.TargetObject = null;
        }

        public override EWalkState WalkState
        {
            get => EWalkState.Follow;
            set { }
        }
    }
}
