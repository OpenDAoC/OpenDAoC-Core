using System;

namespace DOL.GS
{
    public static class TickExecutorExtensions
    {
        public static void ExecuteView<T>(this ITickExecutor executor, in ServiceObjectView<T> view, Action<T> action)
            where T : class, IServiceObject
        {
            if (view.TotalValidCount <= 0)
                return;

            if (view.IsSharded)
                executor.ExecuteForEachSharded(view.Shards, view.ShardStartIndices, view.TotalValidCount, action);
            else
                executor.ExecuteForEach(view.Items, view.TotalValidCount, action);
        }
    }
}
