﻿using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;
using DOL.GS.Keeps;

//DO NOT REMOVE the initializers or the relic guards won't spawn!
namespace DOL.GS
{
    public class BenowycRGInit : GameNPC
    {
        public BenowycRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "Benowyc Relic Guards Init";
            BenowycRGBrain brain = new BenowycRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Albion  Relic Guards [Benowyc] initialised");
        }
    }
    
    public class BerksteadRGInit : GameNPC
    {
        public BerksteadRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "Berkstead Relic Guards Init";
            BerksteadRGBrain brain = new BerksteadRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Albion  Relic Guards [Benowyc] initialised");
        }
    }
    
    public class BoldiamRGInit : GameNPC
    {
        public BoldiamRGInit() : base()
        {
        }
        public override bool AddToWorld()
        {
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Name = "Boldiam Relic Guards Init";
            BoldiamRGBrain brain = new BoldiamRGBrain();
            SetOwnBrain(brain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("Albion  Relic Guards [Boldiam] initialised");
        }
    }
}

#region brains
namespace DOL.AI.Brain
{
    public class BenowycRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 51; // Benowyc

        private string m_guardName = "Benowyc Guardian";
        public BenowycRGBrain()
            : base()
        {
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            var benowyc = GameServer.KeepManager.GetKeepByID(m_keepID);
            if (benowyc == null)
            {
                return;
            }
            if (benowyc.Realm == benowyc.OriginalRealm && !m_guardSpawned)
            {
                SpawnGuards();
                m_guardSpawned = true;
            }
            else if(benowyc.Realm != benowyc.OriginalRealm && m_guardSpawned)
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
            var albRelics = 0;
            foreach (GameRelic relic in relics)
            {
                if (relic.Realm == eRealm.Albion)
                {
                    albRelics++;
                }
            }
            var numGuards = 4 * (1 - 0.25 * (albRelics - 2));

            for (int i = 1; i < numGuards; i++)
            {
                var guard = new GuardFighter();
                guard.X = Body.X + Util.Random(-50, 50);
                guard.Y = Body.Y + Util.Random(-50, 50);
                guard.Z = Body.Z;
                guard.CurrentRegionID = Body.CurrentRegionID;
                guard.Heading = 1000;
                guard.Realm = eRealm.Albion;
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
    
    public class BerksteadRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 50; // Berkstead

        private string m_guardName = "Berkstead Guardian";
        public BerksteadRGBrain()
            : base()
        {
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            var benowyc = GameServer.KeepManager.GetKeepByID(m_keepID);
            if (benowyc == null)
            {
                return;
            }
            if (benowyc.Realm == benowyc.OriginalRealm && !m_guardSpawned)
            {
                SpawnGuards();
                m_guardSpawned = true;
            }
            else if(benowyc.Realm != benowyc.OriginalRealm && m_guardSpawned)
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
            var albRelics = 0;
            foreach (GameRelic relic in relics)
            {
                if (relic.Realm == eRealm.Albion)
                {
                    albRelics++;
                }
            }
            var numGuards = 4 * (1 - 0.25 * (albRelics - 2));

            for (int i = 1; i < numGuards; i++)
            {
                var guard = new GuardFighter();
                guard.X = Body.X + Util.Random(-50, 50);
                guard.Y = Body.Y + Util.Random(-50, 50);
                guard.Z = Body.Z;
                guard.CurrentRegionID = Body.CurrentRegionID;
                guard.Heading = 1000;
                guard.Realm = eRealm.Albion;
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
    
    public class BoldiamRGBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_guardSpawned = false;
        private ushort m_keepID = 53; // Boldiam

        private string m_guardName = "Boldiam Guardian";
        public BoldiamRGBrain()
            : base()
        {
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            var benowyc = GameServer.KeepManager.GetKeepByID(m_keepID);
            if (benowyc == null)
            {
                return;
            }
            if (benowyc.Realm == benowyc.OriginalRealm && !m_guardSpawned)
            {
                SpawnGuards();
                m_guardSpawned = true;
            }
            else if(benowyc.Realm != benowyc.OriginalRealm && m_guardSpawned)
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
            var albRelics = 0;
            foreach (GameRelic relic in relics)
            {
                if (relic.Realm == eRealm.Albion)
                {
                    albRelics++;
                }
            }
            var numGuards = 4 * (1 - 0.25 * (albRelics - 2));

            for (int i = 1; i < numGuards; i++)
            {
                var guard = new GuardFighter();
                guard.X = Body.X + Util.Random(-50, 50);
                guard.Y = Body.Y + Util.Random(-50, 50);
                guard.Z = Body.Z;
                guard.CurrentRegionID = Body.CurrentRegionID;
                guard.Heading = 1000;
                guard.Realm = eRealm.Albion;
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

