﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Styles;
using DOL.Language;

namespace DOL.GS
{
    public class StyleComponent
    {
        private GameLiving _owner;

        public bool AwaitingBackupInput = false;

        public StyleComponent(GameLiving owner)
        {
            _owner = owner;
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

        /// <summary>
        /// Holds the style that this living should use next
        /// </summary>
        protected Style m_nextCombatStyle;
        /// <summary>
        /// Holds the backup style for the style that the living should use next
        /// </summary>
        protected Style m_nextCombatBackupStyle;
        /// <summary>
        /// Holds the time at which the style was set
        /// </summary>
        protected long m_nextCombatStyleTime;
        
        //if automatic backup styles are enabled, this is the one that will be used
        public Style AutomaticBackupStyle { get; set; }

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
        /// Gets or Sets the time at which the style was set
        /// </summary>
        public long NextCombatStyleTime
        {
            get { return m_nextCombatStyleTime; }
            set { m_nextCombatStyleTime = value; }
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
            get => _owner is GamePlayer player && player.DBCharacter != null && player.DBCharacter.CancelStyle;
            set
            {
                if (_owner is GamePlayer player && player.DBCharacter != null)
                    player.DBCharacter.CancelStyle = value;
            }
        }

        public void ExecuteWeaponStyle(Style style)
        {
            StyleProcessor.TryToUseStyle(_owner, style);
        }

        /// <summary>
        /// Decides which style living will use in this moment
        /// </summary>
        /// <returns>Style to use or null if none</returns>
        public Style GetStyleToUse()
        {
            if (NextCombatStyle == null)
                return null;

            AttackData lastAttackData = _owner.TempProperties.GetProperty<AttackData>(GameLiving.LAST_ATTACK_DATA, null);
            DbInventoryItem weapon = NextCombatStyle.WeaponTypeRequirement == (int) eObjectType.Shield ? _owner.Inventory.GetItem(eInventorySlot.LeftHandWeapon) : _owner.ActiveWeapon;
            
            //if they've cached a style and then respecced to no longer have access, remove it
            if (AutomaticBackupStyle != null && _owner is GamePlayer player && player.WeaponBaseSpecLevel(weapon) < AutomaticBackupStyle.SpecLevelRequirement)
            {
                player.Out.SendMessage($"{AutomaticBackupStyle.Name} is no longer a valid backup style for your spec level and has been cleared.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                AutomaticBackupStyle = null;
            }
           
            //determine which style will actually be used
            Style styleToUse;

            if (StyleProcessor.CanUseStyle(lastAttackData, _owner, NextCombatStyle, weapon))
                styleToUse = NextCombatStyle;
            else if (NextCombatBackupStyle != null)
                styleToUse = NextCombatBackupStyle;
            else if (AutomaticBackupStyle != null)
            {
                StyleProcessor.TryToUseStyle(_owner, AutomaticBackupStyle);
                styleToUse = NextCombatBackupStyle; // `NextCombatBackupStyle` became `AutomaticBackupStyle` if `TryToUse` succeeded.
            }
            else
                styleToUse = NextCombatStyle; // Not sure why.

            return styleToUse;
        }

        /// <summary>
        /// Picks a style, prioritizing reactives and chains over positionals and anytimes
        /// </summary>
        /// <returns>Selected style</returns>
        public Style NPCGetStyleToUse()
        {
            var p = _owner as GameNPC;
            if (p.Styles == null || p.Styles.Count < 1 || p.TargetObject == null)
                return null;

            AttackData lastAttackData = p.TempProperties.GetProperty<AttackData>(GameLiving.LAST_ATTACK_DATA, null);

            // Chain and defensive styles are excluded from the chance roll because they would almost never happen otherwise. 
            // For example, an NPC blocks 10% of the time, so the default 20% style chance effectively means the defensive 
            // style would only actually occur during 2% of of a mob's attacks. In comparison, a style chain would only happen 
            // 0.4% of the time.
            if (p.StylesChain != null && p.StylesChain.Count > 0)
                foreach (Style s in p.StylesChain)
                    if (StyleProcessor.CanUseStyle(lastAttackData, p, s, p.ActiveWeapon))
                        return s;

            if (p.StylesDefensive != null && p.StylesDefensive.Count > 0)
                foreach (Style s in p.StylesDefensive)
                    if (StyleProcessor.CanUseStyle(lastAttackData, p, s, p.ActiveWeapon)
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
                    if (StyleProcessor.CanUseStyle(lastAttackData, p, s, p.ActiveWeapon))
                        return s;
                }

                if (p.StylesSide != null && p.StylesSide.Count > 0)
                {
                    Style s = p.StylesSide[Util.Random(0, p.StylesSide.Count - 1)];
                    if (StyleProcessor.CanUseStyle(lastAttackData, p, s, p.ActiveWeapon))
                        return s;
                }

                if (p.StylesFront != null && p.StylesFront.Count > 0)
                {
                    Style s = p.StylesFront[Util.Random(0, p.StylesFront.Count - 1)];
                    if (StyleProcessor.CanUseStyle(lastAttackData, p, s, p.ActiveWeapon))
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
            StyleProcessor.DelveWeaponStyle(delveInfo, style, _owner as GamePlayer);
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
            var p = _owner as GamePlayer;

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

                        if (!string.IsNullOrEmpty(message))
                            p.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }
            }
        }
    }
}
