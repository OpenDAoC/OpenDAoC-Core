using Core.GS;

namespace Core.AI.Brain;

public class AlinaModelBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private bool changed;
    
    public override void Think()
    {
        if (Body.CurrentRegion.IsNightTime == false)
        {
            if (changed == false)
            {
                Body.Model = 220;
                Body.Name = "Alina";
                Body.Level = 19;
                Body.Realm = ERealm.Midgard;
                Body.LoadEquipmentTemplateFromDatabase("Alina");
                changed = true;
            }
        }
        if (Body.CurrentRegion.IsNightTime)
        {
            if (changed)
            {
                Body.Model = 395;
                Body.Name = "Noble Werewolf Alina";
                Body.Level = 22;
                Body.Realm = ERealm.None;
                Body.LoadEquipmentTemplateFromDatabase("");
                changed = false;
            }
        }
        base.Think();
    }
}