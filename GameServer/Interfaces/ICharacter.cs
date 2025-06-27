using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerClass;
using DOL.GS.PlayerTitles;
using DOL.GS.Spells;
using DOL.GS.RealmAbilities;

namespace DOL.GS.Interfaces
{
    /// <summary>
    /// Core interface for player characters
    /// Combines multiple focused interfaces following ISP
    /// </summary>
    public interface ICharacter : IGameLiving, ICharacterProgression, ICharacterAccount, ICharacterPersistence
    {
        /// <summary>
        /// Game client connection for this character
        /// </summary>
        GameClient Client { get; }
        
        /// <summary>
        /// Packet output interface for client communication
        /// </summary>
        IPacketLib Out { get; }
        
        /// <summary>
        /// Character class defining abilities and progression
        /// </summary>
        ICharacterClass CharacterClass { get; }
        
        /// <summary>
        /// Whether this character has entered the game world
        /// </summary>
        bool EnteredGame { get; set; }
        
        /// <summary>
        /// Previous login timestamp
        /// </summary>
        DateTime PreviousLoginDate { get; set; }
    }

    /// <summary>
    /// Interface for character progression (levels, experience, skills)
    /// DAoC Rule: Progression follows specific formulas and level caps
    /// </summary>
    public interface ICharacterProgression
    {
        /// <summary>
        /// Current experience points
        /// </summary>
        long Experience { get; set; }
        
        /// <summary>
        /// Whether character gains experience
        /// </summary>
        bool GainXP { get; set; }
        
        /// <summary>
        /// Whether character gains realm points
        /// </summary>
        bool GainRP { get; set; }
        
        /// <summary>
        /// Current realm rank level
        /// </summary>
        int RealmLevel { get; set; }
        
        /// <summary>
        /// Available skill specialty points
        /// </summary>
        int SkillSpecialtyPoints { get; }
    }

    /// <summary>
    /// Interface for specialization management
    /// DAoC Rule: Specializations determine available skills and abilities
    /// </summary>
    public interface ISpecializationContainer
    {
        /// <summary>
        /// Get base specialization level for a skill line
        /// </summary>
        int GetBaseSpecLevel(string keyName);
        
        /// <summary>
        /// Get modified specialization level including bonuses
        /// </summary>
        int GetModifiedSpecLevel(string keyName);
        
        /// <summary>
        /// Add points to a specialization
        /// </summary>
        bool AddSpecialization(string keyName, int amount);
        
        /// <summary>
        /// Remove specialization line completely
        /// </summary>
        bool RemoveSpecialization(string keyName);
        
        /// <summary>
        /// Respec all specialization lines
        /// </summary>
        void RespecAllLines();
    }

    /// <summary>
    /// Interface for spell line management
    /// DAoC Rule: Spell lines unlock with specializations and class
    /// </summary>
    public interface ISpellLineContainer
    {
        /// <summary>
        /// Get a specific spell line by key name
        /// </summary>
        SpellLine GetSpellLine(string keyName);
        
        /// <summary>
        /// Add a spell line to the character
        /// </summary>
        void AddSpellLine(SpellLine line);
        
        /// <summary>
        /// Remove a spell line from the character
        /// </summary>
        bool RemoveSpellLine(string lineKeyName);
        
        /// <summary>
        /// Get all available spell lines
        /// </summary>
        IList<SpellLine> GetSpellLines();
    }

    /// <summary>
    /// Interface for realm abilities management
    /// DAoC Rule: Realm abilities unlock at realm rank levels
    /// </summary>
    public interface IRealmAbilityContainer
    {
        /// <summary>
        /// Current realm points
        /// </summary>
        long RealmPoints { get; set; }
        
        /// <summary>
        /// Current bounty points
        /// </summary>
        long BountyPoints { get; set; }
        
        /// <summary>
        /// Available realm specialty points for abilities
        /// </summary>
        int RealmSpecialtyPoints { get; }
        
        /// <summary>
        /// Get realm ability by key name
        /// </summary>
        RealmAbility GetRealmAbility(string keyName);
        
        /// <summary>
        /// Add a realm ability to the character
        /// </summary>
        void AddRealmAbility(RealmAbility ability);
    }

    /// <summary>
    /// Interface for account-related character properties
    /// </summary>
    public interface ICharacterAccount
    {
        /// <summary>
        /// Account name this character belongs to
        /// </summary>
        string AccountName { get; }
        
        /// <summary>
        /// Whether character appears anonymous
        /// </summary>
        bool IsAnonymous { get; set; }
        
        /// <summary>
        /// Role-playing flag
        /// </summary>
        bool RPFlag { get; set; }
        
        /// <summary>
        /// Hardcore mode flag
        /// </summary>
        bool HCFlag { get; set; }
        
        /// <summary>
        /// Whether to hide specialization in API
        /// </summary>
        bool HideSpecializationAPI { get; set; }
    }

    /// <summary>
    /// Interface for character persistence and database operations
    /// </summary>
    public interface ICharacterPersistence
    {
        /// <summary>
        /// Database character record
        /// </summary>
        DbCoreCharacter DBCharacter { get; }
        
        /// <summary>
        /// Bind stone location region
        /// </summary>
        int BindRegion { get; set; }
        
        /// <summary>
        /// Bind stone position X
        /// </summary>
        int BindXpos { get; set; }
        
        /// <summary>
        /// Bind stone position Y
        /// </summary>
        int BindYpos { get; set; }
        
        /// <summary>
        /// Bind stone position Z
        /// </summary>
        int BindZpos { get; set; }
    }

    /// <summary>
    /// Interface for character statistics management
    /// DAoC Rule: Stats are modified by race, class, items, and effects
    /// </summary>
    public interface ICharacterStats
    {
        /// <summary>
        /// Strength stat with all modifiers
        /// </summary>
        int Strength { get; }
        
        /// <summary>
        /// Dexterity stat with all modifiers
        /// </summary>
        int Dexterity { get; }
        
        /// <summary>
        /// Constitution stat with all modifiers
        /// </summary>
        int Constitution { get; }
        
        /// <summary>
        /// Quickness stat with all modifiers
        /// </summary>
        int Quickness { get; }
        
        /// <summary>
        /// Intelligence stat with all modifiers
        /// </summary>
        int Intelligence { get; }
    }

    /// <summary>
    /// Interface for player titles and display
    /// </summary>
    public interface ITitleContainer
    {
        /// <summary>
        /// Get current active title
        /// </summary>
        IPlayerTitle CurrentTitle { get; }
        
        /// <summary>
        /// Set the active title
        /// </summary>
        void SetTitle(IPlayerTitle title);
        
        /// <summary>
        /// Check if character has a specific title
        /// </summary>
        bool HasTitle(IPlayerTitle title);
        
        /// <summary>
        /// Add a title to the character
        /// </summary>
        void AddTitle(IPlayerTitle title);
    }

    /// <summary>
    /// Interface for guild membership
    /// DAoC Rule: Guild membership provides benefits and social structure
    /// </summary>
    public interface IGuildMember
    {
        /// <summary>
        /// Current guild membership
        /// </summary>
        Guild Guild { get; set; }
        
        /// <summary>
        /// Guild identifier
        /// </summary>
        string GuildID { get; }
        
        /// <summary>
        /// Guild rank within the guild
        /// </summary>
        byte GuildRank { get; set; }
        
        /// <summary>
        /// Guild note set by officers
        /// </summary>
        string GuildNote { get; set; }
    }

    /// <summary>
    /// Interface for character appearance and customization
    /// </summary>
    public interface ICharacterAppearance
    {
        /// <summary>
        /// Character face customization attributes
        /// </summary>
        byte GetFaceAttribute(eCharFacePart part);
        
        /// <summary>
        /// Set face customization attribute
        /// </summary>
        void SetFaceAttribute(eCharFacePart part, byte value);
        
        /// <summary>
        /// Whether cloak hood is up
        /// </summary>
        bool IsCloakHoodUp { get; set; }
        
        /// <summary>
        /// Whether cloak is invisible
        /// </summary>
        bool IsCloakInvisible { get; set; }
        
        /// <summary>
        /// Whether helm is invisible
        /// </summary>
        bool IsHelmInvisible { get; set; }
    }

    /// <summary>
    /// Interface for social features
    /// </summary>
    public interface ISocialCharacter
    {
        /// <summary>
        /// Ignore list for blocking communication
        /// </summary>
        ArrayList IgnoreList { get; set; }
        
        /// <summary>
        /// Whether looking for a group
        /// </summary>
        bool LookingForGroup { get; set; }
        
        /// <summary>
        /// Auto-split loot setting
        /// </summary>
        bool AutoSplitLoot { get; set; }
        
        /// <summary>
        /// Away from keyboard message
        /// </summary>
        string AfkMessage { get; set; }
    }

    /// <summary>
    /// Interface for respecs and character reset
    /// DAoC Rule: Respecs allow redistribution of skill points
    /// </summary>
    public interface ICharacterRespec
    {
        /// <summary>
        /// Number of full skill respecs available
        /// </summary>
        int RespecAmountAllSkill { get; set; }
        
        /// <summary>
        /// Number of single line respecs available
        /// </summary>
        int RespecAmountSingleSkill { get; set; }
        
        /// <summary>
        /// Perform a full character reset to level 1
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Respec a single specialization line
        /// </summary>
        bool RespecSingleSkill(string skillName);
    }

    /// <summary>
    /// Interface for champion level progression
    /// DAoC Rule: Champion levels provide additional character advancement
    /// </summary>
    public interface IChampionLevelContainer
    {
        /// <summary>
        /// Current champion level
        /// </summary>
        int ChampionLevel { get; set; }
        
        /// <summary>
        /// Champion experience points
        /// </summary>
        long ChampionExperience { get; set; }
        
        /// <summary>
        /// Maximum champion level allowed
        /// </summary>
        int ChampionMaxLevel { get; }
        
        /// <summary>
        /// Whether character gains champion experience
        /// </summary>
        bool GainChampionExperience { get; set; }
    }

    /// <summary>
    /// Interface for crafting and trade skills
    /// DAoC Rule: Crafting skills are separate from combat skills
    /// </summary>
    public interface ICraftingCharacter
    {
        /// <summary>
        /// Get crafting skill level
        /// </summary>
        int GetCraftingSkillValue(eCraftingSkill skill);
        
        /// <summary>
        /// Set crafting skill level
        /// </summary>
        void SetCraftingSkillValue(eCraftingSkill skill, int value);
        
        /// <summary>
        /// Maximum crafting skill allowed
        /// </summary>
        int MaxCraftingSkillValue { get; }
        
        /// <summary>
        /// Whether character can craft items
        /// </summary>
        bool CanCraft { get; }
    }
} 