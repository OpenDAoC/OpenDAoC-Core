using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class StyleComponent
    {
        public GameLiving Owner { get; }

        public virtual bool CancelStyle
        {
            get => false;
            set { }
        }

        public virtual bool AwaitingBackupInput
        {
            get => false;
            set { }
        }

        public virtual Style AutomaticBackupStyle
        {
            get => null;
            set { }
        }

        protected StyleComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public static StyleComponent Create(GameLiving owner)
        {
            if (owner is GameNPC npcOwner)
                return new NpcStyleComponent(npcOwner);
            else if (owner is GamePlayer playerOwner)
                return new PlayerStyleComponent(playerOwner);
            else
                return new StyleComponent(owner);
        }

        /// <summary>
        /// Holds all styles of the player
        /// </summary>
        protected readonly Dictionary<int, Style> m_styles = new Dictionary<int, Style>();

        /// <summary>
        /// Used to lock the style list
        /// </summary>
        protected readonly object lockStyleList = new();

        /// <summary>
        /// Gets a list of available styles
        /// This creates a copy
        /// </summary>
        public IList GetStyleList()
        {
            List<Style> list = new List<Style>();
            lock (lockStyleList)
            {
                list = m_styles.Values.OrderBy(x => x.SpecLevelRequirement).ThenBy(y => y.ID).ToList();
            }
            return list;
        }

        protected Style _nextCombatStyle;
        protected Style _nextCombatBackupStyle;
        protected long _nextCombatStyleTime;

        /// <summary>
        /// Gets or Sets the next combat style to use
        /// </summary>
        public Style NextCombatStyle
        {
            get { return _nextCombatStyle; }
            set { _nextCombatStyle = value; }
        }

        /// <summary>
        /// Gets or Sets the next combat backup style to use
        /// </summary>
        public Style NextCombatBackupStyle
        {
            get { return _nextCombatBackupStyle; }
            set { _nextCombatBackupStyle = value; }
        }

        /// <summary>
        /// Gets or Sets the time at which the style was set
        /// </summary>
        public long NextCombatStyleTime
        {
            get { return _nextCombatStyleTime; }
            set { _nextCombatStyleTime = value; }
        }

        /// <summary>
        /// Holds the cancel style flag
        /// </summary>
        protected bool m_cancelStyle;

        public void ExecuteWeaponStyle(Style style)
        {
            StyleProcessor.TryToUseStyle(Owner, style);
        }

        /// <summary>
        /// Decides which style living will use in this moment
        /// </summary>
        /// <returns>Style to use or null if none</returns>
        public virtual Style GetStyleToUse()
        {
            throw new NotImplementedException();
        }

        public virtual void DelveWeaponStyle(List<string> delveInfo, Style style)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllStyles()
        {
            lock (lockStyleList)
            {
                m_styles.Clear();
            }
        }

        public virtual void AddStyle(Style style, bool notify)
        {
            throw new NotImplementedException();
        }
    }
}
