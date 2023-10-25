using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS;

public class AncientBlackOak : GameEpicBoss
{
    public AncientBlackOak() : base()
    {
    }
    public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
    {
        base.OnAttackedByEnemy(ad);
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if (Util.Chance(50))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
            {
                CastSpell(OakDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
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
        get { return 40000; }
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
        return base.AttackDamage(weapon) * Strength / 100;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (this.IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public static int OakCount = 0;
    public override void Die(GameObject killer)
    {
        OakCount=0;
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8823);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        BodyType = (ushort)EBodyType.Plant;
        AncientBlackOakBrain.IsPulled = false;
        OakCount =1;
        MeleeDamageType = EDamageType.Matter;
        AncientBlackOakBrain sbrain = new AncientBlackOakBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = true;
        base.AddToWorld();
        return true;
    }
    private Spell m_OakDD;

    private Spell OakDD
    {
        get
        {
            if (m_OakDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 2;
                spell.ClientEffect = 11027;
                spell.Icon = 11027;
                spell.TooltipId = 11027;
                spell.Damage = 700;
                spell.Name = "Ancient Root";
                spell.Duration = 30;
                spell.Value = 60;
                spell.Radius = 800;
                spell.SpellID = 11790;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DamageSpeedDecreaseNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Body;
                m_OakDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OakDD);
            }

            return m_OakDD;
        }
    }
}
