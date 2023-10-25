/*
 * [NOTE:StephenxPimentel] The Die/AddToWorld functions are just an example.
 * Although this is a great base for individual Relic Protector Classes, and
 * it would be a very good idea for new classes to be derived from this
 *
 * e.g. Lab_Center_Relic : BaseProtector.
 * This way you can call the Locking/Unlocking Methods!
 *
 * Make sure that in your scripts u define "Relic" as the correct relic so
 * the Unlock/Lock Functions work properly!
 */

using Core.GS.Enums;

namespace Core.GS
{
    public class BaseProtector : GameNpc
    {
        private static MinotaurRelic m_relic;
        private static GameNpc m_lockedEffect;

        public static MinotaurRelic Relic
        {
            get { return m_relic; }
            set { m_relic = value; }
        }

        public static GameNpc LockedEffect
        {
            get { return m_lockedEffect; }
            set { m_lockedEffect = value; }
        }

        public static void UnlockRelic()
        {
            if (LockedEffect != null)
                LockedEffect.RemoveFromWorld();
        }
        //Never save the mob into the database either
        //or you will get double spawns on server start!
        public override void SaveIntoDatabase()
        {
            return;
        }
        public static bool LockRelic()
        {
            //make sure the relic exists before you lock it!
            if (Relic == null)
                return false;

            LockedEffect = new GameNpc();
            LockedEffect.Model = 1583;
            LockedEffect.Name = "LOCKED_RELIC";
            LockedEffect.X = Relic.X;
            LockedEffect.Y = Relic.Y;
            LockedEffect.Z = Relic.Z;
            LockedEffect.Heading = Relic.Heading;
            LockedEffect.CurrentRegionID = Relic.CurrentRegionID;
            LockedEffect.Flags = ENpcFlags.CANTTARGET;
            LockedEffect.AddToWorld();

            return true;
        }
        //these mobs should NEVER respawn.
        public override int RespawnInterval
        {
            get { return 0; }
        }
        public override void Die(GameObject killer)
        {
            /* NOTE: THIS IS JUST AN EXAMPLE!
             * Unlock the relic when the mob dies!
             
            UnlockRelic(); 
             */

            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            LoadedFromScript = true;

            /* NOTE: THIS IS JUST AN EXAMPLE!
             * At Spawn: Define Relic with RelicID, then lock it!
              
            Relic = MinotaurRelicManager.GetRelic(1);
            LockRelic();
             */

            //You must also define the location that this mob should spawn
            //it will be spawned whenever the relic is spawned.

            return base.AddToWorld();
        }
    }
}
