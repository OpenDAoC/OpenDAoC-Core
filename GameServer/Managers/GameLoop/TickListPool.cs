using System.Collections.Generic;
using System.Reflection;
using DOL.Logging;

namespace DOL.GS
{
    public sealed class TickListPool<T> : TickPool<List<T>> where T : IPooledList<T>
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        protected override List<T> CreateNew()
        {
            return new();
        }

        protected override bool IsDirty(List<T> item)
        {
            return item.Count > 0;
        }

        protected override void LogDirtyItemWarning(List<T> item)
        {
            if (log.IsWarnEnabled)
                log.Warn($"List of type '{typeof(T)}' was not cleared last tick (Count: {item.Count}) (CurrentTime: {GameLoop.GameLoopTime}).");
        }

        protected override void PrepareForUse(List<T> item) { }

        protected override void OnResetItems(int itemsInUse)
        {
            for (int i = 0; i < itemsInUse; i++)
                _pool[i].Clear();
        }
    }
}
