﻿using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
    public class DaySpawn : GameNPC
    {
        public override bool AddToWorld()
        {
            DaySpawnBrain sBrain = new DaySpawnBrain();
            if (NPCTemplate != null)
            {
                sBrain.AggroLevel = NPCTemplate.AggroLevel;
                sBrain.AggroRange = NPCTemplate.AggroRange;
            }
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Day mobs initialising...");
        }
    }
}

namespace DOL.AI.Brain
{
    public class DaySpawnBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        ushort oldModel;
        GameNPC.eFlags oldFlags;
        bool changed;

        public override void Think()
        {
            if (Body.CurrentRegion.IsNightTime)
            {
                if (changed == false)
                {
                    oldFlags = Body.Flags;
                    Body.Flags ^= GameNPC.eFlags.CANTTARGET;
                    Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
                    Body.Flags ^= GameNPC.eFlags.PEACE;

                    if (oldModel == 0)
                    {
                        oldModel = Body.Model;
                    }

                    Body.Model = 1;

                    changed = true;
                }
            }
            if (Body.CurrentRegion.IsNightTime == false)
            {
                if (changed)
                {
                    Body.Flags = oldFlags;
                    Body.Model = oldModel;
                    changed = false;
                }
            }

            base.Think();
        }
    }
}