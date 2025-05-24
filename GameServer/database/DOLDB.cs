using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;

namespace DOL.GS
{
    public class DOLDB<T> where T : DataObject
    {
        public static IList<T> SelectAllObjects()
        {
            return GameServer.Database.SelectAllObjects<T>();
        }

        public static async Task<IList<T>> SelectAllObjectsAsync()
        {
            return await Task.Factory.StartNew(
                static (state) => GameServer.Database.SelectAllObjects<T>(),
                null,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).ConfigureAwait(false);
        }

        public static T SelectObject(WhereClause whereClause)
        {
            return GameServer.Database.SelectObject<T>(whereClause);
        }

        public static async Task<T> SelectObjectAsync(WhereClause whereClause)
        {
            return await Task.Factory.StartNew(
                static (state) => GameServer.Database.SelectObject<T>(state as WhereClause),
                whereClause,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).ConfigureAwait(false);
        }

        public static IList<T> SelectObjects(WhereClause whereClause)
        {
            return GameServer.Database.SelectObjects<T>(whereClause);
        }

        public static async Task<IList<T>> SelectObjectsAsync(WhereClause whereClause)
        {
            return await Task.Factory.StartNew(
                static (state) => GameServer.Database.SelectObjects<T>(state as WhereClause),
                whereClause,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).ConfigureAwait(false);
        }

        public static IList<IList<T>> MultipleSelectObjects(IEnumerable<WhereClause> whereClauseBatch)
        {
            return GameServer.Database.MultipleSelectObjects<T>(whereClauseBatch);
        }

        public static async Task<IList<IList<T>>> MultipleSelectObjectsAsync(IEnumerable<WhereClause> whereClauseBatch)
        {
            return await Task.Factory.StartNew(
                static (state) => GameServer.Database.MultipleSelectObjects<T>(state as IEnumerable<WhereClause>),
                whereClauseBatch,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).ConfigureAwait(false);
        }

        public static void FillObjectRelations(T dataObject)
        {
            GameServer.Database.FillObjectRelations(dataObject);
        }

        public static async Task FillObjectRelationsAsync(T dataObject)
        {
            await Task.Factory.StartNew(
                static (state) => GameServer.Database.FillObjectRelations(state as T),
                dataObject,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).ConfigureAwait(false);
        }

        public static void FillObjectRelations(IEnumerable<T> dataObjects)
        {
            GameServer.Database.FillObjectRelations(dataObjects);
        }

        public static async Task FillObjectRelationsAsync(IEnumerable<T> dataObjects)
        {
            await Task.Factory.StartNew(
                static (state) => GameServer.Database.FillObjectRelations(state as IEnumerable<T>),
                dataObjects,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).ConfigureAwait(false);
        }
    }
}
