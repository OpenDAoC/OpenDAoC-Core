﻿using System;
using Core.AI.Brain;
using Core.Events;
using Core.GS;
using Core.GS.AI.Brains;

namespace Core.GS
{
    public class NightSpawnMob : GameNpc
    {
        public override bool AddToWorld()
        {
            NightSpawnBrain sBrain = new NightSpawnBrain();
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
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Night mobs initialising...");
        }
    }
}

namespace Core.AI.Brain
{
    public class NightSpawnBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        ushort oldModel;
        ENpcFlags oldFlags;
        bool changed;

        public override void Think()
        {
            if (!Body.CurrentRegion.IsNightTime)
            {
                if (!changed)
                {
                    oldFlags = Body.Flags;
                    Body.Flags |= ENpcFlags.CANTTARGET;
                    Body.Flags |= ENpcFlags.DONTSHOWNAME;
                    Body.Flags |= ENpcFlags.PEACE;

                    if (oldModel == 0)
                    {
                        oldModel = Body.Model;
                    }

                    Body.Model = 1;

                    changed = true;
                }
            }
            if (Body.CurrentRegion.IsNightTime)
            {
                if (changed)
                {
                    Body.Flags = oldFlags;
                    Body.Model = oldModel;
                    changed = false;
                }
                
                if (Body.Name == "shadow")
                    Body.Flags |= ENpcFlags.GHOST;
            }

            base.Think();
        }
    }
}