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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.GS.Utils;
using DOL.Language;
using log4net;

namespace DOL.GS
{
    /// <summary>
    /// This class represents one Zone in DAoC. It holds all relevant information that is needed to do different calculations.
    /// </summary>
    public class Zone : ITranslatableObject
    {
        #region Fields and Properties

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const ushort SUBZONE_NBR_ON_ZONE_SIDE = 32; // MUST BE A POWER OF 2 (current implementation limit is 128 inclusive).
        private const ushort SUBZONE_NBR = SUBZONE_NBR_ON_ZONE_SIDE * SUBZONE_NBR_ON_ZONE_SIDE;
        private const ushort SUBZONE_SIZE = 65536 / SUBZONE_NBR_ON_ZONE_SIDE;
        private static readonly ushort SUBZONE_SHIFT = (ushort)Math.Round(Math.Log(SUBZONE_SIZE) / Math.Log(2)); // To get log in base 2.
        private static readonly ushort SUBZONE_ARRAY_Y_SHIFT = (ushort)Math.Round(Math.Log(SUBZONE_NBR_ON_ZONE_SIDE) / Math.Log(2));

        public virtual LanguageDataObject.eTranslationIdentifier TranslationIdentifier => LanguageDataObject.eTranslationIdentifier.eZone;
        public string TranslationId { get => ID.ToString(); set { } }
        public eRealm Realm { get; private set; }
        public Region ZoneRegion { get; set; }
        public ushort ID { get; }
        public ushort ZoneSkinID { get; } // The ID we send to the client, for client-side positioning of gameobjects and npcs.
        public string Description { get; set; }
        public int XOffset { get; }
        public int YOffset { get; }
        public int Width { get; }
        public int Height { get; }
        public int Waterlevel { get; set; }
        public bool IsDivingEnabled { get; set; }
        public virtual bool IsLava { get; set; }
        public int ObjectCount => _objectCount;

        public bool IsDungeon
        {
            get
            {
                switch (ZoneRegion.ID)
                {
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                    case 24:
                    case 50:
                    case 60:
                    case 61:
                    case 65:
                    case 66:
                    case 67:
                    case 68:
                    case 69:
                    case 92:
                    case 93:
                    case 98:
                    case 109:
                    case 125:
                    case 126:
                    case 127:
                    case 128:
                    case 129:
                    case 149:
                    case 150:
                    case 160:
                    case 161:
                    case 180:
                    case 188:
                    case 190:
                    case 191:
                    case 196:
                    case 220:
                    case 221:
                    case 222:
                    case 223:
                    case 224:
                    case 227:
                    case 228:
                    case 229:
                    case 244:
                    case 245:
                    case 246:
                    case 247:
                    case 248:
                    case 249:
                    case 276:
                    case 277:
                    case 296:
                    case 297:
                    case 298:
                    case 326:
                    case 340:
                    // case 335:
                    case 352:
                    case 356:
                    case 376:
                    case 377:
                    case 379:
                    case 382:
                    case 383:
                    case 386:
                    case 387:
                    case 388:
                    case 390:
                    case 395:
                    case 396:
                    case 397:
                    case 415:
                    case 443:
                    case 489://lvl5-9 Demons breach
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsRvR
        {
            get
            {
                switch (ID)
                {
                    case 11://forest sauvage
                    case 12://snowdonia
                    case 14://pennine mountains
                    case 15://hadrian's wall
                    case 111://uppland
                    case 112://yggdra forest
                    case 113://jamtland mountains
                    case 115://odin's gate
                    case 210://mount collory
                    case 211://cruachan gorge
                    case 212://breifine
                    case 214://emain macha
                    case 233://summoner's hall
                    case 244://passage of conflict
                    case 246://dodens gruva
                    case 250://caledonia
                    case 251://murdaigean
                    case 252://thidranki
                    case 253://abermenai
                    case 249://darkness falls
                    case 276://marfach caverns
                    case 277://hall of the corrupt
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsOF
        {
            get
            {
                switch (ID)
                {
                    case 11://forest sauvage
                    case 12://snowdonia
                    case 14://pennine mountains
                    case 15://hadrian's wall
                    case 111://uppland
                    case 112://yggdra forest
                    case 113://jamtland mountains
                    case 115://odin's gate
                    case 210://mount collory
                    case 211://cruachan gorge
                    case 212://breifine
                    case 214://emain macha
                    case 233://summoner's hall
                    case 244://passage of conflict
                    case 246://dodens gruva
                    case 276://marfach caverns
                    case 277://hall of the corrupt
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsBG
        {
            get
            {
                switch (ID)
                {
                    case 250://caledonia
                    case 251://murdaigean
                    case 252://thidranki
                    case 253://abermenai
                        return true;
                    default:
                        return false;
                }
            }
        }

        public int BonusExperience { get; set; } = 0;
        public int BonusRealmpoints { get; set; } = 0;
        public int BonusBountypoints { get; set; } = 0;
        public int BonusCoin { get; set; } = 0;

        private SubZone[] _subZones = new SubZone[SUBZONE_NBR];
        private int _objectCount;
        private bool _initialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new Zone object.
        /// </summary>
        /// <param name="region">the parent region</param>
        /// <param name="id">the zone id (eg. 15)</param>
        /// <param name="desc">the zone description (eg. "Camelot Hills")</param>
        /// <param name="xoff">the X offset of this zone inside the region</param>
        /// <param name="yoff">the Y offset of this zone inside the region</param>
        /// <param name="width">the Width of this zone</param>
        /// <param name="height">the Height of this zone</param>
        /// <param name="zoneskinID">For clientside positioning in instances: The 'fake' zoneid we send to clients.</param>
        public Zone(Region region, ushort id, string desc, int xoff, int yoff, int width, int height, ushort zoneskinID, bool isDivingEnabled, int waterlevel, bool islava, int xpBonus, int rpBonus, int bpBonus, int coinBonus, byte realm)
        {
            ZoneRegion = region;
            ID = id;
            Description = desc;
            XOffset = xoff;
            YOffset = yoff;
            Width = width;
            Height = height;
            ZoneSkinID = zoneskinID;
            Waterlevel = waterlevel;
            IsDivingEnabled = isDivingEnabled;
            IsLava = islava;
            BonusExperience = xpBonus;
            BonusRealmpoints = rpBonus;
            BonusBountypoints = bpBonus;
            BonusCoin = coinBonus;
            Realm = (eRealm)realm;
        }

        public void Delete()
        {
            _subZones = null;
            ZoneRegion = null;
            Events.GameEventMgr.RemoveAllHandlersForObject(this);
        }

        private void InitializeZone()
        {
            if (_initialized)
                return;

            for (int i = 0; i < SUBZONE_NBR; i++)
                _subZones[i] = new SubZone(this);

            _initialized = true;
        }

        #endregion

        #region Subzone Management

        private static short GetSubZoneOffset(int lineSubZoneIndex, int columnSubZoneIndex)
        {
            return (short)(columnSubZoneIndex + (lineSubZoneIndex << SUBZONE_ARRAY_Y_SHIFT));
        }

        /// <summary>
        /// Returns the SubZone index using a position in the zone.
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <returns>The SubZoneIndex</returns>
        public short GetSubZoneIndex(int x, int y)
        {
            int xDiff = x - XOffset;
            int yDiff = y - YOffset;

            // Is the object out of the zone?
            if ((xDiff < 0) || (xDiff > 65535) || (yDiff < 0) || (yDiff > 65535))
                return -1;
            else
            {
                xDiff >>= SUBZONE_SHIFT;
                yDiff >>= SUBZONE_SHIFT;

                return GetSubZoneOffset(yDiff, xDiff);
            }
        }

        public SubZone GetSubZone(int subZoneIndex)
        {
            return _subZones[subZoneIndex];
        }

        public void AddObjectToZone(GameObject gameObject)
        {
            if (!_initialized)
                InitializeZone();

            eGameObjectType objectType;

            // Only GamePlayer, GameNPC and GameStaticItem classes are handled.
            if (gameObject is GamePlayer)
                objectType = eGameObjectType.PLAYER;
            else if (gameObject is GameNPC)
                objectType = eGameObjectType.NPC;
            else if (gameObject is GameStaticItem)
                objectType = eGameObjectType.ITEM;
            else if (gameObject is GameDoorBase)
                objectType = eGameObjectType.DOOR;
            else
                return;

            SubZoneObject subZoneObject = new(gameObject, null);
            SubZone subZone = _subZones[GetSubZoneIndex(gameObject.X, gameObject.Y)];
            LightConcurrentLinkedList<SubZoneObject>.Node node = new(subZoneObject);
            ObjectChangingSubZone objectChangingSubZone = new(node, objectType, this, subZone);
            EntityManager.Add(EntityManager.EntityType.ObjectChangingSubZone, objectChangingSubZone);
        }

        /// <summary>
        /// Gets the lists of objects, located in the current Zone and of the given type, that are at most at a 'radius' distance from (x,y,z).
        /// The found objects are appended to the given 'partialList'.
        /// </summary>
        /// <param name="objectType">the type of objects to look for</param>
        /// <param name="x">the x coordinate of the observation position</param>
        /// <param name="y">the y coordinate of the observation position</param>
        /// <param name="z">the z coordinate of the observation position</param>
        /// <param name="radius">the radius to check against</param>
        /// <param name="partialList">an initial (eventually empty but initialized, i.e. never null !!) list of objects</param>
        public void GetObjectsInRadius<T>(eGameObjectType objectType, int x, int y, int z, ushort radius, HashSet<T> partialList, bool ignoreZ) where T : GameObject
        {
            if (!_initialized)
                InitializeZone();

            uint sqRadius = (uint)radius * radius;
            int referenceSubzoneIndex = GetSubZoneIndex(x, y);

            int xInZone = x - XOffset; // x in zone coordinates.
            int yInZone = y - YOffset; // y in zone coordinates.

            int cellNbr = (radius >> SUBZONE_SHIFT) + 1; // Radius in terms of subzone number.
            int xInCell = xInZone >> SUBZONE_SHIFT; // xInZone in terms of subzone coord.
            int yInCell = yInZone >> SUBZONE_SHIFT; // yInZone in terms of subzone coord.

            int minColumn = xInCell - cellNbr;
            if (minColumn < 0)
                minColumn = 0;

            int maxColumn = xInCell + cellNbr;
            if (maxColumn > (SUBZONE_NBR_ON_ZONE_SIDE - 1))
                maxColumn = SUBZONE_NBR_ON_ZONE_SIDE - 1;

            int minLine = yInCell - cellNbr;
            if (minLine < 0)
                minLine = 0;

            int maxLine = yInCell + cellNbr;
            if (maxLine > (SUBZONE_NBR_ON_ZONE_SIDE - 1))
                maxLine = SUBZONE_NBR_ON_ZONE_SIDE - 1;

            int currentSubZoneIndex;
            LightConcurrentLinkedList<SubZoneObject> objects;

            for (int currentLine = minLine; currentLine <= maxLine; ++currentLine)
            {
                for (int currentColumn = minColumn; currentColumn <= maxColumn; ++currentColumn)
                {
                    currentSubZoneIndex = GetSubZoneOffset(currentLine, currentColumn);
                    SubZone subZone = _subZones[currentSubZoneIndex];
                    objects = subZone.GetObjects(objectType);

                    if (objects.Count == 0)
                        continue;

                    // The current subzone contains some objects.
                    if (currentSubZoneIndex == referenceSubzoneIndex)
                        // We are in the subzone of the observation point. Check all distances for all objects in the subzone.
                        AddToListWithDistanceCheck(objects, x, y, z, sqRadius, objectType, currentSubZoneIndex, partialList, ignoreZ);
                    else
                    {
                        int xLeft = currentColumn << SUBZONE_SHIFT;
                        int xRight = xLeft + SUBZONE_SIZE;
                        int yTop = currentLine << SUBZONE_SHIFT;
                        int yBottom = yTop + SUBZONE_SIZE;

                        if (CheckMinDistance(xInZone, yInZone, xLeft, xRight, yTop, yBottom, sqRadius))
                        {
                            if (CheckMaxDistance(xInZone, yInZone, xLeft, xRight, yTop, yBottom, sqRadius))
                                // The current subzone is fully enclosed within the radius. Add all the objects of the current subzone.
                                AddToListWithoutDistanceCheck(objects, objectType, currentSubZoneIndex, partialList);
                            else
                                // The current subzone is partially enclosed within the radius. Only add the objects within the right area.
                                AddToListWithDistanceCheck(objects, x, y, z, sqRadius, objectType, currentSubZoneIndex, partialList, ignoreZ);
                        }
                    }
                }
            }
        }

        private void AddToListWithoutDistanceCheck<T>(LightConcurrentLinkedList<SubZoneObject> subZoneObjects, eGameObjectType objectType, int subZoneIndex, HashSet<T> partialList) where T : GameObject
        {
            using LightConcurrentLinkedList<SubZoneObject>.Reader reader = subZoneObjects.GetReader();

            for (LightConcurrentLinkedList<SubZoneObject>.Node node = reader.Current(); node != null; node = reader.Next())
            {
                if (!ShouldObjectChangeSubZone(node, objectType, subZoneIndex))
                    partialList.Add((T)node.Item.Object);
            }
        }

        private void AddToListWithDistanceCheck<T>(LightConcurrentLinkedList<SubZoneObject> subZoneObjects, int x, int y, int z, uint sqRadius, eGameObjectType objectType, int subZoneIndex, HashSet<T> partialList, bool ignoreZ) where T : GameObject
        {
            using LightConcurrentLinkedList<SubZoneObject>.Reader reader = subZoneObjects.GetReader();

            // Check all distances for all objects in the subzone.
            for (LightConcurrentLinkedList<SubZoneObject>.Node node = reader.Current(); node != null; node = reader.Next())
            {
                if (!ShouldObjectChangeSubZone(node, objectType, subZoneIndex))
                {
                    SubZoneObject subZoneObject = node.Item;
                    GameObject gameObject = subZoneObject.Object;

                    if (CheckSquareDistance(x, y, z, gameObject.X, gameObject.Y, gameObject.Z, sqRadius, ignoreZ))
                        partialList.Add((T)gameObject);
                }
            }
        }

        #region Relocation

        private bool ShouldObjectChangeSubZone(LightConcurrentLinkedList<SubZoneObject>.Node node, eGameObjectType objectType, int subZoneIndex)
        {
            SubZoneObject subZoneObject = node.Item;
            GameObject gameObject = subZoneObject.Object;

            // Does the current object exists, is active and still in the region where this zone is located?
            if (gameObject != null && gameObject.ObjectState == GameObject.eObjectState.Active && gameObject.CurrentRegion == ZoneRegion)
            {
                int objectSubZoneIndex = GetSubZoneIndex(gameObject.X, gameObject.Y);

                // Has the object moved to another zone in the same region, or to another subzone in the same zone?
                if (objectSubZoneIndex == -1)
                {
                    if (!subZoneObject.IsChangingSubZone)
                    {
                        Zone newZone = ZoneRegion.GetZone(gameObject.X, gameObject.Y);
                        SubZone newSubZone = newZone.GetSubZone(newZone.GetSubZoneIndex(gameObject.X, gameObject.Y));
                        EntityManager.Add(EntityManager.EntityType.ObjectChangingSubZone, new ObjectChangingSubZone(node, objectType, newZone, newSubZone));
                        return true;
                    }
                }
                else if (objectSubZoneIndex != subZoneIndex)
                {
                    if (!subZoneObject.IsChangingSubZone)
                    {
                        EntityManager.Add(EntityManager.EntityType.ObjectChangingSubZone, new ObjectChangingSubZone(node, objectType, this, _subZones[objectSubZoneIndex]));
                        return true;
                    }
                }
            }
            else if (!subZoneObject.IsChangingSubZone)
                EntityManager.Add(EntityManager.EntityType.ObjectChangingSubZone, new ObjectChangingSubZone(node, objectType, null, null)); // Ghost object.

            return false;
        }

        public void OnObjectAddedToZone()
        {
            Interlocked.Increment(ref _objectCount);
        }

        public void OnObjectRemovedFromZone()
        {
            Interlocked.Decrement(ref _objectCount);
        }

        #endregion

        /// <summary>
        /// Checks that the square distance between two arbitary points in space is lower or equal to the given square distance.
        /// </summary>
        /// <param name="x1">X of Point1</param>
        /// <param name="y1">Y of Point1</param>
        /// <param name="z1">Z of Point1</param>
        /// <param name="x2">X of Point2</param>
        /// <param name="y2">Y of Point2</param>
        /// <param name="z2">Z of Point2</param>
        /// <param name="sqDistance">the square distance to check for</param>
        /// <returns>The distance</returns>
        public static bool CheckSquareDistance(int x1, int y1, int z1, int x2, int y2, int z2, uint sqDistance, bool ignoreZ)
        {
            int xDiff = x1 - x2;
            long dist = (long)xDiff * xDiff;

            if (dist > sqDistance)
                return false;

            int yDiff = y1 - y2;
            dist += (long)yDiff * yDiff;

            if (dist > sqDistance)
                return false;

            if (ignoreZ == false)
            {
                int zDiff = z1 - z2;
                dist += (long)zDiff * zDiff;
            }

            return dist <= sqDistance;
        }

        /// <summary>
        /// Checks that the minimal distance between a point and a subzone (defined by the four position of the sides) is lower or equal to the distance (given as a square distance).
        /// PRECONDITION: The point is not in the tested subzone.
        /// </summary>
        /// <param name="x">X position of the point</param>
        /// <param name="y">Y position of the square</param>
        /// <param name="xLeft">X value of the left side of the square</param>
        /// <param name="xRight">X value of the right side of the square</param>
        /// <param name="yTop">Y value of the top side of the square</param>
        /// <param name="yBottom">Y value of the bottom side of the square</param>
        /// <param name="squareRadius">the square of the radius to check for</param>
        /// <returns>The distance</returns>
        private static bool CheckMinDistance(int x, int y, int xLeft, int xRight, int yTop, int yBottom, uint squareRadius)
        {
            long distance;

            if ((y >= yTop) && (y <= yBottom))
            {
                int xdiff = Math.Min(FastMath.Abs(x - xLeft), FastMath.Abs(x - xRight));
                distance = (long)xdiff * xdiff;
            }
            else
            {
                if ((x >= xLeft) && (x <= xRight))
                {
                    int ydiff = Math.Min(FastMath.Abs(y - yTop), FastMath.Abs(y - yBottom));
                    distance = (long)ydiff * ydiff;
                }
                else
                {
                    int xdiff = Math.Min(FastMath.Abs(x - xLeft), FastMath.Abs(x - xRight));
                    int ydiff = Math.Min(FastMath.Abs(y - yTop), FastMath.Abs(y - yBottom));
                    distance = (long)xdiff * xdiff + (long)ydiff * ydiff;
                }
            }

            return distance <= squareRadius;
        }

        /// <summary>
        /// Checks that the maximal distance between a point and a subzone (defined by the four position of the sides) is lower or equal to the distance (given as a square distance).
        /// </summary>
        /// <param name="x">X position of the point</param>
        /// <param name="y">Y position of the square</param>
        /// <param name="xLeft">X value of the left side of the square</param>
        /// <param name="xRight">X value of the right side of the square</param>
        /// <param name="yTop">Y value of the top side of the square</param>
        /// <param name="yBottom">Y value of the bottom side of the square</param>
        /// <param name="squareRadius">the square of the radius to check for</param>
        /// <returns>The distance</returns>
        private static bool CheckMaxDistance(int x, int y, int xLeft, int xRight, int yTop, int yBottom, uint squareRadius)
        {
            int xdiff = Math.Max(FastMath.Abs(x - xLeft), FastMath.Abs(x - xRight));
            int ydiff = Math.Max(FastMath.Abs(y - yTop), FastMath.Abs(y - yBottom));
            long distance = (long) xdiff * xdiff + (long) ydiff * ydiff;
            return distance <= squareRadius;
        }

        #endregion

        #region Area

        /// <summary>
        /// Convenient method for Region.GetAreasOfZone(), since zone.Region.getAreasOfZone(zone,x,y,z) is a bit confusing.
        /// </summary>
        public IList<IArea> GetAreasOfSpot(IPoint3D spot)
        {
            return GetAreasOfSpot(spot, true);
        }

        public IList<IArea> GetAreasOfSpot(int x, int y, int z)
        {
            return ZoneRegion.GetAreasOfZone(this, x, y, z);
        }

        public IList<IArea> GetAreasOfSpot(IPoint3D spot, bool checkZ)
        {
            return ZoneRegion.GetAreasOfZone(this, spot, checkZ);
        }

        #endregion

        #region Get random NPC

        /// <summary>
        /// Get a random NPC belonging to a realm.
        /// </summary>
        public GameNPC GetRandomNPC(eRealm realm)
        {
            return GetRandomNPC(new eRealm[] { realm }, 0, 0);
        }

        /// <summary>
        /// Get a random NPC belonging to a realm between levels minlevel and maxlevel.
        /// </summary>
        public GameNPC GetRandomNPC(eRealm realm, int minLevel, int maxLevel)
        {
            return GetRandomNPC(new eRealm[] { realm }, minLevel, maxLevel);
        }

        /// <summary>
        /// Get a random npc from zone with given realms.
        /// </summary>
        public GameNPC GetRandomNPC(eRealm[] realms)
        {
            return GetRandomNPC(realms, 0, 0);
        }

        /// <summary>
        /// Get a random npc from zone with given realms.
        /// </summary>
        public GameNPC GetRandomNPC(eRealm[] realms, int minLevel, int maxLevel)
        {
            List<GameNPC> npcs = GetNPCsOfZone(realms, minLevel, maxLevel, 0, 0, true);
            GameNPC randomNPC = npcs.Count == 0 ? null : npcs[Util.Random(npcs.Count - 1)];
            return randomNPC;
        }

        /// <summary>
        /// Gets all NPC's in zone.
        /// </summary>
        public List<GameNPC> GetNPCsOfZone(eRealm realm)
        {
            return GetNPCsOfZone(new eRealm[] { realm }, 0, 0, 0, 0, false);
        }

        /// <summary>
        /// Get NPCs of a zone given various parameters.
        /// </summary>
        public List<GameNPC> GetNPCsOfZone(eRealm[] realms, int minLevel, int maxLevel, int compareLevel, int conLevel, bool firstOnly)
        {
            if (!_initialized)
                InitializeZone();

            List<GameNPC> list = new();
            GameNPC currentNPC;
            bool addToList;

            try
            {
                foreach (SubZone subZone in _subZones)
                {
                    using LightConcurrentLinkedList<SubZoneObject>.Reader reader = subZone.GetObjects(eGameObjectType.NPC).GetReader();

                    for (LightConcurrentLinkedList<SubZoneObject>.Node node = reader.Current(); node != null; node = reader.Next())
                    {
                        currentNPC = (GameNPC)node.Item.Object;

                        for (int i = 0; i < realms.Length; i++)
                        {
                            if (currentNPC.Realm == realms[i])
                            {
                                addToList = true;

                                if (compareLevel > 0 && conLevel > 0)
                                    addToList = (int)GameObject.GetConLevel(compareLevel, currentNPC.Level) == conLevel;
                                else
                                {
                                    if (minLevel > 0 && currentNPC.Level < minLevel)
                                        addToList = false;
                                    if (maxLevel > 0 && currentNPC.Level > maxLevel)
                                        addToList = false;
                                }

                                if (addToList)
                                {
                                    list.Add(currentNPC);

                                    if (firstOnly)
                                        return list;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("GetNPCsOfZone: Caught Exception for zone " + Description + ".", ex);
            }

            return list;
        }

        #endregion

        public enum eGameObjectType : byte
        {
            ITEM = 0,
            NPC = 1,
            PLAYER = 2,
            DOOR = 3,
        }
    }
}
