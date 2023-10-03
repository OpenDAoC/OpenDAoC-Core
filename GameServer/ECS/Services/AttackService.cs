﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class AttackService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(AttackService);

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<AttackComponent> list = EntityManager.UpdateAndGetAll<AttackComponent>(EntityManager.EntityType.AttackComponent, out int lastValidIndex);

            Parallel.For(0, lastValidIndex + 1, i =>
            {
                AttackComponent attackComponent = list[i];

                try
                {
                    if (attackComponent?.EntityManagerId.IsSet != true)
                        return;
                    long startTick = GameLoop.GetCurrentTime();
                    attackComponent.Tick(tick);
                    long stopTick = GameLoop.GetCurrentTime();

                    if (stopTick - startTick > 25)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {attackComponent.owner.Name}({attackComponent.owner.ObjectID}) Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, attackComponent, attackComponent.owner);
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
