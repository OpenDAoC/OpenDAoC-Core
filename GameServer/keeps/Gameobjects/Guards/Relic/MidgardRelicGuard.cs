using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;
using DOL.GS.Keeps;

//DO NOT REMOVE the initializers or the relic guards won't spawn!
namespace DOL.GS
{
    public class BledmeerRGInit : GameNPC
    {
        public BledmeerRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "Bledmeer Relic Guards Init";
            BledmeerRGBrain brain = new BledmeerRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Midgard Relic Guards [Bledmeer] initialised");
        }
    }
    
    public class NotmoorRGInit : GameNPC
    {
        public NotmoorRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "Notmoor Relic Guards Init";
            NotmoorRGBrain brain = new NotmoorRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Midgard Relic Guards [Notmoor] initialised");
        }
    }
    
    public class GlenlockRGInit : GameNPC
    {
        public GlenlockRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "Glenlock Relic Guards Init";
            GlenlockRGBrain brain = new GlenlockRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Midgard Relic Guards [Glenlock] initialised");
        }
    }
}

#region brains
namespace DOL.AI.Brain
{
    public class BledmeerRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 75; // Bledmeer

        private string m_guardName = "Bledmeer Guardian";
        public BledmeerRGBrain()
            : base()
        {
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            var keep = GameServer.KeepManager.GetKeepByID(m_keepID);
            if (keep == null)
            {
                return;
            }
            if (keep.Realm == keep.OriginalRealm && !m_guardSpawned)
            {
                SpawnGuards();
                m_guardSpawned = true;
            }
            else if(keep.Realm != keep.OriginalRealm && m_guardSpawned)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc is not GuardFighter guard) continue;
                    if (guard.Name == m_guardName)
                    {
                        guard.Delete();
                    }
                }
                m_guardSpawned = false;
            }
            base.Think();
        }

        private void SpawnGuards()
        {
            var relics = RelicMgr.getNFRelics();
            var numRelics = 0;
            foreach (GameRelic relic in relics)
            {
                if (relic.Realm == eRealm.Midgard)
                {
                    numRelics++;
                }
            }
            var numGuards = 4 * (1 - 0.25 * (numRelics - 2));

            for (int i = 1; i < numGuards; i++)
            {
                var guard = new GuardFighter();
                guard.X = Body.X + Util.Random(-50, 50);
                guard.Y = Body.Y + Util.Random(-50, 50);
                guard.Z = Body.Z;
                guard.CurrentRegionID = Body.CurrentRegionID;
                guard.Heading = Body.Heading;
                guard.Realm = eRealm.Midgard;
                guard.LoadedFromScript = false;
                foreach (AbstractArea area in guard.CurrentAreas)
                {
                    if (area is KeepArea)
                    {
                        AbstractGameKeep keep = (area as KeepArea).Keep;
                        guard.Component = new GameKeepComponent();
                        guard.Component.Keep = keep;
                        break;
                    }
                }
                
                guard.AddToWorld();
                guard.RefreshTemplate();
                guard.Name = m_guardName;
            }
        }
    }
    
    public class NotmoorRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 76; // Notmoor

        private string m_guardName = "Notmoor Guardian";
        public NotmoorRGBrain()
            : base()
        {
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            var keep = GameServer.KeepManager.GetKeepByID(m_keepID);
            if (keep == null)
            {
                return;
            }
            if (keep.Realm == keep.OriginalRealm && !m_guardSpawned)
            {
                SpawnGuards();
                m_guardSpawned = true;
            }
            else if(keep.Realm != keep.OriginalRealm && m_guardSpawned)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc is not GuardFighter guard) continue;
                    if (guard.Name == m_guardName)
                    {
                        guard.Delete();
                    }
                }
                m_guardSpawned = false;
            }
            base.Think();
        }

        private void SpawnGuards()
        {
            var relics = RelicMgr.getNFRelics();
            var numRelics = 0;
            foreach (GameRelic relic in relics)
            {
                if (relic.Realm == eRealm.Midgard)
                {
                    numRelics++;
                }
            }
            var numGuards = 4 * (1 - 0.25 * (numRelics - 2));

            for (int i = 1; i < numGuards; i++)
            {
                var guard = new GuardFighter();
                guard.X = Body.X + Util.Random(-50, 50);
                guard.Y = Body.Y + Util.Random(-50, 50);
                guard.Z = Body.Z;
                guard.CurrentRegionID = Body.CurrentRegionID;
                guard.Heading = Body.Heading;
                guard.Realm = eRealm.Midgard;
                guard.LoadedFromScript = false;
                foreach (AbstractArea area in guard.CurrentAreas)
                {
                    if (area is KeepArea)
                    {
                        AbstractGameKeep keep = (area as KeepArea).Keep;
                        guard.Component = new GameKeepComponent();
                        guard.Component.Keep = keep;
                        break;
                    }
                }
                
                guard.AddToWorld();
                guard.RefreshTemplate();
                guard.Name = m_guardName;
            }
        }
    }
    
    public class GlenlockRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 79; // Glenlock

        private string m_guardName = "Glenlock Guardian";
        public GlenlockRGBrain()
            : base()
        {
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            var keep = GameServer.KeepManager.GetKeepByID(m_keepID);
            if (keep == null)
            {
                return;
            }
            if (keep.Realm == keep.OriginalRealm && !m_guardSpawned)
            {
                SpawnGuards();
                m_guardSpawned = true;
            }
            else if(keep.Realm != keep.OriginalRealm && m_guardSpawned)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc is not GuardFighter guard) continue;
                    if (guard.Name == m_guardName)
                    {
                        guard.Delete();
                    }
                }
                m_guardSpawned = false;
            }
            base.Think();
        }

        private void SpawnGuards()
        {
            var relics = RelicMgr.getNFRelics();
            var numRelics = 0;
            foreach (GameRelic relic in relics)
            {
                if (relic.Realm == eRealm.Midgard)
                {
                    numRelics++;
                }
            }
            var numGuards = 4 * (1 - 0.25 * (numRelics - 2));

            for (int i = 1; i < numGuards; i++)
            {
                var guard = new GuardFighter();
                guard.X = Body.X + Util.Random(-50, 50);
                guard.Y = Body.Y + Util.Random(-50, 50);
                guard.Z = Body.Z;
                guard.CurrentRegionID = Body.CurrentRegionID;
                guard.Heading = Body.Heading;
                guard.Realm = eRealm.Midgard;
                guard.LoadedFromScript = false;
                foreach (AbstractArea area in guard.CurrentAreas)
                {
                    if (area is KeepArea)
                    {
                        AbstractGameKeep keep = (area as KeepArea).Keep;
                        guard.Component = new GameKeepComponent();
                        guard.Component.Keep = keep;
                        break;
                    }
                }
                
                guard.AddToWorld();
                guard.RefreshTemplate();
                guard.Name = m_guardName;
            }
        }
    }
    
}
#endregion

