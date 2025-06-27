using System;
using System.Collections.Generic;

namespace DOL.GS.Interfaces
{
    /// <summary>
    /// Interface for spell lines available to characters
    /// </summary>
    public interface ISpellLine
    {
        string KeyName { get; }
        string Name { get; }
        string Spec { get; }
        bool IsBaseLine { get; }
        int Level { get; }
    }

    /// <summary>
    /// Interface for realm abilities
    /// </summary>
    public interface IRealmAbility
    {
        string KeyName { get; }
        string Name { get; }
        string Description { get; }
        int Level { get; }
        int MaxLevel { get; }
        int CostForUpgrade { get; }
        eRealm Realm { get; }
    }

    /// <summary>
    /// Interface for player titles
    /// </summary>
    public interface IPlayerTitle
    {
        string GetValue(GamePlayer source, GamePlayer target);
        string GetDescription(GamePlayer source, GamePlayer target);
        bool IsSuitable(GamePlayer player);
    }

    /// <summary>
    /// Interface for crafting skills
    /// </summary>
    public interface ICraftingSkill
    {
        eCraftingSkill Skill { get; }
        int Level { get; }
        string Name { get; }
    }

    /// <summary>
    /// Interface for guild functionality
    /// </summary>
    public interface IGuild
    {
        string Name { get; }
        string Motd { get; }
        eRealm Realm { get; }
        int Level { get; }
        DateTime CreationDate { get; }
        int MemberCount { get; }
    }

    /// <summary>
    /// Enumeration for respec types
    /// </summary>
    public enum eRespecType
    {
        All,
        Single,
        Realm,
        Champion
    }

    /// <summary>
    /// Enumeration for crafting skills
    /// </summary>
    public enum eCraftingSkill
    {
        WeaponCrafting = 1,
        Armorcrafting = 2,
        Siegecrafting = 3,
        Alchemy = 4,
        MetalWorking = 5,
        LeatherCrafting = 6,
        ClothWorking = 7,
        GemCutting = 8,
        HerbalCrafting = 9,
        Tailoring = 10,
        SpellCrafting = 11,
        WoodWorking = 12
    }
} 