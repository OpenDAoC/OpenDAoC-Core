using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class StyleComponent
    {
        public GameLiving Owner { get; }
        public Style NextCombatStyle { get; set; }
        public Style NextCombatBackupStyle { get; set; }
        public long NextCombatStyleTime { get; set; }

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

        protected readonly Dictionary<int, Style> _styles = new();
        protected readonly Lock _stylesLock = new();

        public IList GetStyleList()
        {
            List<Style> list = new();

            lock (_stylesLock)
            {
                list = _styles.Values.OrderBy(x => x.SpecLevelRequirement).ThenBy(y => y.ID).ToList();
            }

            return list;
        }

        public void ExecuteWeaponStyle(Style style)
        {
            StyleProcessor.TryToUseStyle(Owner, style);
        }

        public virtual Style GetStyleToUse()
        {
            return null;
        }

        public virtual void DelveWeaponStyle(List<string> delveInfo, Style style)
        {
            return;
        }

        public void RemoveAllStyles()
        {
            lock (_stylesLock)
            {
                _styles.Clear();
            }
        }

        public virtual void AddStyle(Style style, bool notify)
        {
            throw new NotImplementedException();
        }
    }
}
