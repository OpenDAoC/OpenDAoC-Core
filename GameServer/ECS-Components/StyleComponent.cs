using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Styles;
using DOL.Language;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class StyleComponent
    {
        public GameLiving owner;

        public StyleComponent(GameLiving owner)
        {
            this.owner = owner;
        }

        /// <summary>
		/// Holds all styles of the player
		/// </summary>
		protected readonly Dictionary<int, Style> m_styles = new Dictionary<int, Style>();

        /// <summary>
        /// Used to lock the style list
        /// </summary>
        protected readonly Object lockStyleList = new Object();

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

        /// <summary>
		/// Holds the Style that this living should use next
		/// </summary>
		protected Style m_nextCombatStyle;
        /// <summary>
        /// Holds the backup style for the style that the living should use next
        /// </summary>
        protected Style m_nextCombatBackupStyle;

        /// <summary>
        /// Gets or Sets the next combat style to use
        /// </summary>
        public Style NextCombatStyle
        {
            get { return m_nextCombatStyle; }
            set { m_nextCombatStyle = value; }
        }
        /// <summary>
        /// Gets or Sets the next combat backup style to use
        /// </summary>
        public Style NextCombatBackupStyle
        {
            get { return m_nextCombatBackupStyle; }
            set { m_nextCombatBackupStyle = value; }
        }

        /// <summary>
		/// Holds the cancel style flag
		/// </summary>
		protected bool m_cancelStyle;

        /// <summary>
        /// Gets or Sets the cancel style flag
        /// (delegate to PlayerCharacter)
        /// </summary>
        public bool CancelStyle
        {
            get { return (owner as GamePlayer).DBCharacter != null ? (owner as GamePlayer).DBCharacter.CancelStyle : false; }
            set { if ((owner as GamePlayer).DBCharacter != null) (owner as GamePlayer).DBCharacter.CancelStyle = value; }
        }

        public void ExecuteWeaponStyle(Style style)
        {
            StyleProcessor.TryToUseStyle(owner, style);
        }

        /// <summary>
        /// Decides which style living will use in this moment
        /// </summary>
        /// <returns>Style to use or null if none</returns>
        public Style GetStyleToUse()
        {
            InventoryItem weapon;
            if (NextCombatStyle == null) return null;
            if (NextCombatStyle.WeaponTypeRequirement == (int)eObjectType.Shield)
                weapon = owner.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
            else weapon = owner.attackComponent.AttackWeapon;

            if (StyleProcessor.CanUseStyle(owner, NextCombatStyle, weapon))
                return NextCombatStyle;

            if (NextCombatBackupStyle == null) return NextCombatStyle;

            return NextCombatBackupStyle;
        }

        /// <summary>
		/// Picks a style, prioritizing reactives an	d chains over positionals and anytimes
		/// </summary>
		/// <returns>Selected style</returns>
		public Style NPCGetStyleToUse()
        {
            var p = owner as GameNPC;
            if (p.Styles == null || p.Styles.Count < 1 || p.TargetObject == null)
                return null;

            // Chain and defensive styles are excluded from the chance roll because they would almost never happen otherwise. 
            // For example, an NPC blocks 10% of the time, so the default 20% style chance effectively means the defensive 
            // style would only actually occur during 2% of of a mob's attacks. In comparison, a style chain would only happen 
            // 0.4% of the time.
            if (p.StylesChain != null && p.StylesChain.Count > 0)
                foreach (Style s in p.StylesChain)
                    if (StyleProcessor.CanUseStyle(p, s, p.attackComponent.AttackWeapon))
                        return s;

            if (p.StylesDefensive != null && p.StylesDefensive.Count > 0)
                foreach (Style s in p.StylesDefensive)
                    if (StyleProcessor.CanUseStyle(p, s, p.attackComponent.AttackWeapon)
                        && p.CheckStyleStun(s)) // Make sure we don't spam stun styles like Brutalize
                        return s;

            if (Util.Chance(Properties.GAMENPC_CHANCES_TO_STYLE))
            {
                // All of the remaining lists are randomly picked from,
                // as this creates more variety with each combat result.
                // For example, a mob with both Pincer and Ice Storm
                // styles could potentially use one or the other with
                // each attack roll that succeeds.
                
                // First, check positional styles (in order of back, side, front)
                // in case the defender is facing another direction
                if (p.StylesBack != null && p.StylesBack.Count > 0)
                {
                    Style s = p.StylesBack[Util.Random(0, p.StylesBack.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.attackComponent.AttackWeapon))
                        return s;
                }

                if (p.StylesSide != null && p.StylesSide.Count > 0)
                {
                    Style s = p.StylesSide[Util.Random(0, p.StylesSide.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.attackComponent.AttackWeapon))
                        return s;
                }

                if (p.StylesFront != null && p.StylesFront.Count > 0)
                {
                    Style s = p.StylesFront[Util.Random(0, p.StylesFront.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.attackComponent.AttackWeapon))
                        return s;
                }

                // Pick a random anytime style
                if (p.StylesAnytime != null && p.StylesAnytime.Count > 0)
                    return p.StylesAnytime[Util.Random(0, p.StylesAnytime.Count - 1)];
            }

            return null;
        }

        /// <summary>
		/// Delve a weapon style for this player
		/// </summary>
		/// <param name="delveInfo"></param>
		/// <param name="style"></param>
		/// <returns></returns>
		public void DelveWeaponStyle(IList<string> delveInfo, Style style)
        {
            StyleProcessor.DelveWeaponStyle(delveInfo, style, owner as GamePlayer);
        }

        public void RemoveAllStyles()
        {
            lock (lockStyleList)
            {
                m_styles.Clear();
            }
        }

        public void AddStyle(Style st, bool notify)
        {
            var p = owner as GamePlayer;

            lock (lockStyleList)
            {
                if (m_styles.ContainsKey(st.ID))
                {
                    m_styles[st.ID].Level = st.Level;
                }
                else
                {
                    m_styles.Add(st.ID, st);

                    // Verbose
                    if (notify)
                    {
                        Style style = st;
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.YouLearn", style.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                        string message = null;

                        if (Style.eOpening.Offensive == style.OpeningRequirementType)
                        {
                            switch (style.AttackResultRequirement)
                            {
                                case Style.eAttackResultRequirement.Style:
                                case Style.eAttackResultRequirement.Hit: // TODO: make own message for hit after styles DB is updated

                                    Style reqStyle = SkillBase.GetStyleByID(style.OpeningRequirementValue, p.CharacterClass.ID);

                                    if (reqStyle == null)
                                        message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.AfterStyle", "(style " + style.OpeningRequirementValue + " not found)");

                                    else message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.AfterStyle", reqStyle.Name);

                                    break;
                                case Style.eAttackResultRequirement.Miss:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.AfterMissed");
                                    break;
                                case Style.eAttackResultRequirement.Parry:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.AfterParried");
                                    break;
                                case Style.eAttackResultRequirement.Block:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.AfterBlocked");
                                    break;
                                case Style.eAttackResultRequirement.Evade:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.AfterEvaded");
                                    break;
                                case Style.eAttackResultRequirement.Fumble:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.AfterFumbles");
                                    break;
                            }
                        }
                        else if (Style.eOpening.Defensive == style.OpeningRequirementType)
                        {
                            switch (style.AttackResultRequirement)
                            {
                                case Style.eAttackResultRequirement.Miss:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.TargetMisses");
                                    break;
                                case Style.eAttackResultRequirement.Hit:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.TargetHits");
                                    break;
                                case Style.eAttackResultRequirement.Parry:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.TargetParried");
                                    break;
                                case Style.eAttackResultRequirement.Block:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.TargetBlocked");
                                    break;
                                case Style.eAttackResultRequirement.Evade:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.TargetEvaded");
                                    break;
                                case Style.eAttackResultRequirement.Fumble:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.TargetFumbles");
                                    break;
                                case Style.eAttackResultRequirement.Style:
                                    message = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.RefreshSpec.TargetStyle");
                                    break;
                            }
                        }

                        if (!Util.IsEmpty(message))
                            p.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }
            }
        }
    }
}
