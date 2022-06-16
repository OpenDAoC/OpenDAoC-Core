
namespace DOL.AI.Brain
{
    public class SINeckBossBrain : StandardMobBrain
    {
        /// <summary>
        /// Determine if we have less than 12, if not, spawn one.
        /// </summary>
        public override void Think()
        {

            if (!Body.InCombatInLast(60 * 1000) && Body.InCombatInLast(65 * 1000)) // 60 seconds
            {
                Body.Say("Cowards..");
                Body.RemoveFromWorld();
            }
            base.Think();
        }
    }
}