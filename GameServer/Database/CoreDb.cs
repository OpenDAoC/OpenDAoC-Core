using System.Collections.Generic;
using Core.Database;

namespace Core.GS.Database;

public class CoreDb<T> where T : DataObject
{
    public static IList<T> SelectAllObjects()
        => GameServer.Database.SelectAllObjects<T>();

    public static T SelectObject(WhereClause whereClause) 
        => GameServer.Database.SelectObject<T>(whereClause);

    public static IList<T> SelectObjects(WhereClause whereClause)
        => GameServer.Database.SelectObjects<T>(whereClause);

    public static IList<IList<T>> MultipleSelectObjects(IEnumerable<WhereClause> whereClauseBatch)
        => GameServer.Database.MultipleSelectObjects<T>(whereClauseBatch);
}