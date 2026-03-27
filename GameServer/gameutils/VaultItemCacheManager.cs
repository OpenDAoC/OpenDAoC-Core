using System.Collections.Concurrent;
using DOL.Logging;

namespace DOL.GS
{
    public static class VaultItemCacheManager
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly ConcurrentDictionary<VaultItemCacheKey, VaultItemCache> _caches = new();

        public static VaultItemCache GetCache(GameVault vault)
        {
            string ownerId = vault.GetOwner();
            return string.IsNullOrEmpty(ownerId) ? null : GetCache(new VaultItemCacheKey(ownerId, vault.FirstDbSlot));
        }

        public static VaultItemCache GetCache(VaultItemCacheKey key)
        {
            return _caches.GetOrAdd(key, id => new(id));
        }

        public static void RemoveCache(VaultItemCacheKey key, VaultItemCache cache)
        {
            if (!_caches.TryRemove(new(key, cache)) && log.IsDebugEnabled)
                log.Debug($"Cache was already replaced before removal, or never existed. (key: {key})");
        }

        public readonly record struct VaultItemCacheKey(string OwnerId, int FirstDbSlot);
    }
}
