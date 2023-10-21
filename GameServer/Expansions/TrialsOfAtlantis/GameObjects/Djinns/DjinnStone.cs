namespace Core.GS.Expansions.TrialsOfAtlantis;

public class DjinnStone : GameStaticItem
{
    private AncientBoundDjinn m_djinn;
    
    /// <summary>
    /// The djinn bound to this stone.
    /// </summary>
    protected AncientBoundDjinn Djinn
    {
        get { return m_djinn;  }
        set { m_djinn = value; }
    }
}