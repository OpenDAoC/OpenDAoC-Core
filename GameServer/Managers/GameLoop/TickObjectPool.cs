using System.Reflection;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class TickObjectPool<T> : TickPool<T> where T : IPooledObject<T>, new()
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        protected override T CreateNew()
        {
            return new T();
        }

        protected override bool IsDirty(T item)
        {
            return item.IssuedTimestamp != 0;
        }

        protected override void LogDirtyItemWarning(T item)
        {
            if (log.IsWarnEnabled)
                log.Warn($"Item '{item}' was not released last tick (IssuedTimestamp: {item.IssuedTimestamp}) (CurrentTime: {GameLoop.GameLoopTime}).");
        }

        protected override void PrepareForUse(T item)
        {
            item.IssuedTimestamp = GameLoop.GameLoopTime;
        }

        protected override void OnResetItems(int itemsInUse)
        {
            // The consumer is responsible for resetting the object's state.
            // This pool only validates it via the IssuedTimestamp.
        }
    }
}
