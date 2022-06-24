using System;
using DOL.Events;

namespace DOL.GS.PlayerTitles
{
    /// <summary>
    /// Example...
    /// </summary>
    ///
    public class LaunchQuestTitle : EventPlayerTitle
    {
        /// <summary>
        /// The title description, shown in "Titles" window.
        /// </summary>
        /// <param name="player">The title owner.</param>
        /// <returns>The title description.</returns>
        public override string GetDescription(GamePlayer player)
        {
            return "Congrats, I guess?";
        }

        /// <summary>
        /// The title value, shown over player's head.
        /// </summary>
        /// <param name="source">The player looking.</param>
        /// <param name="player">The title owner.</param>
        /// <returns>The title value.</returns>
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Congrats, I guess?";
        }
		
        /// <summary>
        /// The event to hook.
        /// </summary>
        public override DOLEvent Event
        {
            get { return GamePlayerEvent.GameEntered; }
        }
		
        /// <summary>
        /// Verify whether the player is suitable for this title.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>true if the player is suitable for this title.</returns>
        public override bool IsSuitable(GamePlayer player)
        {
            var hasCredit = AchievementUtils.CheckAccountCredit("LaunchQuest", player);
            return hasCredit;
        }
		
        /// <summary>
        /// The event callback.
        /// </summary>
        /// <param name="e">The event fired.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="arguments">The event arguments.</param>
        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
        {
            if (sender is GamePlayer p && p.Titles.Contains(this) && p.CurrentTitle == null)
            {
                p.UpdateCurrentTitle();
                return;
            }
            base.EventCallback(e, sender, arguments);
        }
    }
   
}