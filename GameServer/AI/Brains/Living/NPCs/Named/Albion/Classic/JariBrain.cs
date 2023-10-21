namespace Core.GS.AI.Brains;

public class JariBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public JariBrain() : base()
    {
        ThinkInterval = 1000;
    }
    public override void Think()
    {
        base.Think();
    }
}