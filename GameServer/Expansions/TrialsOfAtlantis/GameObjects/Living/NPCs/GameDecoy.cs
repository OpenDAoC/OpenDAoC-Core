using Core.GS.AI.Brains;

namespace Core.GS.Expansions.TrialsOfAtlantis;

public class GameDecoy : GameNpc
{
    public GameDecoy()
    {
        SetOwnBrain(new BlankBrain());
        this.MaxSpeedBase = 0;
    }
    public override void Die(GameObject killer)
    {
        DeleteFromDatabase();
        Delete();
    }
    private GamePlayer m_owner;
    public GamePlayer Owner
    {
        get { return m_owner; }
        set { m_owner = value; }
    }
    public override int MaxHealth
    {
        get { return 1; }
    }
}