using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.Language;

namespace DOL.GS
{
    public class PlayerStyleComponent : StyleComponent
    {
        private GamePlayer _playerOwner;
        private bool _awaitingBackupInput;
        private Style _automaticBackupStyle;

        public override bool CancelStyle
        {
            get => _playerOwner.DBCharacter != null && _playerOwner.DBCharacter.CancelStyle;
            set
            {
                if (_playerOwner.DBCharacter != null)
                    _playerOwner.DBCharacter.CancelStyle = value;
            }
        }

        public override bool AwaitingBackupInput
        {
            get => _awaitingBackupInput;
            set => _awaitingBackupInput = value;
        }

        public override Style AutomaticBackupStyle
        {
            get => _automaticBackupStyle;
            set => _automaticBackupStyle = value;
        }

        public PlayerStyleComponent(GamePlayer playerOwner) : base(playerOwner)
        {
            _playerOwner = playerOwner;
        }

        public void OnPlayerLoadFromDatabase()
        {
            DbCoreCharacter dbCharacter = _playerOwner.DBCharacter;

            if (dbCharacter == null)
                return;

            AutomaticBackupStyle = SkillBase.GetStyleByID(dbCharacter.AutomaticBackupStyleId, _playerOwner.CharacterClass.ID);
        }

        public void OnPlayerSaveIntoDatabase()
        {
            DbCoreCharacter dbCharacter = _playerOwner.DBCharacter;

            if (dbCharacter == null)
                return;

            if (AutomaticBackupStyle == null)
            {
                dbCharacter.AutomaticBackupStyleId = 0;
                return;
            }

            if (AutomaticBackupStyle.ID == dbCharacter.AutomaticBackupStyleId)
                return;

            dbCharacter.AutomaticBackupStyleId = (ushort) AutomaticBackupStyle.ID;
        }

        public override Style GetStyleToUse()
        {
            if (NextCombatStyle == null)
                return null;

            // If the player no longer access to the style.
            if (AutomaticBackupStyle != null && _playerOwner.GetBaseSpecLevel(AutomaticBackupStyle.Spec) < AutomaticBackupStyle.SpecLevelRequirement)
            {
                _playerOwner.Out.SendMessage($"{AutomaticBackupStyle.Name} is no longer a valid backup style for your spec level and has been cleared.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                AutomaticBackupStyle = null;
            }

            AttackData lastAttackData = Owner.attackComponent.attackAction.LastAttackData;
            DbInventoryItem weapon = NextCombatStyle.WeaponTypeRequirement == (int) eObjectType.Shield ? Owner.ActiveLeftWeapon : Owner.ActiveWeapon;

            //determine which style will actually be used
            Style styleToUse;

            if (StyleProcessor.CanUseStyle(lastAttackData, Owner, NextCombatStyle, weapon))
                styleToUse = NextCombatStyle;
            else if (NextCombatBackupStyle != null)
                styleToUse = NextCombatBackupStyle;
            else if (AutomaticBackupStyle != null)
            {
                StyleProcessor.TryToUseStyle(Owner, AutomaticBackupStyle);
                styleToUse = NextCombatBackupStyle; // `NextCombatBackupStyle` became `AutomaticBackupStyle` if `TryToUse` succeeded.
            }
            else
                styleToUse = NextCombatStyle; // Not sure why.

            return styleToUse;
        }

        public override void DelveWeaponStyle(List<string> delveInfo, Style style)
        {
            StyleProcessor.DelveWeaponStyle(delveInfo, style, _playerOwner);
        }

        public override void AddStyle(Style style, bool notify)
        {
            lock (_stylesLock)
            {
                if (_styles.TryGetValue(style.ID, out Style existingStyle))
                {
                    existingStyle.Level = style.Level;
                    return;
                }

                _styles.Add(style.ID, style);

                if (!notify)
                    return;

                _playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.YouLearn", style.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                string message = null;

                if (style.OpeningRequirementType is Style.eOpening.Offensive)
                {
                    switch (style.AttackResultRequirement)
                    {
                        case Style.eAttackResultRequirement.Style:
                        case Style.eAttackResultRequirement.Hit: // TODO: make own message for hit after styles DB is updated
                        {
                            Style reqStyle = SkillBase.GetStyleByID(style.OpeningRequirementValue, _playerOwner.CharacterClass.ID);

                            if (reqStyle == null)
                                message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.AfterStyle", "(style " + style.OpeningRequirementValue + " not found)");
                            else
                                message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.AfterStyle", reqStyle.Name);

                            break;
                        }
                        case Style.eAttackResultRequirement.Miss:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.AfterMissed");
                            break;
                        }
                        case Style.eAttackResultRequirement.Parry:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.AfterParried");
                            break;
                        }
                        case Style.eAttackResultRequirement.Block:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.AfterBlocked");
                            break;
                        }
                        case Style.eAttackResultRequirement.Evade:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.AfterEvaded");
                            break;
                        }
                        case Style.eAttackResultRequirement.Fumble:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.AfterFumbles");
                            break;
                        }
                    }
                }
                else if (style.OpeningRequirementType is Style.eOpening.Defensive)
                {
                    switch (style.AttackResultRequirement)
                    {
                        case Style.eAttackResultRequirement.Miss:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.TargetMisses");
                            break;
                        }
                        case Style.eAttackResultRequirement.Hit:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.TargetHits");
                            break;
                        }
                        case Style.eAttackResultRequirement.Parry:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.TargetParried");
                            break;
                        }
                        case Style.eAttackResultRequirement.Block:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.TargetBlocked");
                            break;
                        }
                        case Style.eAttackResultRequirement.Evade:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.TargetEvaded");
                            break;
                        }
                        case Style.eAttackResultRequirement.Fumble:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.TargetFumbles");
                            break;
                        }
                        case Style.eAttackResultRequirement.Style:
                        {
                            message = LanguageMgr.GetTranslation(_playerOwner.Client.Account.Language, "GamePlayer.RefreshSpec.TargetStyle");
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(message))
                    _playerOwner.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}
