using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
    public class Alina : GameNPC
    {
        public override bool AddToWorld()
        {
            AlinaModelBrain sBrain = new AlinaModelBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Alina initialising...");
        }
    }
}

namespace DOL.AI.Brain
{
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
                    Body.LoadEquipmentTemplateFromDatabase("");
                    changed = false;
                }
            }
            base.Think();
        }
    }
}