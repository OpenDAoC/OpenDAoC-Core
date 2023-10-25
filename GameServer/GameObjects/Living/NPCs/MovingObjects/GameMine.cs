using Core.GS.Enums;

namespace Core.GS;

public class GameMine : GameMovingObject
{
    public GameMine()
    {
        this.Realm = 0;
        this.Level = 1;
        this.Health = this.MaxHealth;
        this.MaxSpeedBase = 0;
    }

    private GamePlayer m_owner;
    public GamePlayer Owner
    {
        get { return m_owner; }
        set { m_owner = value; }
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
        if (source is GamePlayer)
        {
            damageAmount = 0;
            criticalAmount = 0;
        }
        if (Health - damageAmount - criticalAmount <= 0)
            this.Delete();
        else
            Health = Health - damageAmount - criticalAmount;

    }
}