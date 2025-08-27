using System;
using System.Collections.Generic;
using System.Threading;
using DOL.Events;
using DOL.GS.Keeps;

namespace DOL.GS.GameEvents
{
    public class RelicGuardManager
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Lock _lock = new();
        private static int _albAddedGuardsCount = 0;
        private static int _midAddedGuardsCount = 0;
        private static int _hibAddedGuardsCount = 0;
        private static bool _firstRun = true;
        private static Dictionary<eRealm, List<GuardPair>> _guards = new()
        {
            { eRealm.Albion, new() },
            { eRealm.Midgard, new() },
            { eRealm.Hibernia, new() }
        };

        [GameServerStartedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs arguments)
        {
            CreateGuards();
            Init();
            GameEventMgr.AddHandler(KeepEvent.KeepTaken, new DOLEventHandler(Notify));
            GameEventMgr.AddHandler(RelicPadEvent.RelicMounted, new DOLEventHandler(NotifyRelic));
            GameEventMgr.AddHandler(RelicPadEvent.RelicStolen, new DOLEventHandler(NotifyRelic));
            log.Info("Relic Guards System initialized");
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnload(DOLEvent e, object sender, EventArgs args)
        {
            foreach (List<GuardPair> realmGuards in _guards.Values)
            {
                foreach (GuardPair guardPair in realmGuards)
                {
                    guardPair.Guard1?.Delete();
                    guardPair.Guard2?.Delete();
                }
            }
        }

        private static void CreateGuards()
        {
            AddGuard(eRealm.Albion,   "Renaris Knight",     "Chevalier.de.Caer.Renaris",    1,   56,  (601801, 428234, 5645, 2055), null);
            AddGuard(eRealm.Albion,   "Hurbury Knight",     "Chevalier.de.Caer.Hurbury",    1,   55,  null,                         (508047, 310852, 6832, 3638));
            AddGuard(eRealm.Albion,   "Berckstead Knight",  "Chevalier.de.Caer.Berckstead", 1,   51,  (602200, 428233, 5539, 2035), (508642, 310348, 6832, 3638));
            AddGuard(eRealm.Albion,   "Sursbrooke Knight",  "Chevalier.de.Caer.Sursbrooke", 1,   54,  (601527, 428233, 5628, 2062), (507894, 310323, 6832, 3638));
            AddGuard(eRealm.Albion,   "Boldiam Knight",     "Chevalier.de.Caer.Boldiam",    1,   53,  (601769, 427991, 5544, 2055), (508354, 310593, 6832, 3638));
            AddGuard(eRealm.Albion,   "Erasleigh Knight",   "Chevalier.de.Caer.Erasleigh",  1,   52,  (601096, 428255, 5606, 2035), (507705, 311142, 6832, 3638));
            AddGuard(eRealm.Albion,   "Benowyc Knight",     "Chevalier.de.Caer.Benowyc",    1,   50,  (601595, 427989, 5547, 2055), (508253, 310751, 6832, 3640));
            AddGuard(eRealm.Midgard,  "Fensalir Jarl",      "Jarl.de.Fensalir.Faste",       100, 80,  null,                         (771263, 628729, 6992, 3989));
            AddGuard(eRealm.Midgard,  "Arvakr Jarl",        "Jarl.de.Arvakr.Faste",         100, 81,  (678770, 710252, 6912, 2963), null);
            AddGuard(eRealm.Midgard,  "Glenlock Jarl",      "Jarl.de.Glenlock.Faste",       100, 79,  (679248, 710497, 6836, 2950), (769816, 627702, 6992, 923));
            AddGuard(eRealm.Midgard,  "Blendrake Jarl",     "Jarl.de.Blendrake.Faste",      100, 78,  (679164, 711149, 6868, 3418), (770036, 628191, 6941, 503));
            AddGuard(eRealm.Midgard,  "Hlidskialf Jarl",    "Jarl.de.Hlidskialf.Faste",     100, 77,  (679128, 709873, 6787, 2950), (770615, 628037, 6992, 521));
            AddGuard(eRealm.Midgard,  "Nottmoor Jarl",      "Jarl.de.Nottmoor.Faste",       100, 76,  (678654, 709122, 6912, 2511), (770531, 628482, 6985, 503));
            AddGuard(eRealm.Midgard,  "Bledmeer Jarl",      "Jarl.de.Bledmeer.Faste",       100, 75,  (679121, 710193, 6873, 2965), (770308, 628344, 6969, 513));
            AddGuard(eRealm.Hibernia, "Ailinne Sentinel",   "Sentinelle.de.Dun.Ailinne",    200, 106, (348542, 371526, 4880, 2052), null);
            AddGuard(eRealm.Hibernia, "Scathaig Sentinel",  "Sentinelle.de.Dun.Scathaig",   200, 105, (401953, 464351, 2888, 2567), null);
            AddGuard(eRealm.Hibernia, "nGed Sentinel",      "Sentinelle.de.Dun.nGed",       200, 103, (348211, 370945, 4784, 2212), (402193, 463725, 2854, 2565));
            AddGuard(eRealm.Hibernia, "Bolg Sentinel",      "Sentinelle.de.Dun.Bolg",       200, 102, (347374, 371175, 4809, 1690), (401172, 463380, 2884, 2239));
            AddGuard(eRealm.Hibernia, "Behnn Sentinel",     "Sentinelle.de.Dun.Behnn",      200, 104, (348841, 371000, 4688, 2012), (402899, 465176, 2848, 2978));
            AddGuard(eRealm.Hibernia, "Crimthain Sentinel", "Sentinelle.de.Dun.Crimthain",  200, 101, (349191, 371458, 4877, 2253), (402337, 463993, 2862, 2565));
            AddGuard(eRealm.Hibernia, "Crauchon Sentinel",  "Sentinelle.de.Dun.Crauchon",   200, 100, (348634, 371189, 4825, 2057), (402670, 464400, 2888, 2621));
        }

        private static void AddGuard(eRealm realm, string name, string translationId, ushort regionId, int keepId, (int x, int y, int z, ushort heading)? position1, (int x, int y, int z, ushort heading)? position2)
        {
            GuardFighterRK guard1 = position1.HasValue ? CreateGuard(position1.Value) : null;
            GuardFighterRK guard2 = position2.HasValue ? CreateGuard(position2.Value) : null;
            _guards[realm].Add(new(keepId, guard1, guard2, name));

            GuardFighterRK CreateGuard(in (int x, int y, int z, ushort heading) position)
            {
                GuardFighterRK guard = new()
                {
                    X = position.x,
                    Y = position.y,
                    Z = position.z,
                    Heading = position.heading,
                    CurrentRegionID = regionId,
                    Name = name,
                    TranslationId = translationId
                };

                foreach (IArea area in guard.CurrentAreas)
                {
                    if (area is not KeepArea keepArea)
                        continue;

                    guard.Component = new()
                    {
                        Keep = keepArea.Keep
                    };
                    break;
                }

                GuardTemplateMgr.RefreshTemplate(guard);
                return guard;
            }
        }

        public static void Init()
        {
            int albRelicCount = 0;
            int midRelicCount = 0;
            int hibRelicCount = 0;
            int albGuardPercentRelic = 100;
            int midGuardPercentRelic = 100;
            int hibGuardPercentRelic = 100;
            int albMaxGuards = 12;
            int midMaxGuards = 12;
            int hibMaxGuards = 12;

            lock (_lock)
            {
                try
                {
                    foreach (GameRelic relic in RelicMgr.getNFRelics())
                    {
                        switch (relic.Realm)
                        {
                            case eRealm.Albion:
                                albRelicCount++;
                                break;
                            case eRealm.Midgard:
                                midRelicCount++;
                                break;
                            case eRealm.Hibernia:
                                hibRelicCount++;
                                break;
                        }
                    }

                    if (albRelicCount > 2)
                    {
                        for (int i = 2; i < albRelicCount; i++)
                            albGuardPercentRelic -= 25;
                    }

                    if (midRelicCount > 2)
                    {
                        for (int i = 2; i < midRelicCount; i++)
                            midGuardPercentRelic -= 25;
                    }

                    if (hibRelicCount > 2)
                    {
                        for (int i = 2; i < hibRelicCount; i++)
                            hibGuardPercentRelic -= 25;
                    }

                    if (log.IsDebugEnabled)
                        log.Debug($"{nameof(albGuardPercentRelic)}={albGuardPercentRelic} {nameof(midGuardPercentRelic)}={midGuardPercentRelic} {nameof(hibGuardPercentRelic)}={hibGuardPercentRelic}");

                    albMaxGuards = albMaxGuards * albGuardPercentRelic / 100;
                    midMaxGuards = midMaxGuards * midGuardPercentRelic / 100;
                    hibMaxGuards = hibMaxGuards * hibGuardPercentRelic / 100;

                    if (log.IsDebugEnabled)
                        log.Debug($"{nameof(albMaxGuards)}={albMaxGuards} {nameof(midMaxGuards)}={midMaxGuards} {nameof(hibMaxGuards)}={hibMaxGuards}");
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error(e);
                }
            }

            foreach (GuardPair guardPair in _guards[eRealm.Albion])
                HandleGuardPair(guardPair, ref _albAddedGuardsCount, albMaxGuards);

            foreach (GuardPair guardPair in _guards[eRealm.Midgard])
                HandleGuardPair(guardPair, ref _midAddedGuardsCount, midMaxGuards);

            foreach (GuardPair guardPair in _guards[eRealm.Hibernia])
                HandleGuardPair(guardPair, ref _hibAddedGuardsCount, hibMaxGuards);

            if (log.IsDebugEnabled)
                log.Debug($"{nameof(_albAddedGuardsCount)}={_albAddedGuardsCount} {nameof(_midAddedGuardsCount)}={_midAddedGuardsCount} {nameof(_hibAddedGuardsCount)}={_hibAddedGuardsCount}");

            _firstRun = false;

            static void HandleGuardPair(GuardPair guardPair, ref int addedGuardsCount, int maxGuards)
            {
                AbstractGameKeep keep = GameServer.KeepManager.GetKeepByID(guardPair.KeepId);

                if (keep == null)
                    return;

                GuardFighterRK guard1 = guardPair.Guard1;
                GuardFighterRK guard2 = guardPair.Guard2;
                string guardName = guardPair.Name;

                if (keep.Realm == keep.OriginalRealm)
                {
                    if (IsGuardNotInWorld(guard1) && IsGuardNotInWorld(guard2))
                    {
                        AddGuardToWorld(guard1, ref addedGuardsCount, maxGuards, $"{guardName}(1)");
                        AddGuardToWorld(guard2, ref addedGuardsCount, maxGuards, $"{guardName}(2)");
                    }
                    else
                    {
                        ManageGuardInWorld(guard1, ref addedGuardsCount, maxGuards, $"{guardName}(1)");
                        ManageGuardInWorld(guard2, ref addedGuardsCount, maxGuards, $"{guardName}(2)");
                    }
                }
                else if (!_firstRun)
                {
                    RemoveGuardFromWorld(guard1, ref addedGuardsCount, $"{guardName}(1)");
                    RemoveGuardFromWorld(guard2, ref addedGuardsCount, $"{guardName}(2)");
                }

                bool IsGuardNotInWorld(GuardFighterRK guard) => guard != null && guard.ObjectState is not GameObject.eObjectState.Active;

                void AddGuardToWorld(GuardFighterRK guard, ref int count, int max, string name)
                {
                    if (guard != null && count < max)
                    {
                        guard.AddToWorld();
                        count++;

                        if (log.IsInfoEnabled)
                            log.Info($"{name} added");
                    }
                }

                void ManageGuardInWorld(GuardFighterRK guard, ref int count, int max, string name)
                {
                    if (guard == null)
                        return;

                    if (count > max)
                    {
                        if ((guard.IsAlive && guard.ObjectState is GameObject.eObjectState.Active) || guard.IsRespawning)
                        {
                            if (guard.IsRespawning)
                                guard.StopRespawn();
                            else
                                guard.Delete();

                            count--;

                            if (log.IsInfoEnabled)
                                log.Info($"{name} removed");
                        }
                    }
                    else if (count < max)
                    {
                        if (!guard.IsRespawning && guard.ObjectState is not GameObject.eObjectState.Active)
                        {
                            guard.AddToWorld();
                            count++;

                            if (log.IsInfoEnabled)
                                log.Info($"{name} added");
                        }
                    }
                }

                void RemoveGuardFromWorld(GuardFighterRK guard, ref int count, string name)
                {
                    if (guard != null && ((guard.IsAlive && guard.ObjectState is GameObject.eObjectState.Active) || guard.IsRespawning))
                    {
                        if (guard.IsRespawning)
                            guard.StopRespawn();
                        else
                            guard.Delete();

                        count--;
                        if (log.IsInfoEnabled)
                            log.Info($"{name} removed");
                    }
                }
            }
        }

        public static void NotifyRelic(DOLEvent e, object sender, EventArgs args)
        {
            if (e != RelicPadEvent.RelicStolen && e != RelicPadEvent.RelicMounted)
                return;

            Init();
        }

        public static void Notify(DOLEvent e, object sender, EventArgs args)
        {
            if (e != KeepEvent.KeepTaken)
                return;

            KeepEventArgs keepEvent = args as KeepEventArgs;

            if (keepEvent.Keep.CurrentZone.IsOF == false)
                return;

            Init();
        }

        private class GuardPair
        {
            public int KeepId { get; }
            public GuardFighterRK Guard1 { get; }
            public GuardFighterRK Guard2 { get; }
            public string Name { get; }

            public GuardPair(int keepId, GuardFighterRK guard1, GuardFighterRK guard2, string name)
            {
                KeepId = keepId;
                Guard1 = guard1;
                Guard2 = guard2;
                Name = name;
            }
        }
    }
}