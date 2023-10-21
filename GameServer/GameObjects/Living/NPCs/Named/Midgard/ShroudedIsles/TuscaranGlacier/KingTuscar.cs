using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;
using Core.GS.Styles;

namespace Core.GS;

public class KingTuscar : GameEpicBoss
{
    public KingTuscar() : base() { }
    #region Styles declaration
    public static int TauntID = 167;
    public static int TauntClassID = 22;//warrior
    public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

    public static int AfterParryID = 173;
    public static int AfterParryClassID = 22;
    public static Style after_parry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);

    public static int ParryFollowupID = 175;
    public static int ParryFollowupClassID = 22;
    public static Style parry_followup = SkillBase.GetStyleByID(ParryFollowupID, ParryFollowupClassID);

    public static int AfterBlockID = 302;
    public static int AfterBlockClassID = 44;
    public static Style after_block = SkillBase.GetStyleByID(AfterBlockID, AfterBlockClassID);
    #endregion
    #region Resists and TakeDamage()
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 40;// dmg reduction for melee dmg
            case EDamageType.Crush: return 40;// dmg reduction for melee dmg
            case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
            default: return 70;// dmg reduction for rest resists
        }
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (IsOutOfTetherRange)
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                    || damageType == EDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            else
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                
                foreach (GameNpc npc in GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.Brain is QueenKulaBrain && npc.HealthPercent < 100)
                            {
                                npc.Health += damageAmount + criticalAmount;
                                if (truc != null)
                                    truc.Out.SendMessage("Your damage is healing Queen Kula!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                            }
                        }
                    }
                }
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }
    #endregion
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
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
        get { return 300000; }
    }
    public static int KingTuscarCount = 0;
    public override void Die(GameObject killer)//on kill generate orbs
    {
        --KingTuscarCount;
        base.Die(killer);
    }
    #region Styles
    public override void OnAttackedByEnemy(AttackData ad)// on Boss actions
    {
        if(ad != null && ad.AttackResult == EAttackResult.Parried)
        {
            styleComponent.NextCombatBackupStyle = after_parry;//boss parried so prepare after parry style backup style
            styleComponent.NextCombatStyle = parry_followup;//main style after parry followup
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void OnAttackEnemy(AttackData ad)//on enemy actions
    {
        if (ad != null && ad.AttackResult == EAttackResult.HitStyle)
        {
            styleComponent.NextCombatBackupStyle = taunt;//taunt as backup style
            styleComponent.NextCombatStyle = parry_followup;//after parry style as main
        }
        if (ad != null && ad.AttackResult == EAttackResult.HitUnstyled)
        {
            styleComponent.NextCombatStyle = taunt;//boss hit unstyled so taunt
        }
        if (ad != null && ad.AttackResult == EAttackResult.Blocked)
        {
            styleComponent.NextCombatStyle = after_block;//target blocked boss attack so use after block style
            if(Util.Chance(50))
                CastSpell(Hammers_aoe2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//aoe mjolnirs after style big dmg
        }
        if (ad != null && ad.AttackResult == EAttackResult.Parried)
        {
            if (Util.Chance(50))
                CastSpell(Thunder_aoe2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//aoe mjolnirs after style big dmg
        }
        if (QueenKula.QueenKulaCount == 0 || (HealthPercent <= 50 && KingTuscarBrain.TuscarRage==true))
        {
            if (ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 175 && ad.Style.ClassID == 22)
            {
                CastSpell(Hammers_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//aoe mjolnirs after style big dmg
            }
            if (ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 302 && ad.Style.ClassID == 44)
            {
                CastSpell(Thunder_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//aoe lightining after style medium dmg
            }
            if (ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 173 && ad.Style.ClassID == 22)
            {
                CastSpell(Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//bleed after style low dot bleed dmg
            }
        }
        base.OnAttackEnemy(ad);
    }
    #endregion
    #region AddToWorld
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162909);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
        BodyType = (ushort)EBodyType.Giant;
        if(!Styles.Contains(taunt))
            Styles.Add(taunt);
        if (!Styles.Contains(after_parry))
            Styles.Add(after_parry);
        if (!Styles.Contains(parry_followup))
            Styles.Add(parry_followup);
        if (!Styles.Contains(after_block))
            Styles.Add(after_block);
        ++KingTuscarCount;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 575, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        KingTuscarBrain.message2 = false;
        KingTuscarBrain.TuscarRage = false;
        KingTuscarBrain.IsPulled2 = false;

        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        KingTuscarBrain sbrain = new KingTuscarBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false;//load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }       
    #endregion
    #region Spells
    private Spell m_Hammers_aoe;
    private Spell Hammers_aoe
    {
        get
        {
            if (m_Hammers_aoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 3;
                spell.ClientEffect = 3541;
                spell.Icon = 3541;
                spell.TooltipId = 3541;
                spell.Damage = 600;
                spell.Name = "Mjolnir's Fury";
                spell.Radius = 500;
                spell.Range = 350;
                spell.SpellID = 11752;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Energy;
                m_Hammers_aoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hammers_aoe);
            }
            return m_Hammers_aoe;
        }
    }
    private Spell m_Thunder_aoe;
    private Spell Thunder_aoe
    {
        get
        {
            if (m_Thunder_aoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 2;
                spell.ClientEffect = 3528;
                spell.Icon = 3528;
                spell.TooltipId = 3528;
                spell.Damage = 350;
                spell.Name = "Thor's Might";
                spell.Radius = 500;
                spell.Range = 350;
                spell.SpellID = 11753;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Energy;
                m_Thunder_aoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Thunder_aoe);
            }
            return m_Thunder_aoe;
        }
    }
    private Spell m_Hammers_aoe2;
    private Spell Hammers_aoe2
    {
        get
        {
            if (m_Hammers_aoe2 == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 5;
                spell.ClientEffect = 3541;
                spell.Icon = 3541;
                spell.TooltipId = 3541;
                spell.Damage = 500;
                spell.Name = "Mjolnir's Fury";
                spell.Radius = 500;
                spell.Range = 350;
                spell.SpellID = 11890;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Energy;
                m_Hammers_aoe2 = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hammers_aoe2);
            }
            return m_Hammers_aoe2;
        }
    }
    private Spell m_Thunder_aoe2;
    private Spell Thunder_aoe2
    {
        get
        {
            if (m_Thunder_aoe2 == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 5;
                spell.ClientEffect = 3528;
                spell.Icon = 3528;
                spell.TooltipId = 3528;
                spell.Damage = 400;
                spell.Name = "Thor's Might";
                spell.Radius = 500;
                spell.Range = 350;
                spell.SpellID = 11891;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Energy;
                m_Thunder_aoe2 = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Thunder_aoe2);
            }
            return m_Thunder_aoe2;
        }
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
                spell.Name = "Scar of Gods";
                spell.Description = "Does 65 damage to a target every 3 seconds for 36 seconds.";
                spell.Message1 = "You are bleeding! ";
                spell.Message2 = "{0} is bleeding! ";
                spell.Duration = 36;
                spell.Frequency = 30;
                spell.Range = 350;
                spell.SpellID = 11754;
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
    #endregion
}