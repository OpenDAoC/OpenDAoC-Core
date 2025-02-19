using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Logging;
using OpenTelemetry.Metrics;

namespace DOL.GS.Metrics.Meters
{
    public class CurrencyMeterProvider : IMeterProvider
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        public string MeterName => "GameServer.CurrencyMetrics";

        public void Register(MeterProviderBuilder meterProviderBuilder)
        {
            meterProviderBuilder.AddMeter(MeterName);
            Meter meter = new(MeterName);

            var copperTotal = meter.CreateObservableGauge(
                "daoc.copper.total",
                TotalCopper,
                description: "Total amount of copper ingame"
            );
        }

        /// <summary>
        /// Get the current ingame money
        /// </summary>
        /// <returns>The total amount of money in copper</returns>
        private static long TotalCopper()
        {
            try
            {
                List<DbAccountXMoney> money = [.. GameServer.Database.SelectAllObjects<DbAccountXMoney>()];
                long copper = money.Sum(m => m.Copper);
                long silver = money.Sum(m => m.Silver);
                long gold = money.Sum(m => m.Gold);
                long platinum = money.Sum(m => m.Platinum);
                long mithril = money.Sum(m => m.Mithril);

                platinum += MithrilToPlatinum(mithril);
                gold += PlatinumToGold(platinum);
                silver += GoldToSilver(gold);
                copper += SilverToCopper(silver);

                return copper;

                long MithrilToPlatinum(long mithril)
                {
                    return mithril * 1000L;
                }

                long PlatinumToGold(long platinum)
                {
                    return platinum * 1000L;
                }

                long GoldToSilver(long gold)
                {
                    return gold * 100L;
                }

                long SilverToCopper(long silver)
                {
                    return silver * 100L;
                }
            }
            catch (Exception ex)
            {
                log.Error("MetricsCollector.CollectMetrics threw an exception", ex);
            }

            return 0;
        }
    }
}