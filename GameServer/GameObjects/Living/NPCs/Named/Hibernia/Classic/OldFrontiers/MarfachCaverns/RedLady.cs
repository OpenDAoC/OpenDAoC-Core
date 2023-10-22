using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS;

#region Red Lady
public class RedLady : GameEpicBoss
{
    public RedLady() : base()
    {
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Red Lady initialized..");
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
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
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
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8819);
        LoadTemplate(npcTemplate);
        
        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        SpecialInnocent.InnocentCount = 0;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 67, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 380, 67, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 379, 67);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 381, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 382, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 443, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 468, 67, 94);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);

        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        RedLadyBrain redladybrain = new RedLadyBrain();
        SetOwnBrain(redladybrain);
        base.AddToWorld();
        return true;
    }
    public override void Die(GameObject killer)
    {
        base.Die(killer);

        foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
        {
            if (npc.Brain is SpecialInnocentBrain)
            {
                npc.RemoveFromWorld();
            }
        }
    }
}
#endregion Red Lady

#region Innocent adds
public class SpecialInnocent : GameNpc
{
    public SpecialInnocent() : base()
    {
    }
    public override void OnAttackEnemy(AttackData ad)
    {
        if (Util.Chance(5))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
            {
                CastSpell(Innocent_Disease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        base.OnAttackEnemy(ad);
    }
    public static int InnocentCount = 0;
    public override bool AddToWorld()
    {
        Model = (ushort)Util.Random(442, 446);
        Size = 50;
        Level = (byte)Util.Random(34, 38);
        Name = "summoned innocent";
        Realm = ERealm.None;
        MaxDistance = 0;
        TetherRange = 0;
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

        ++InnocentCount;
        Strength = 50;
        Dexterity = 120;
        Constitution = 100;
        Quickness = 98;
        SpecialInnocentBrain innocentbrain = new SpecialInnocentBrain();
        SetOwnBrain(innocentbrain);
        base.AddToWorld();
        return true;
    }
    public override void Die(GameObject killer)
    {
        --InnocentCount;
        base.Die(killer);
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 200;
    }
    public override long ExperienceValue => 0;
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash:
            case EDamageType.Crush:
            case EDamageType.Thrust: return 25;
            default: return 15;
        }
    }
    public override int MaxHealth
    {
        get { return 1000; }
    }
    public Spell m_Innocent_Disease;
    public Spell Innocent_Disease
    {
        get
        {
            if (m_Innocent_Disease == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 4375;
                spell.Icon = 4375;
                spell.TooltipId = 4375;
                spell.Duration = 120;
                spell.Name = "Disease";
                spell.Radius = 100;
                spell.Range = 1500;
                spell.SpellID = 11789;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.Disease.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Matter;
                m_Innocent_Disease = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Innocent_Disease);
            }
            return m_Innocent_Disease;
        }
    }
}
#endregion Innocent adds