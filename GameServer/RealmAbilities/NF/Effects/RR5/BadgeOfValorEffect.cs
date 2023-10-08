using System;
using System.Collections.Generic;
using DOL.Events;

namespace DOL.GS.Effects
{
    /// <summary>
    /// Effect handler for Arms Length
    /// </summary>
    public class BadgeOfValorEffect : TimedEffect
    {
        //http://daocpedia.de/index.php/Abzeichen_des_Mutes    

        /// <summary>
        /// Default constructor for AmelioratingMelodiesEffect
        /// </summary>
		public BadgeOfValorEffect()
			: base(20000)
		{

		}

        /// <summary>
        /// Called when effect is to be started
        /// </summary>
        /// <param name="living"></param>
        public override void Start(GameLiving living)
        {
			base.Start(living);
            GameEventMgr.AddHandler(m_owner, GamePlayerEvent.AttackFinished, new CoreEventHandler(AttackFinished));
        }



        /// <summary>
        /// Called when a player is inflicted in an combat action
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        private void AttackFinished(CoreEvent e, object sender, EventArgs args)
        {
            AttackFinishedEventArgs afea = (AttackFinishedEventArgs)args;
            
            if (m_owner != afea.AttackData.Attacker || afea.AttackData.AttackType == EAttackType.Spell)
                return;
            //only affect this onto players
            if (!(afea.AttackData.Target is GamePlayer))
                return;
            GamePlayer target = afea.AttackData.Target as GamePlayer;
            
            Database.DbInventoryItem armor = target.Inventory.GetItem((EInventorySlot)((int)afea.AttackData.ArmorHitLocation));
            
            if (armor == null || armor.SPD_ABS == 0)
                return;
            //cap at 50%
            int bonusPercent = Math.Min(armor.SPD_ABS,50);
                        

            //add 2times percentual of abs, one time will be substracted later
            afea.AttackData.Damage = (int)(armor.SPD_ABS*(bonusPercent*2 + 100)*0.01 );

        }

        /// <summary>
        ///  Called when effect is to be cancelled
        /// </summary>
        public override void Stop()
        {
			base.Stop();
            GameEventMgr.RemoveHandler(m_owner, GamePlayerEvent.AttackFinished, new CoreEventHandler(AttackFinished));
        }


        /// <summary>
        /// Name of the effect
        /// </summary>
        public override string Name
        {
            get
            {
                return "Badge of Valor";
            }
        }

        /// <summary>
        /// Icon ID
        /// </summary>
        public override UInt16 Icon
        {
            get
            {
                return 3056;
            }
        }

        /// <summary>
        /// Delve information
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>();
                delveInfoList.Add("Melee damage for the next 20 seconds will be INCREASED by the targets armor-based ABS instead of decreased.");
                delveInfoList.Add(" ");

                int seconds = (int) RemainingTime/1000;
                if (seconds > 0)
                {
                    delveInfoList.Add(" ");
                    delveInfoList.Add("- " + seconds + " seconds remaining.");
                }

                return delveInfoList;
            }
        }
    }
}
