/*
 *
 * Atlas - Test phase titles
 *
 */

using System;
using DOL.Database;
using DOL.Events;

namespace DOL.GS.PlayerTitles
{
    /// <summary>
    /// Example...
    /// </summary>
    ///
    #region Alpha
    
    // I'm Bugged
    public class AlphaTitle1 : EventPlayerTitle 
    {
        /// <summary>
        /// The title description, shown in "Titles" window.
        /// </summary>
        /// <param name="player">The title owner.</param>
        /// <returns>The title description.</returns>
        public override string GetDescription(GamePlayer player)
        {
            return "I'm bugged";
        }

        /// <summary>
        /// The title value, shown over player's head.
        /// </summary>
        /// <param name="source">The player looking.</param>
        /// <param name="player">The title owner.</param>
        /// <returns>The title value.</returns>
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "I'm bugged";
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
            DateTime alphaEnd = new DateTime(2021, 12, 17);

            return player.Client.Account.CreationDate < alphaEnd;
        }
		
        /// <summary>
        /// The event callback.
        /// </summary>
        /// <param name="e">The event fired.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="arguments">The event arguments.</param>
        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer p = sender as GamePlayer;
            if (p != null && p.Titles.Contains(this))
            {
                p.UpdateCurrentTitle();
                return;
            }
            base.EventCallback(e, sender, arguments);
        }
    }
    
    // Linkdead
    public class AlphaTitle2 : EventPlayerTitle 
    {
        /// <summary>
        /// The title description, shown in "Titles" window.
        /// </summary>
        /// <param name="player">The title owner.</param>
        /// <returns>The title description.</returns>
        public override string GetDescription(GamePlayer player)
        {
            return "Linkdead";
        }

        /// <summary>
        /// The title value, shown over player's head.
        /// </summary>
        /// <param name="source">The player looking.</param>
        /// <param name="player">The title owner.</param>
        /// <returns>The title value.</returns>
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Linkdead";
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
            DateTime alphaEnd = new DateTime(2021, 12, 17);

            return player.Client.Account.CreationDate < alphaEnd;
        }
		
        /// <summary>
        /// The event callback.
        /// </summary>
        /// <param name="e">The event fired.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="arguments">The event arguments.</param>
        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer p = sender as GamePlayer;
            if (p != null && p.Titles.Contains(this))
            {
                p.UpdateCurrentTitle();
                return;
            }
            base.EventCallback(e, sender, arguments);
        }
    }
    #endregion
    #region PvE Beta
    // Beta Participation
    public class PveBetaParticipationTitle : EventPlayerTitle
    {
        /// <summary>
        /// The title description, shown in "Titles" window.
        /// </summary>
        /// <param name="player">The title owner.</param>
        /// <returns>The title description.</returns>
        public override string GetDescription(GamePlayer player)
        {
            return "Baby Beetle";
        }

        /// <summary>
        /// The title value, shown over player's head.
        /// </summary>
        /// <param name="source">The player looking.</param>
        /// <param name="player">The title owner.</param>
        /// <returns>The title value.</returns>
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Baby Beetle";
        }
		
        /// <summary>
        /// The event to hook.
        /// </summary>
        public override DOLEvent Event
        {
            get { return GamePlayerEvent.LevelUp; }
        }
		
        /// <summary>
        /// Verify whether the player is suitable for this title.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>true if the player is suitable for this title.</returns>
        public override bool IsSuitable(GamePlayer player)
        {
            var keyName = "PvEBetaParticipation";
            
            if (player.Client.Account.CustomParams == null)
                return false;

            foreach (AccountXCustomParam customParam in player.Client.Account.CustomParams)
            {
                if (customParam.KeyName == keyName)
                {
                    return true;
                }
            }

            return false;
        }
		
        /// <summary>
        /// The event callback.
        /// </summary>
        /// <param name="e">The event fired.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="arguments">The event arguments.</param>
        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer p = sender as GamePlayer;
            if (p != null && p.Titles.Contains(this))
            {
                p.UpdateCurrentTitle();
                return;
            }
            base.EventCallback(e, sender, arguments);
        }
    }
    
    // Beta - Level 35 Title
    public class PveBeta35Title : EventPlayerTitle
    {
        /// <summary>
        /// The title description, shown in "Titles" window.
        /// </summary>
        /// <param name="player">The title owner.</param>
        /// <returns>The title description.</returns>
        public override string GetDescription(GamePlayer player)
        {
            return "Growing Beetle";
        }

        /// <summary>
        /// The title value, shown over player's head.
        /// </summary>
        /// <param name="source">The player looking.</param>
        /// <param name="player">The title owner.</param>
        /// <returns>The title value.</returns>
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Growing Beetle";
        }
		
        /// <summary>
        /// The event to hook.
        /// </summary>
        public override DOLEvent Event
        {
            get { return GamePlayerEvent.LevelUp; }
        }
		
        /// <summary>
        /// Verify whether the player is suitable for this title.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>true if the player is suitable for this title.</returns>
        public override bool IsSuitable(GamePlayer player)
        {
            var keyName = "PvEBeta35";
            
            if (player.Client.Account.CustomParams == null)
                return false;

            foreach (AccountXCustomParam customParam in player.Client.Account.CustomParams)
            {
                if (customParam.KeyName == keyName)
                {
                    return true;
                }
            }

            return false;
        }
		
        /// <summary>
        /// The event callback.
        /// </summary>
        /// <param name="e">The event fired.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="arguments">The event arguments.</param>
        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer p = sender as GamePlayer;
            if (p != null && p.Titles.Contains(this))
            {
                p.UpdateCurrentTitle();
                return;
            }
            base.EventCallback(e, sender, arguments);
        }
    }
    
    // Beta - Level 50 Title
    public class PveBeta50Title : EventPlayerTitle
    {
        /// <summary>
        /// The title description, shown in "Titles" window.
        /// </summary>
        /// <param name="player">The title owner.</param>
        /// <returns>The title description.</returns>
        public override string GetDescription(GamePlayer player)
        {
            return "Massive Beetle";
        }

        /// <summary>
        /// The title value, shown over player's head.
        /// </summary>
        /// <param name="source">The player looking.</param>
        /// <param name="player">The title owner.</param>
        /// <returns>The title value.</returns>
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Massive Beetle";
        }
		
        /// <summary>
        /// The event to hook.
        /// </summary>
        public override DOLEvent Event
        {
            get { return GamePlayerEvent.LevelUp; }
        }
		
        /// <summary>
        /// Verify whether the player is suitable for this title.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>true if the player is suitable for this title.</returns>
        public override bool IsSuitable(GamePlayer player)
        {
            var keyName = "PvEBeta50";
            
            if (player.Client.Account.CustomParams == null)
                return false;

            foreach (AccountXCustomParam customParam in player.Client.Account.CustomParams)
            {
                if (customParam.KeyName == keyName)
                {
                    return true;
                }
            }

            return false;
        }
		
        /// <summary>
        /// The event callback.
        /// </summary>
        /// <param name="e">The event fired.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="arguments">The event arguments.</param>
        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer p = sender as GamePlayer;
            if (p != null && p.Titles.Contains(this))
            {
                p.UpdateCurrentTitle();
                return;
            }
            base.EventCallback(e, sender, arguments);
        }
    }
    #endregion
}
