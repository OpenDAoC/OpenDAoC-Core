using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.GS.Housing;
using DOL.GS.ServerProperties;
using DOL.Logging;
using static DOL.GS.RolloverSchedulerService;

namespace DOL.GS
{
    public class HouseRentService
    {
        public static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Initialize()
        {
            // We check every hour, CheckRents only processes houses whose individual RENT_DUE_DAYS timer has expired.
            RolloverSchedulerService.Instance.Subscribe(IntervalKey.Hourly, CheckRents);
        }

        private static readonly Queue<House> _removalQueue = new(); // Populated by ProcessHouseRent.
        private static readonly Lock _removalLock = new();

        private static void CheckRents()
        {
            if (Properties.RENT_DUE_DAYS == 0)
                return;

            List<House> houses = HouseMgr.GetHouses();

            if (houses.Count == 0)
                return;

            GameLoop.ExecuteForEach(houses, houses.Count, ProcessHouseRent);

            while (_removalQueue.TryDequeue(out House house))
                HouseMgr.RemoveHouse(house);
        }

        private static void ProcessHouseRent(House house)
        {
            if (string.IsNullOrEmpty(house.OwnerID) || house.NoPurge)
                return;

            long rent = HouseMgr.GetRentByModel(house.Model);

            if (rent <= 0)
                return;

            DateTime now = DateTime.Now;
            TimeSpan diff = now - house.LastPaid;

            if (diff.Days < Properties.RENT_DUE_DAYS)
                return;

            long lockboxAmount = house.KeptMoney;

            // Try to pull from the lockbox first.
            if (lockboxAmount >= rent)
            {
                house.KeptMoney -= rent;
                house.LastPaid = now;
                house.SaveIntoDatabase();
                return;
            }

            GameConsignmentMerchant consignmentMerchant = house.ConsignmentMerchant;
            long consignmentAmount = 0;

            // Try to pull from the consignment merchant.
            if (consignmentMerchant != null)
            {
                consignmentAmount = consignmentMerchant.TotalMoney;
                long remainingDifference = rent - lockboxAmount;

                if (remainingDifference <= consignmentAmount)
                {
                    // TotalMoney delegates to ConsignmentState and immediately saves the consignment merchant.
                    house.KeptMoney = 0;
                    consignmentMerchant.TotalMoney -= remainingDifference;
                    house.LastPaid = now;
                    house.SaveIntoDatabase();
                    return;
                }
            }

            // If we reached here, they can't afford rent.
            if (log.IsWarnEnabled)
                log.Warn($"[HOUSING] House {house.HouseNumber} owned by {house.Name} can't afford rent and is being repossessed! rentAmount: {rent} lockboxAmount: {lockboxAmount} consignmentAmount: {consignmentAmount}");

            lock (_removalLock)
            {
                _removalQueue.Enqueue(house);
            }
        }
    }
}
