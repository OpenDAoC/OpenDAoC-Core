namespace Core.GS.Enums;

public enum EAuditSubType
{
    AccountCreate,
    AccountFailedLogin,
    AccountSuccessfulLogin,
    AccountLogout,
    AccountPasswordChange,
    AccountEmailChange,
    AccountDelete,
    CharacterCreate,
    CharacterDelete,
    CharacterRename,
    CharacterLogin,
    CharacterLogout,
    PublicChat,
    PrivateChat
}