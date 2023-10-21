namespace DOL.GS.Scripts;

public class HitPointMob : GameNpc
{
    
    public override bool AddToWorld()
    {
       
        Flags = 0;
        return base.AddToWorld();
    }

    
  
    public override int MaxHealth
    {
        get
        {
            return base.Charisma;
        }
    }

  
}