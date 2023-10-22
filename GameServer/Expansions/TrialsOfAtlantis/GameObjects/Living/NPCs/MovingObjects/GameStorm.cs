using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS.Expansions.TrialsOfAtlantis;

public class GameStorm : GameMovingObject
{
    public GameStorm()
    {
        SetOwnBrain(new BlankBrain());
        this.Realm = 0;
        this.Level = 60;
        this.MaxSpeedBase = 191;
        this.Model = 3457;
        this.Name = "Storm";
        this.Flags |= ENpcFlags.DONTSHOWNAME;
        this.Flags |= ENpcFlags.CANTTARGET;
        this.Movable = true;
    }

    private GamePlayer m_owner;
    public GamePlayer Owner
    {
        get { return m_owner; }
        set { m_owner = value; }
    }

    private bool m_movable;
    public bool Movable
    {
        get { return m_movable; }
        set { m_movable = value; }
    }

    public override int MaxHealth
    { 
        get { return 10000; } 
    }

    public override void Die(GameObject killer)
    {
        DeleteFromDatabase();
        Delete();
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
    }
}