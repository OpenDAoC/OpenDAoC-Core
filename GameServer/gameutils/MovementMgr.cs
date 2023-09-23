/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS.Movement
{
    /// <summary>
    /// TODO: instead movement manager we need AI when npc should travel on path and attack 
    /// enemies if they are near and after that return to pathing for example.
    /// this current implementation is incomplete but usable for horses
    /// </summary>
    public class MovementMgr
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static Dictionary<string, DbPaths> m_pathCache = new Dictionary<string, DbPaths>();
		private static Dictionary<string, SortedList<int, DbPathPoints>> m_pathpointCache = new Dictionary<string, SortedList<int, DbPathPoints>>();
		private static object LockObject = new object();
		/// <summary>
		/// Cache all the paths and pathpoints
		/// </summary>
		private static void FillPathCache()
		{
			IList<DbPaths> allPaths = GameServer.Database.SelectAllObjects<DbPaths>();
			foreach (DbPaths path in allPaths)
			{
				m_pathCache.Add(path.PathID, path);
			}

			int duplicateCount = 0;

			IList<DbPathPoints> allPathPoints = GameServer.Database.SelectAllObjects<DbPathPoints>();
			foreach (DbPathPoints pathPoint in allPathPoints)
			{
				if (m_pathpointCache.ContainsKey(pathPoint.PathID))
				{
					if (m_pathpointCache[pathPoint.PathID].ContainsKey(pathPoint.Step) == false)
					{
						m_pathpointCache[pathPoint.PathID].Add(pathPoint.Step, pathPoint);
					}
					else
					{
						duplicateCount++;
					}
				}
				else
				{
					SortedList<int, DbPathPoints> pList = new SortedList<int, DbPathPoints>();
					pList.Add(pathPoint.Step, pathPoint);
					m_pathpointCache.Add(pathPoint.PathID, pList);
				}
			}
            if (duplicateCount > 0)
                log.ErrorFormat("{0} duplicate steps ignored while loading paths.", duplicateCount);
			log.InfoFormat("Path cache filled with {0} paths.", m_pathCache.Count);
		}

		public static void UpdatePathInCache(string pathID)
		{
			log.DebugFormat("Updating path {0} in path cache.", pathID);

			var dbpath = DOLDB<DbPaths>.SelectObject(DB.Column("PathID").IsEqualTo(pathID));
			if (dbpath != null)
			{
				if (m_pathCache.ContainsKey(pathID))
				{
					m_pathCache[pathID] = dbpath;
				}
				else
				{
					m_pathCache.Add(dbpath.PathID, dbpath);
				}
			}

			var pathPoints = DOLDB<DbPathPoints>.SelectObjects(DB.Column("PathID").IsEqualTo(pathID));
			SortedList<int, DbPathPoints> pList = new SortedList<int, DbPathPoints>();
			if (m_pathpointCache.ContainsKey(pathID))
			{
				m_pathpointCache[pathID] = pList;
			}
			else
			{
				m_pathpointCache.Add(pathID, pList);
			}

			foreach (DbPathPoints pathPoint in pathPoints)
			{
				m_pathpointCache[pathPoint.PathID].Add(pathPoint.Step, pathPoint);
			}
		}

        /// <summary>
        /// loads a path from the cache
        /// </summary>
        /// <param name="pathID">path to load</param>
        /// <returns>first pathpoint of path or null if not found</returns>
        public static PathPoint LoadPath(string pathID)
        {
        	lock(LockObject)
        	{
	        	if (m_pathCache.Count == 0)
				{
					FillPathCache();
				}
	
				DbPaths dbpath = null;
	
				if (m_pathCache.ContainsKey(pathID))
				{
					dbpath = m_pathCache[pathID];
				}
	
				// even if path entry not found see if pathpoints exist and try to use it
	
	            EPathType pathType = EPathType.Once;
	
	            if (dbpath != null)
	            {
	                pathType = (EPathType)dbpath.PathType;
	            }
	
				SortedList<int, DbPathPoints> pathPoints = null;
	
				if (m_pathpointCache.ContainsKey(pathID))
				{
					pathPoints = m_pathpointCache[pathID];
				}
				else
				{
					pathPoints = new SortedList<int, DbPathPoints>();
				}
	
	            PathPoint prev = null;
	            PathPoint first = null;
	
				foreach (DbPathPoints pp in pathPoints.Values)
				{
					PathPoint p = new PathPoint(pp.X, pp.Y, pp.Z, (short) pp.MaxSpeed, pathType);
					p.WaitTime = pp.WaitTime;
	
					if (first == null)
					{
						first = p;
					}
					p.Prev = prev;
					if (prev != null)
					{
						prev.Next = p;
					}
					prev = p;
				}

            	return first;
        	}
        }

        /// <summary>
        /// Saves the path into the database
        /// </summary>
        /// <param name="pathID">The path ID</param>
        /// <param name="path">The path waypoint</param>
        public static void SavePath(string pathID, PathPoint path)
        {
            if (path == null)
                return;

            pathID.Replace('\'', '/');

			// First delete any path with this pathID from the database

			var dbpath = DOLDB<DbPaths>.SelectObject(DB.Column("PathID").IsEqualTo(pathID));
			if (dbpath != null)
			{
				GameServer.Database.DeleteObject(dbpath);
			}

			GameServer.Database.DeleteObject(DOLDB<DbPathPoints>.SelectObjects(DB.Column("PathID").IsEqualTo(pathID)));

			// Now add this path and iterate through the PathPoint linked list to add all the path points

            PathPoint root = FindFirstPathPoint(path);

            //Set the current pathpoint to the rootpoint!
            path = root;
            dbpath = new DbPaths(pathID, root.Type);
            GameServer.Database.AddObject(dbpath);

            int i = 1;
            do
            {
                DbPathPoints dbpp = new DbPathPoints(path.X, path.Y, path.Z, path.MaxSpeed);
                dbpp.Step = i++;
                dbpp.PathID = pathID;
                dbpp.WaitTime = path.WaitTime;
                GameServer.Database.AddObject(dbpp);
                path = path.Next;
            }
			while (path != null && path != root);

			UpdatePathInCache(pathID);
        }

        /// <summary>
        /// Searches for the first point in the waypoints chain
        /// </summary>
        /// <param name="path">One of the pathpoints</param>
        /// <returns>The first pathpoint in the chain or null</returns>
        public static PathPoint FindFirstPathPoint(PathPoint path)
        {
            PathPoint root = path;
            // avoid circularity
            int iteration = 50000;
            while (path.Prev != null && path.Prev != root)
            {
                path = path.Prev;
                iteration--;
                if (iteration <= 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Path cannot be saved, it seems endless");
                    return null;
                }
            }
            return path;
        }
    }
}