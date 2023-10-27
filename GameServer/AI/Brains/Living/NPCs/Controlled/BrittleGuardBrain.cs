using System;

namespace Core.GS.AI
{
    public class BrittleGuardBrain : ControlledNpcBrain
    {
        public BrittleGuardBrain(GameLiving owner)
            : base(owner)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");
        }

        public override void FollowOwner()
        {
            Body.StopAttack();
            Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
        }
    }
}