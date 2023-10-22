namespace Core.GS.AI.Brains;

public class CorpseSummonerBrain : KeepGuardBrain
{
    public CorpseSummonerBrain()
        : base()
    {
        AggroLevel = 90;
        AggroRange = 1500;
    }
    
    /*public override void Think()
    {
        base.Think();
        if ((Body as GuardCorpseSummoner).Component.Keep != null && (Body as GuardCorpseSummoner).Component.Keep.Level < 10)
        {
            Body.RemoveFromWorld();
            (Body as GuardCorpseSummoner).StartModifiedRespawn();
        }
    }*/
}