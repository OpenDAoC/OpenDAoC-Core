namespace Core.GS.Enums;

/// <summary>
/// Types of chat messages
/// </summary>
public enum EChatType : byte
{
    CT_System = 0x00,
    CT_Say = 0x01,
    CT_Send = 0x02,
    CT_Group = 0x03,
    CT_Guild = 0x04,
    CT_Broadcast = 0x05,
    CT_Emote = 0x06,
    CT_Help = 0x07,
    CT_Chat = 0x08,
    CT_Advise = 0x09,
    CT_Officer = 0x0a,
    CT_Alliance = 0x0b,
    CT_BattleGroup = 0x0c,
    CT_BattleGroupLeader = 0x0d,
    // 0x0e sends nothing (tested with client v1.99)
    CT_Staff = 0xf,

    CT_Spell = 0x10,
    CT_YouHit = 0x11,
    CT_YouWereHit = 0x12,
    CT_Skill = 0x13,
    CT_Merchant = 0x14,
    CT_YouDied = 0x15,
    CT_PlayerDied = 0x16,
    CT_OthersCombat = 0x17,
    CT_DamageAdd = 0x18,
    CT_SpellExpires = 0x19,
    CT_Loot = 0x1a,
    CT_SpellResisted = 0x1b,
    CT_Important = 0x1c,
    CT_Damaged = 0x1d,
    CT_Missed = 0x1e,
    CT_SpellPulse = 0x1f,
    CT_KilledByAlb = 0x20,
    CT_KilledByMid = 0x21,
    CT_KilledByHib = 0x22,
    CT_LFG = 0x23,
    CT_Trade = 0x24,

    CT_SocialInterface = 0x64,
    CT_ScreenCenter = 0xC8,
    CT_ScreenCenterSmaller = 0xC9,
    CT_ScreenCenter_And_CT_System = 0xCA,
    CT_ScreenCenterSmaller_And_CT_System = 0xCB,
}