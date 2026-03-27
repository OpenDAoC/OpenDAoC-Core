using System.Collections.Concurrent;
using DOL.Logging;

namespace DOL.GS
{
    public static class ConsignmentStateManager
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly ConcurrentDictionary<string, ConsignmentState> _states = new();

        public static ConsignmentState GetState(GameConsignmentMerchant consignmentMerchant)
        {
            string ownerId = consignmentMerchant.GetOwner();
            return GetState(ownerId);
        }

        public static ConsignmentState GetState(string key)
        {
            return string.IsNullOrEmpty(key) ? null : _states.GetOrAdd(key, id => new(id));
        }

        public static void RemoveState(string ownerId, ConsignmentState stateInstance)
        {
            if (!_states.TryRemove(new(ownerId, stateInstance)) && log.IsDebugEnabled)
                log.Debug($"Consignment state was already replaced before removal, or never existed. (OwnerId: {ownerId})");
        }
    }
}
