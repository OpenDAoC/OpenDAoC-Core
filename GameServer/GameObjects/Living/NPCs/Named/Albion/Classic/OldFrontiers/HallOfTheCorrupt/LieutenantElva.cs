using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Styles;

namespace Core.GS;

public class LieutenantElva : GameNpc
{
    public LieutenantElva() : base()
    {
    }
    public static int TauntID = 342;
    public static int TauntClassID = 9;
    public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

    public static int TauntFollowUpID = 344;
    public static int TauntFollowUpClassID = 9;
    public static Style TauntFollowUp = SkillBase.GetStyleByID(TauntFollowUpID, TauntFollowUpClassID);

    public static int AfterEvadeID = 340;
    public static int AfterEvadeClassID = 9;
    public static Style AfterEvade = SkillBase.GetStyleByID(AfterEvadeID, AfterEvadeClassID);

    public static int EvadeFollowUpID = 345;
    public static int EvadeFollowUpClassID = 9;
    public static Style EvadeFollowUp = SkillBase.GetStyleByID(EvadeFollowUpID, EvadeFollowUpClassID);
    public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
    {
        if(ad != null && ad.AttackResult == EAttackResult.Evaded)
        {
            this.styleComponent.NextCombatBackupStyle = AfterEvade;
            this.styleComponent.NextCombatStyle = EvadeFollowUp;
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if(ad != null && ad.AttackResult == EAttackResult.HitUnstyled)
        {
            this.styleComponent.NextCombatBackupStyle = Taunt;
            this.styleComponent.NextCombatStyle = TauntFollowUp;
        }
        if (ad != null && ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 342 && ad.Style.ClassID == 9)
        {
            this.styleComponent.NextCombatBackupStyle = Taunt;
            this.styleComponent.NextCombatStyle = TauntFollowUp;
        }
        if (ad != null && ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 340 && ad.Style.ClassID == 9)
        {
            this.styleComponent.NextCombatBackupStyle = Taunt;
            this.styleComponent.NextCombatStyle = EvadeFollowUp;
        }
        if (Util.Chance(35))
        {
            if (!ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
            {
                if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
                {
                    this.CastSpell(Poison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
        }
        base.OnAttackEnemy(ad);
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 35; // dmg reduction for melee dmg
            case EDamageType.Crush: return 35; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
            default: return 25; // dmg reduction for rest resists
        }
    }

    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (this.IsOutOfTetherRange)
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
        return base.AttackDamage(weapon) * Strength / 150;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (this.IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 500;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.35;
    }
    public override int MaxHealth
    {
        get { return 5000; }
    }
    public override bool AddToWorld()
    {
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        BodyType = (ushort)EBodyType.Humanoid;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 134, 0, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 136, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 135, 0);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 137, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 138, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
        template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 3, 0, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.Standard);
        if (!this.Styles.Contains(TauntFollowUp))
        {
            Styles.Add(TauntFollowUp);
        }
        if (!this.Styles.Contains(Taunt))
        {
            Styles.Add(Taunt);
        }
        if (!this.Styles.Contains(AfterEvade))
        {
            Styles.Add(AfterEvade);
        }
        if (!this.Styles.Contains(EvadeFollowUp))
        {
            Styles.Add(EvadeFollowUp);
        }
        Strength = 50;
        Constitution = 100;
        Dexterity = 200;
        Quickness = 145;
        IsCloakHoodUp = true;
        EvadeChance = 50;
        MaxDistance = 2000;
        TetherRange = 1500;
        MaxSpeedBase = 225;
        Gender = EGender.Female;
        Flags = ENpcFlags.GHOST;
        VisibleActiveWeaponSlots = 16;
        MeleeDamageType = EDamageType.Slash;
        LieutenantElvaBrain sbrain = new LieutenantElvaBrain();
        SetOwnBrain(sbrain);
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Lieutenant Elva", 277, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Lieutenant Elva not found, creating it...");

            log.Warn("Initializing Lieutenant Elva...");
            LieutenantElva HOC = new LieutenantElva();
            HOC.Name = "Lieutenant Elva";
            HOC.Model = 5;
            HOC.Realm = 0;
            HOC.Level = 50;
            HOC.Size = 50;
            HOC.CurrentRegionID = 277; //hall of the corrupt
            HOC.Faction = FactionMgr.GetFactionByID(187);
            HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            HOC.Strength = 50;
            HOC.Constitution = 100;
            HOC.Dexterity = 200;
            HOC.Quickness = 145;
            HOC.MaxSpeedBase = 225;
            HOC.X = 31358;
            HOC.Y = 35631;
            HOC.Z = 15365;
            HOC.Heading = 2044;
            LieutenantElvaBrain ubrain = new LieutenantElvaBrain();
            HOC.SetOwnBrain(ubrain);
            HOC.AddToWorld();
            HOC.SaveIntoDatabase();
            HOC.Brain.Start();
        }
        else
            log.Warn("Lieutenant Elva exist ingame, remove it and restart server if you want to add by script code.");
    }
    private Spell m_Poison;

    private Spell Poison
    {
        get
        {
            if (m_Poison == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 2;
                spell.ClientEffect = 4099;
                spell.Icon = 4099;
                spell.TooltipId = 4099;
                spell.Damage = 55;
                spell.Name = "Poison";
                spell.Description = "Inflicts damage to the target repeatly over a given time period.";
                spell.Message1 = "You are afflicted with a vicious poison!";
                spell.Message2 = "{0} has been poisoned!";
                spell.Duration = 30;
                spell.Frequency = 30;
                spell.Range = 350;
                spell.SpellID = 11781;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DamageOverTime.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Body;
                m_Poison = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Poison);
            }

            return m_Poison;
        }
    }
}