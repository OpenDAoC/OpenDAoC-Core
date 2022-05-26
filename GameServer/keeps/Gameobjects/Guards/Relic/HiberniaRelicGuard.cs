using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;
using DOL.GS.Keeps;

//DO NOT REMOVE the initializers or the relic guards won't spawn!
namespace DOL.GS
{
    public class CrauchonRGInit : GameNPC
    {
        public CrauchonRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "Crauchon Relic Guards Init";
            CrauchonRGBrain brain = new CrauchonRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Hibernia Relic Guards [Crauchon] initialised");
        }
    }
    
    public class CrimthainRGInit : GameNPC
    {
        public CrimthainRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "Crimthain Relic Guards Init";
            CrimthainRGBrain brain = new CrimthainRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Hibernia Relic Guards [Crimthain] initialised");
        }
    }
    
    public class nGedRGInit : GameNPC
    {
        public nGedRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "nGed Relic Guards Init";
            nGedRGBrain brain = new nGedRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Hibernia Relic Guards [nGed] initialised");
        }
    }
}

#region brains
namespace DOL.AI.Brain
{
    public class CrauchonRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 100; // Crauchon

        private string m_guardName = "Crauchon Guardian";
        public CrauchonRGBrain()
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
                if (relic.Realm == eRealm.Hibernia)
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
                guard.Heading = 1000;
                guard.Realm = eRealm.Hibernia;
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
    
    public class CrimthainRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 101; // Crimthain

        private string m_guardName = "Crimthain Guardian";
        public CrimthainRGBrain()
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
                if (relic.Realm == eRealm.Hibernia)
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
                guard.Heading = 1000;
                guard.Realm = eRealm.Hibernia;
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
    
    public class nGedRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 103; // nGed

        private string m_guardName = "nGed Guardian";
        public nGedRGBrain()
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
                if (relic.Realm == eRealm.Hibernia)
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
                guard.Heading = 1000;
                guard.Realm = eRealm.Hibernia;
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

