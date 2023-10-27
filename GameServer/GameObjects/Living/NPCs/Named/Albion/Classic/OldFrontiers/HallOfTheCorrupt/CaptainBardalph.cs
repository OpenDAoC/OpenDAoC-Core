﻿using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.Styles;
using Core.GS.World;

namespace Core.GS;

public class CaptainBardalph : GameEpicBoss
{
    public CaptainBardalph() : base()
    {
    }
    
    public static int AfterEvadeID = 145;
    public static int AfterEvadeClassID = 11;
    public static Style AfterEvade = SkillBase.GetStyleByID(AfterEvadeID, AfterEvadeClassID);

    public static int TauntID = 130;
    public static int TauntClassID = 11;
    public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);
    public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
    {
        base.OnAttackedByEnemy(ad);
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if (Util.Chance(35))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
            {
                CastSpell(Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        base.OnAttackEnemy(ad);
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 30; // dmg reduction for rest resists
        }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 350;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override int MaxHealth
    {
        get { return 30000; }
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (IsOutOfTetherRange)
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                    damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
                    damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                    || damageType == EDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            else
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override bool AddToWorld()
    {
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        BodyType = (ushort)EBodyType.Humanoid;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 186, 0, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 188, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 187, 0);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 189, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 190, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 653, 0, 0);
        template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 25, 0, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.Standard);
        if (!Styles.Contains(AfterEvade))
            Styles.Add(AfterEvade);
        if (!Styles.Contains(Taunt))
            Styles.Add(Taunt);

        EvadeChance = 50;
        MaxDistance = 2000;
        TetherRange = 1500;
        MaxSpeedBase = 225;
        Gender = EGender.Female;
        Flags = ENpcFlags.GHOST;
        VisibleActiveWeaponSlots = 16;
        MeleeDamageType = EDamageType.Thrust;
        CaptainBardalphBrain sbrain = new CaptainBardalphBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Captain Bardalph", 277, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Captain Heathyr not found, creating it...");

            log.Warn("Initializing Captain Bardalph...");
            CaptainBardalph HOC = new CaptainBardalph();
            HOC.Name = "Captain Bardalph";
            HOC.Model = 55;
            HOC.Realm = 0;
            HOC.Level = 65;
            HOC.Size = 50;
            HOC.CurrentRegionID = 277; //hall of the corrupt
            HOC.RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            HOC.Faction = FactionMgr.GetFactionByID(187);
            HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            HOC.Strength = 5;
            HOC.Constitution = 100;
            HOC.Dexterity = 200;
            HOC.Quickness = 145;
            HOC.Empathy = 200;

            HOC.X = 34604;
            HOC.Y = 35842;
            HOC.Z = 14134;
            HOC.Heading = 1008;
            CaptainBardalphBrain ubrain = new CaptainBardalphBrain();
            HOC.SetOwnBrain(ubrain);
            HOC.AddToWorld();
            HOC.SaveIntoDatabase();
            HOC.Brain.Start();
        }
        else
            log.Warn("Captain Bardalph exist ingame, remove it and restart server if you want to add by script code.");
    }
    private Spell m_Bleed;

    private Spell Bleed
    {
        get
        {
            if (m_Bleed == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 2;
                spell.ClientEffect = 2130;
                spell.Icon = 3411;
                spell.TooltipId = 3411;
                spell.Damage = 65;
                spell.Name = "Bleed";
                spell.Description = "Does 65 damage to a target every 3 seconds for 30 seconds.";
                spell.Message1 = "You are bleeding! ";
                spell.Message2 = "{0} is bleeding! ";
                spell.Duration = 30;
                spell.Frequency = 30;
                spell.Range = 350;
                spell.SpellID = 11780;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.StyleBleeding.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Body;
                m_Bleed = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bleed);
            }
            return m_Bleed;
        }
    }
}