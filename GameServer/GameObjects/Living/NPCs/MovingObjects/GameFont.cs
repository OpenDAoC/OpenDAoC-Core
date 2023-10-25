using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS;

public class GameFont : GameMovingObject
{
    public GameFont()
    {
        SetOwnBrain(new BlankBrain());
        this.Realm = 0;
        this.Level = 1;
        this.MaxSpeedBase = 0;
        this.Flags |= ENpcFlags.DONTSHOWNAME;
        this.Health = this.MaxHealth;
    }

    private GamePlayer m_owner;
    public GamePlayer Owner
    {
        get { return m_owner; }
        set { m_owner = value; }
    }
    public override int MaxHealth
    {
        get { int hp=500; if(Name.ToLower().IndexOf("speed")>=0) hp=100; return hp; }
    }
    public virtual int CalculateToHitChance(GameLiving target)
    {
        int spellLevel = m_owner.Level;
        GameLiving caster = m_owner as GameLiving;
        int spellbonus = m_owner.GetModified(EProperty.SpellLevel);
        spellLevel += spellbonus;
        if (spellLevel > 50)
            spellLevel = 50;
        int hitchance = 85 + ((spellLevel - target.Level) / 2);
        return hitchance;
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (damageType == EDamageType.Slash || damageType == EDamageType.Crush || damageType == EDamageType.Thrust)
        {
            damageAmount /= 10;
            criticalAmount /= 10;
        }
        else
        {
            damageAmount /= 25;
            criticalAmount /= 25;
        }
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
    }

    public override void Die(GameObject killer)
    {
        DeleteFromDatabase();
        Delete();
    }
}