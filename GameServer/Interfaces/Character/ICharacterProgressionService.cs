using DOL.GS.Interfaces.Core;
using System.Collections.Generic;

namespace DOL.GS.Interfaces.Character
{
    /// <summary>
    /// Service for managing character progression (leveling, experience, etc.)
    /// </summary>
    public interface ICharacterProgressionService : IGameService
    {
        void LevelUp(ICharacter character, int levels = 1);
        void GrantExperience(ICharacter character, long experience);
        void GrantRealmPoints(ICharacter character, long realmPoints);
        void GrantChampionExperience(ICharacter character, int experience);
    }

    /// <summary>
    /// Calculator for experience values and bonuses
    /// </summary>
    public interface IExperienceCalculator
    {
        long GetExperienceForLevel(int level);
        double CalculateGroupBonus(IGroup group);
        double CalculateCampBonus(ICharacter killer, INPC target);
        long CalculateExperienceReward(ICharacter killer, INPC target);
    }

    /// <summary>
    /// Service for managing specializations - uses OpenDAoC specialization system
    /// </summary>
    public interface ISpecializationService
    {
        int GetSpecializationLevel(ICharacter character, string specializationKey); // Uses Specs constants
        bool CanTrainSpecialization(ICharacter character, string specializationKey, int level);
        void TrainSpecialization(ICharacter character, string specializationKey, int level);
        int GetAvailablePoints(ICharacter character);
    }

    /// <summary>
    /// Calculator for specialization points
    /// </summary>
    public interface ISpecializationCalculator
    {
        double CalculatePointsForLevel(ICharacter character, int level);
        double CalculateTotalPoints(ICharacter character);
        double CalculateTotalPointsWithBonus(ICharacter character);
    }

    /// <summary>
    /// Calculator for stat progression - uses OpenDAoC eStat enum
    /// </summary>
    public interface IStatProgressionCalculator
    {
        int CalculateStatGain(ICharacter character, eStat stat, int level);
        void ApplyLevelStatGains(ICharacter character, int fromLevel, int toLevel);
    }

    /// <summary>
    /// Service for champion levels
    /// </summary>
    public interface IChampionLevelService
    {
        bool CanGainChampionLevels(ICharacter character);
        int GetTotalPointsForLevel(int championLevel);
        IList<IAbility> GetUnlockedAbilities(ICharacter character);
        void GrantChampionLevel(ICharacter character);
    }

    /// <summary>
    /// Calculator for realm ranks
    /// </summary>
    public interface IRealmRankCalculator
    {
        (int rank, int level) CalculateRealmRank(long realmPoints);
        int GetAbilityPointsForRank(int rank, int level);
        int GetBonusHitPoints(int rank, int level);
        long GetRealmPointsForRank(int rank, int level);
    }

    /// <summary>
    /// Service for realm rank abilities
    /// </summary>
    public interface IRealmRankService
    {
        bool HasRealmRankAbility(ICharacter character);
        IList<IRealmAbility> GetAvailableAbilities(ICharacter character);
        bool PurchaseRealmAbility(ICharacter character, IRealmAbility ability);
    }

    /// <summary>
    /// Extended character interface - uses OpenDAoC enums
    /// </summary>
    public interface ICharacter : ILiving
    {
        eCharacterClass CharacterClass { get; } // Use OpenDAoC character class enum
        IStats BaseStats { get; }
        IStats ModifiedStats { get; }
        ISpecializationList Specializations { get; }
        IAbilityList Abilities { get; }
        IInventory Inventory { get; }
        IQuestLog QuestLog { get; }
        
        // Progression properties
        long Experience { get; }
        int RealmRank { get; }
        long RealmPoints { get; }
        int ChampionLevel { get; }
        int ChampionExperience { get; }
        int BonusSpecPoints { get; set; }
    }

    /// <summary>
    /// Character class interface - uses OpenDAoC enums
    /// </summary>
    public interface ICharacterClass
    {
        eCharacterClass ID { get; } // Use OpenDAoC character class enum
        string Name { get; }
        int ClassType { get; } // Use int to avoid enum conflicts
        eStat PrimaryStat { get; } // Use OpenDAoC stat enum
        eStat SecondaryStat { get; }
        eStat TertiaryStat { get; }
        eStat ManaStat { get; }
        int SpecializationMultiplier { get; }
        int BaseHP { get; }
        int WeaponSkillBase { get; }
        IList<string> AllowedRaces { get; }
        IList<IAbility> GetAbilitiesAtLevel(int level);
        IList<ISpecialization> GetSpecializations();
    }

    /// <summary>
    /// Group interface for experience bonuses
    /// </summary>
    public interface IGroup
    {
        int MemberCount { get; }
        IList<ICharacter> Members { get; }
        double GetGroupBonus();
    }

    /// <summary>
    /// NPC interface for experience calculations
    /// </summary>
    public interface INPC : ILiving
    {
        bool HasBeenKilledInArea { get; set; }
    }

    /// <summary>
    /// Realm ability interface
    /// </summary>
    public interface IRealmAbility : IAbility
    {
        int Cost { get; }
        int MaxLevel { get; }
        eRealm Realm { get; } // Use OpenDAoC realm enum
    }

    /// <summary>
    /// Specialization list interface
    /// </summary>
    public interface ISpecializationList
    {
        ISpecialization GetSpecialization(string keyName); // Uses Specs constants
        IList<ISpecialization> GetAll();
        void AddSpecialization(ISpecialization spec);
        void RemoveSpecialization(string keyName);
    }

    /// <summary>
    /// Ability list interface
    /// </summary>
    public interface IAbilityList
    {
        IAbility GetAbility(string keyName);
        IList<IAbility> GetAll();
        void AddAbility(IAbility ability);
        void RemoveAbility(string keyName);
    }

    /// <summary>
    /// Quest log interface
    /// </summary>
    public interface IQuestLog
    {
        IList<IQuest> ActiveQuests { get; }
        IList<IQuest> CompletedQuests { get; }
        bool AddQuest(IQuest quest);
        bool RemoveQuest(IQuest quest);
        bool CompleteQuest(IQuest quest);
    }

    /// <summary>
    /// Quest interface
    /// </summary>
    public interface IQuest
    {
        string ID { get; }
        string Name { get; }
        string Description { get; }
        int MinLevel { get; }
        int MaxLevel { get; }
        eRealm Realm { get; } // Use OpenDAoC realm enum
        bool IsComplete { get; }
    }

    // Supporting interfaces
    public enum SkillType
    {
        Spec,
        Ability,
        Spell,
        Style,
        Song,
        Other
    }

    public interface ISkill
    {
        string Name { get; }
        SkillType Type { get; }
        int Level { get; }
    }

    public enum InventorySlot
    {
        RightHand,
        LeftHand,
        TwoHanded,
        Ranged,
        FirstBackpack,
        LastBackpack,
        FirstVault,
        LastVault
    }
} 