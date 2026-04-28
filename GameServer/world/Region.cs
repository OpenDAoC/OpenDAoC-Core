using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;
using DOL.Events;
using DOL.GS.Keeps;
using DOL.GS.ServerProperties;
using DOL.Logging;

namespace DOL.GS
{
    /// <summary>
    /// This class represents a region in DAOC. A region is everything where you
    /// need a loading screen to go there. Eg. whole Albion is one Region, Midgard and
    /// Hibernia are just one region too. Darkness Falls is a region. Each dungeon, city
    /// is a region ... you get the clue. Each Region can hold an arbitrary number of
    /// Zones! Camelot Hills is one Zone, Tir na Nog is one Zone (and one Region)...
    /// </summary>
    public class Region
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        #region Region Variables

        /// <summary>
        /// This is the minimum size for object array that is allocated when
        /// the first object is added to the region must be dividable by 32 (optimization)
        /// </summary>
        public static readonly int MINIMUMSIZE = 256;

        /// <summary>
        /// This holds all objects inside this region. Their index = their id!
        /// </summary>
        protected GameObject[] _objects;

        /// <summary>
        /// Object to lock when changing objects in the array
        /// </summary>
        public readonly Lock ObjectsSyncLock = new();

        /// <summary>
        /// This holds a counter with the absolute count of all objects that are actually in this region
        /// </summary>
        protected int _objectsInRegion;

        /// <summary>
        /// Total number of objects in this region
        /// </summary>
        public int TotalNumberOfObjects => _objectsInRegion;

        /// <summary>
        /// This array holds a bit array
        /// Its used to know which slots in region object array are free and what allocated
        /// This is used to accelerate inserts a lot
        /// </summary>
        protected uint[] _objectsAllocatedSlots;

        /// <summary>
        /// This holds the index of a possible next object slot
        /// but needs further checks (basically its last added object index + 1)
        /// </summary>
        protected int _nextObjectSlot;

        /// <summary>
        /// Holds all the Zones inside this Region
        /// </summary>
        protected readonly List<Zone> _zones;

        protected Dictionary<ushort, IArea> Areas { get; }
        protected readonly Dictionary<Zone, List<IArea>> _zoneAreas;
        protected readonly ReaderWriterLockSlim _areasLock = new(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// How often shall we remove unused objects
        /// </summary>
        protected static readonly int CLEANUPTIMER = 60000;

        /// <summary>
        /// Contains the # of players in the region
        /// </summary>
        protected int _numPlayer;

        #endregion

        #region Constructor

        public RegionData RegionData { get; protected set; }

        /// <summary>
        /// Factory method to create regions.  Will create a region of data.ClassType, or default to Region if 
        /// an error occurs or ClassType is not specified
        /// </summary>
        /// <param name="time"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Region Create(RegionData data)
        {
            try
            {
                Type t = typeof(Region);

                if (string.IsNullOrEmpty(data.ClassType) == false)
                {
                    t = Type.GetType(data.ClassType) ?? ScriptMgr.GetType(data.ClassType);

                    if (t != null)
                    {
                        ConstructorInfo info = t.GetConstructor([typeof(RegionData)]);

                        Region r = (Region)info.Invoke([data]);

                        if (r != null)
                        {
                            // Success with requested class type
                            if (log.IsInfoEnabled)
                                log.InfoFormat("Created Region {0} using ClassType '{1}'", r.ID, data.ClassType);

                            return r;
                        }

                        if (log.IsErrorEnabled)
                            log.ErrorFormat("Failed to Invoke Region {0} using ClassType '{1}'", r.ID, data.ClassType);
                    }
                    else if (log.IsErrorEnabled)
                        log.ErrorFormat("Failed to find ClassType '{0}' for region {1}!", data.ClassType, data.Id);
                }
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Failed to start region {0} with requested class type: {1}.  Exception: {2}!", data.Id, data.ClassType, ex.Message);
            }

            // Create region using default type
            return new Region(data);
        }

        /// <summary>
        /// Constructs a new empty Region
        /// </summary>
        /// <param name="time">The time manager for this region</param>
        /// <param name="data">The region data</param>
        public Region(RegionData data)
        {
            RegionData = data;
            _objects = [];
            _objectsInRegion = 0;
            _nextObjectSlot = 0;
            _objectsAllocatedSlots = [];
            _zones = new();
            _zoneAreas = new();
            Areas = new();

            List<string> list = null;

            if (Properties.DEBUG_LOAD_REGIONS != string.Empty)
                list = Util.SplitCSV(Properties.DEBUG_LOAD_REGIONS, true);

            if (list != null && list.Count > 0)
            {
                _loadObjects = false;

                foreach (string region in list)
                {
                    if (region.ToString() == ID.ToString())
                    {
                        _loadObjects = true;
                        break;
                    }
                }
            }

            list = Util.SplitCSV(Properties.DISABLED_REGIONS, true);
            foreach (string region in list)
            {
                if (region.ToString() == ID.ToString())
                {
                    _isDisabled = true;
                    break;
                }
            }

            list = Util.SplitCSV(Properties.DISABLED_EXPANSIONS, true);
            foreach (string expansion in list)
            {
                if (expansion.ToString() == RegionData.Expansion.ToString())
                {
                    _isDisabled = true;
                    break;
                }
            }
        }

        /// <summary>
        /// What to do when the region collapses.
        /// This is called when instanced regions need to be closed
        /// </summary>
        public virtual void OnCollapse()
        {
            //Delete objects
            foreach (GameObject obj in _objects)
            {
                if (obj != null)
                {
                    obj.Delete();
                    RemoveObject(obj);
                    obj.CurrentRegion = null;
                }
            }

            _objects = null;

            foreach (Zone z in _zones)
            {
                z.Delete();
            }

            _zones.Clear();
            GameEventMgr.RemoveAllHandlersForObject(this);
        }

        #endregion

        /// <summary>
        /// Handles players leaving this region via a zone point
        /// </summary>
        /// <param name="player"></param>
        /// <param name="zonePoint"></param>
        /// <returns>false to halt processing of this request</returns>
        public virtual bool OnZonePoint(GamePlayer player, DbZonePoint zonePoint)
        {
            return true;
        }

        #region Properties

        public virtual bool IsRvR
        {
            get
            {
                switch (RegionData.Id)
                {
                    case 163: //new frontiers
                    case 165: //Cathal Valley
                    case 233: //Summoners Hall
                    case 234: //1to4BG
                    case 235: //5to9BG
                    case 236: //10to14BG
                    case 237: //15to19BG
                    case 238: //20to24BG
                    case 239: //25to29BG
                    case 240: //30to34BG
                    case 241: //35to39BG
                    case 242: //40to44BG and Test BG
                    case 244: //Frontiers RvR dungeon
                    case 249: //Darkness Falls - RvR dungeon
                    case 489: //lvl5-9 Demons breach
                        return true;
                    default:
                        return false;
                }
            }
        }

        public virtual bool IsFrontier
        { get => RegionData.IsFrontier; set => RegionData.IsFrontier = value;
        }

        /// <summary>
        /// Is the Region a temporary instance
        /// </summary>
        public virtual bool IsInstance => false;

        /// <summary>
        /// Is this region a standard DAoC region or a custom server region
        /// </summary>
        public virtual bool IsCustom => false;

        /// <summary>
        /// Gets whether this region is a dungeon or not
        /// </summary>
        public virtual bool IsDungeon
        {
            get
            {
                const int dungeonOffset = 8192;
                const int zoneCount = 1;

                if (Zones.Count != zoneCount)
                    return false; //Dungeons only have 1 zone!

                Zone zone = Zones[0];

                if (zone.XOffset == dungeonOffset && zone.YOffset == dungeonOffset)
                    return true; //Only dungeons got this offset

                return false;
            }
        }

        /// <summary>
        /// Gets the # of players in the region
        /// </summary>
        public virtual int NumPlayers => _numPlayer;

        /// <summary>
        /// The Region Name eg. Region000
        /// </summary>
        public virtual string Name => RegionData.Name;

        /// <summary>
        /// The Regi on Description eg. Cursed Forest
        /// </summary>
        public virtual string Description => RegionData.Description;

        /// <summary>
        /// The ID of the Region eg. 21
        /// </summary>
        public virtual ushort ID => RegionData.Id;

        /// <summary>
        /// The Region Server IP ... for future use
        /// </summary>
        public string ServerIP => RegionData.Ip;

        /// <summary>
        /// The Region Server Port ... for future use
        /// </summary>
        public ushort ServerPort => RegionData.Port;

        /// <summary>
        /// An ArrayList of all Zones within this Region
        /// </summary>
        public List<Zone> Zones => _zones;

        /// <summary>
        /// Returns the object array of this region
        /// </summary>
        public GameObject[] Objects => _objects;

        /// <summary>
        /// Gets or Sets the region expansion (we use client expansion + 1)
        /// </summary>
        public virtual int Expansion => RegionData.Expansion + 1;

        /// <summary>
        /// Gets or Sets the water level in this region
        /// </summary>
        public virtual int WaterLevel => RegionData.WaterLevel;

        /// <summary>
        /// Gets or Sets diving flag for region
        /// Note: This flag should normally be checked at the zone level
        /// </summary>
        public virtual bool IsRegionDivingEnabled => RegionData.DivingEnabled;

        /// <summary>
        /// Does this region contain housing?
        /// </summary>
        public virtual bool HousingEnabled => RegionData.HousingEnabled;

        /// <summary>
        /// Should this region use the housing manager?
        /// Standard regions always use the housing manager if housing is enabled, custom regions might not.
        /// </summary>
        public virtual bool UseHousingManager => HousingEnabled;

        /// <summary>
        /// Gets the current region time in milliseconds
        /// </summary>
        public virtual long Time => GameLoop.GameLoopTime;

        protected bool _isDisabled = false;
        /// <summary>
        /// Is this region disabled
        /// </summary>
        public virtual bool IsDisabled => _isDisabled;

        protected bool _loadObjects = true;
        /// <summary>
        /// Will this region load objects
        /// </summary>
        public virtual bool LoadObjects => _loadObjects;

        /// <summary>
        /// Added to allow instances; the 'appearance' of the region, the map the GameClient uses.
        /// </summary>
        public virtual ushort Skin => ID;

        /// <summary>
        /// Should this region respond to time manager send requests
        /// Normally yes, might be disabled for some instances.
        /// </summary>
        public virtual bool UseTimeManager
        {
            get => true;
            set { }
        }

        /// <summary>
        /// Each region can return it's own game time
        /// By default let WorldMgr handle it
        /// </summary>
        public virtual uint GameTime
        {
            get => WorldMgr.GetCurrentGameTime();
            set { }
        }

        /// <summary>
        /// Get the day increment for this region.
        /// By default let WorldMgr handle it
        /// </summary>
        public virtual uint DayIncrement
        {
            get => WorldMgr.GetDayIncrement();
            set { }
        }

        /// <summary>
        /// Create a keep for this region
        /// </summary>
        /// <returns></returns>
        public virtual AbstractGameKeep CreateGameKeep()
        {
            return new GameKeep();
        }
        
        /// <summary>
        /// Create a new Relic keep for this region
        /// </summary>
        /// <returns></returns>
        public virtual AbstractGameKeep CreateRelicGameKeep()
        {
            return new RelicGameKeep();
        }

        /// <summary>
        /// Create the appropriate GameKeepTower for this region
        /// </summary>
        /// <returns></returns>
        public virtual AbstractGameKeep CreateGameKeepTower()
        {
            return new GameKeepTower();
        }

        /// <summary>
        /// Create the appropriate GameKeepComponent for this region
        /// </summary>
        /// <returns></returns>
        public virtual GameKeepComponent CreateGameKeepComponent()
        {
            return new GameKeepComponent();
        }

        /// <summary>
        /// Determine if the current time is AM.
        /// </summary>
        public virtual bool IsAM => !_isPM;

        private bool _isPM;
        /// <summary>
        /// Determine if the current time is PM.
        /// </summary>
        public virtual bool IsPM
        {
            get
            {
                uint cTime = GameTime;

                uint hour = cTime / 1000 / 60 / 60;
                bool pm = hour is >= 12 and <= 23;

                _isPM = pm;

                return _isPM;
            }

            set => _isPM = value;
        }

        private bool _isNightTime;
        /// <summary>
        /// Determine if current time is between 6PM and 6AM, can be used for conditional spells.
        /// </summary>
        public virtual bool IsNightTime
        {
            get
            {
                uint cTime = GameTime;

                uint hour = cTime / 1000 / 60 / 60;
                bool night = hour is >= 18 or < 6;

                _isNightTime = night;

                return _isNightTime;
            }

            set => _isNightTime = value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the RegionMgr
        /// </summary>
        public void StartRegionMgr()
        {
            Notify(RegionEvent.RegionStart, this);
        }

        /// <summary>
        /// Stops the RegionMgr
        /// </summary>
        public void StopRegionMgr()
        {
            Notify(RegionEvent.RegionStop, this);
        }

        /// <summary>
        /// Reallocates objects array with given size
        /// </summary>
        /// <param name="count">The size of new objects array, limited by MAXOBJECTS</param>
        public virtual void PreAllocateRegionSpace(int count)
        {
            if (count > Properties.REGION_MAX_OBJECTS)
                count = Properties.REGION_MAX_OBJECTS;
            lock (ObjectsSyncLock)
            {
                if (_objects.Length > count)
                    return;
                GameObject[] newObj = new GameObject[count];
                Array.Copy(_objects, newObj, _objects.Length);
                if (count / 32 + 1 > _objectsAllocatedSlots.Length)
                {
                    uint[] slotArray = new uint[count / 32 + 1];
                    Array.Copy(_objectsAllocatedSlots, slotArray, _objectsAllocatedSlots.Length);
                    _objectsAllocatedSlots = slotArray;
                }

                _objects = newObj;
            }
        }

        /// <summary>
        /// Loads the region from database
        /// </summary>
        public virtual void LoadFromDatabase(DbMob[] mobs, ref long mobCount, ref long merchantCount, ref long itemCount, ref long bindCount)
        {
            if (!LoadObjects)
                return;

            Assembly assembly = Assembly.GetAssembly(typeof(GameServer));
            IList<DbWorldObject> staticObjects = DOLDB<DbWorldObject>.SelectObjects(DB.Column("Region").IsEqualTo(ID));
            IList<DbGravestone> gravestones = DOLDB<DbGravestone>.SelectObjects(DB.Column("Region").IsEqualTo(ID));
            IList<DbBindPoint> bindPoints = DOLDB<DbBindPoint>.SelectObjects(DB.Column("Region").IsEqualTo(ID));
            int count = mobs.Length + staticObjects.Count + gravestones.Count;
            if (count > 0)
                PreAllocateRegionSpace(count + 100);
            int myItemCount = staticObjects.Count;
            int myGravestoneCount = gravestones.Count;
            int myMobCount = 0;
            int myMerchantCount = 0;
            int myBindCount = bindPoints.Count;
            string allErrors = string.Empty;

            if (mobs.Length > 0)
            {
                Parallel.ForEach(mobs, (mob) =>
                {
                    GameNPC myMob = null;
                    string error = string.Empty;

                    // Default class type
                    string classType = Properties.GAMENPC_DEFAULT_CLASSTYPE;

                    // load template if any
                    NpcTemplate template = null;
                    if(mob.NPCTemplateID != -1)
                    {
                       template = NpcTemplateMgr.GetTemplate(mob.NPCTemplateID);
                    }
                    

                    if (Properties.USE_NPCGUILDSCRIPTS && mob.Guild.Length > 0 && mob.Realm >= 0 && mob.Realm <= (int)eRealm._Last)
                    {
                        Type type = ScriptMgr.FindNPCGuildScriptClass(mob.Guild, (eRealm)mob.Realm);
                        if (type != null)
                        {
                            try
                            {
                                myMob = (GameNPC)type.Assembly.CreateInstance(type.FullName);
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                    log.Error("LoadFromDatabase", e);
                            }
                        }
                    }

                    if (myMob == null)
                    {
                        if(template != null && template.ClassType != null && template.ClassType.Length > 0 && template.ClassType != DbMob.DEFAULT_NPC_CLASSTYPE && template.ReplaceMobValues)
                        {
                            classType = template.ClassType;
                        }
                        else if (mob.ClassType != null && mob.ClassType.Length > 0 && mob.ClassType != DbMob.DEFAULT_NPC_CLASSTYPE)
                        {
                            classType = mob.ClassType;
                        }

                        try
                        {
                            myMob = (GameNPC)assembly.CreateInstance(classType, false);
                        }
                        catch
                        {
                            error = classType;
                        }

                        if (myMob == null)
                        {
                            foreach (Assembly asm in ScriptMgr.Scripts)
                            {
                                try
                                {
                                    myMob = (GameNPC)asm.CreateInstance(classType, false);
                                    error = string.Empty;
                                }
                                catch
                                {
                                    error = classType;
                                }

                                if (myMob != null)
                                    break;
                            }

                            if (myMob == null)
                            {
                                myMob = new GameNPC();
                                error = classType;
                            }
                        }
                    }

                    if (!allErrors.Contains(error))
                        allErrors += " " + error + ",";

                    if (myMob != null)
                    {
                        try
                        {
                            myMob.LoadFromDatabase(mob);

                            if (myMob is GameMerchant)
                            {
                                Interlocked.Increment(ref myMerchantCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref myMobCount);
                            }
                        }
                        catch (Exception e)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Failed: " + myMob.GetType().FullName + ":LoadFromDatabase(" + mob.GetType().FullName + ");", e);
                            throw;
                        }

                        myMob.AddToWorld();
                    }
                });
            }

            if (staticObjects.Count > 0)
            {
                Parallel.ForEach(staticObjects, (item) =>
                {
                    GameStaticItem myItem;
                    if (!string.IsNullOrEmpty(item.ClassType))
                    {
                        myItem = assembly.CreateInstance(item.ClassType, false) as GameStaticItem;
                        if (myItem == null)
                        {
                            foreach (Assembly asm in ScriptMgr.Scripts)
                            {
                                try
                                {
                                    myItem = (GameStaticItem)asm.CreateInstance(item.ClassType, false);
                                }
                                catch { }

                                if (myItem != null)
                                    break;
                            }

                            myItem ??= new GameStaticItem();
                        }
                    }
                    else
                        myItem = new GameStaticItem();

                    myItem.LoadFromDatabase(item);
                    myItem.AddToWorld();
                });
            }

            if (gravestones.Count > 0)
            {
                Parallel.ForEach(gravestones, (stone) =>
                {
                    GameGravestone myStone = new();
                    myStone.LoadFromDatabase(stone);
                    myStone.AddToWorld();
                });
            }

            foreach (DbBindPoint bindPoint in bindPoints)
                AddArea(new Area.BindArea("bind point", bindPoint));

            if (myMobCount + myItemCount + myGravestoneCount + myMerchantCount + myBindCount > 0)
            {
                if (log.IsInfoEnabled)
                    log.Info($"Region: {Description} ({ID}) loaded {myMobCount} mobs, {myMerchantCount} merchants, {myItemCount + myGravestoneCount} items, {myBindCount} bind points");

                if (log.IsDebugEnabled)
                    log.Debug("Used Memory: " + GC.GetTotalMemory(false) / 1024 / 1024 + "MB");

                if (allErrors != string.Empty && log.IsErrorEnabled)
                    log.Error("Error loading the following NPC ClassType(s), GameNPC used instead:" + allErrors.TrimEnd(','));
            }

            Interlocked.Add(ref mobCount, myMobCount);
            Interlocked.Add(ref merchantCount, myMerchantCount);
            Interlocked.Add(ref itemCount, myItemCount + myGravestoneCount);
            Interlocked.Add(ref bindCount, myBindCount);
        }

        /// <summary>
        /// Adds an object to the region and assigns the object an id
        /// </summary>
        /// <param name="obj">A GameObject to be added to the region</param>
        /// <returns>success</returns>
        internal bool AddObject(GameObject obj)
        {
            //Assign a new id
            lock (ObjectsSyncLock)
            {
                if (obj.ObjectID != 0)
                {
                    if (obj.ObjectID < _objects.Length && obj == _objects[obj.ObjectID - 1])
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"Object is already in \"{Description}\". ({obj})");

                        return false;
                    }

                    if (log.IsWarnEnabled)
                        log.Warn($"Object already has an OID. ({obj})");

                    return false;
                }

                GameObject[] objectsRef = _objects;

                //*** optimized object management for memory saving primary but keeping it very fast - Blue ***

                // find first free slot for the object
                int objID = _nextObjectSlot;
                if (objID >= _objects.Length || _objects[objID] != null)
                {

                    // we are at array end, are there any holes left?
                    if (_objects.Length > _objectsInRegion)
                    {
                        // yes there are some places left in current object array, try to find them
                        // by using the bit array (can check 32 slots at once!)

                        int i = _objects.Length / 32;
                        // INVARIANT: i * 32 is always lower or equal to _objects.Length (integer division property)
                        if (i * 32 == _objects.Length)
                        {
                            i -= 1;
                        }

                        bool found = false;
                        objID = -1;

                        while (!found && (i >= 0))
                        {
                            if (_objectsAllocatedSlots[i] != 0xffffffff)
                            {
                                // we found a free slot
                                // => search for exact place

                                int currentIndex = i * 32;
                                int upperBound = (i + 1) * 32;
                                while (!found && (currentIndex < _objects.Length) && (currentIndex < upperBound))
                                {
                                    if (_objects[currentIndex] == null)
                                    {
                                        found = true;
                                        objID = currentIndex;
                                    }

                                    currentIndex++;
                                }

                                // INVARIANT: at this point, found must be true (otherwise the underlying data structure is corrupt)
                            }

                            i--;
                        }
                    }
                    else
                    { // our array is full, we must resize now to fit new objects

                        if (objectsRef.Length == 0)
                        {

                            // there is no array yet, so set it to a minimum at least
                            objectsRef = new GameObject[MINIMUMSIZE];
                            Array.Copy(_objects, objectsRef, _objects.Length);
                            objID = 0;

                        }
                        else if (objectsRef.Length >= Properties.REGION_MAX_OBJECTS)
                        {

                            // no available slot
                            if (log.IsErrorEnabled)
                                log.Error($"Can't add new object to \"{Description}\" because it is full. ({obj})");

                            return false;
                        }
                        else
                        {

                            // we need to add a certain amount to grow
                            int size = (int)(_objects.Length * 1.20);
                            if (size < _objects.Length + 256)
                                size = _objects.Length + 256;
                            if (size > Properties.REGION_MAX_OBJECTS)
                                size = Properties.REGION_MAX_OBJECTS;
                            objectsRef = new GameObject[size]; // grow the array by 20%, at least 256
                            Array.Copy(_objects, objectsRef, _objects.Length);
                            objID = _objects.Length; // new object adds right behind the last object in old array

                        }
                        // resize the bit array as well
                        int diff = objectsRef.Length / 32 - _objectsAllocatedSlots.Length;
                        if (diff >= 0)
                        {
                            uint[] newBitArray = new uint[Math.Max(_objectsAllocatedSlots.Length + diff + 50, 100)];	// add at least 100 integers, makes it resize less often, serves 3200 new objects, only 400 bytes
                            Array.Copy(_objectsAllocatedSlots, newBitArray, _objectsAllocatedSlots.Length);
                            _objectsAllocatedSlots = newBitArray;
                        }
                    }
                }

                if (objID < 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"There was an unexpected problem while adding new object to \"{Description}\". ({obj})");

                    return false;
                }

                // if we found a slot add the object
                GameObject oidObj = objectsRef[objID];
                if (oidObj == null)
                {
                    objectsRef[objID] = obj;
                    _nextObjectSlot = objID + 1;
                    _objectsInRegion++;
                    obj.ObjectID = (ushort) (objID + 1); // Safe.
                    _objectsAllocatedSlots[objID / 32] |= (uint)1 << (objID % 32);
                    Thread.MemoryBarrier();
                    _objects = objectsRef;

                    if (obj is GamePlayer)
                        ++_numPlayer;
                }
                else
                {
                    // no available slot
                    if (log.IsErrorEnabled)
                        log.Error($"Can't add new object to \"{Description}\" because  OID is already used by {oidObj}. ({obj})");

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes the object with the specified ID from the region
        /// </summary>
        /// <param name="obj">A GameObject to be removed from the region</param>
        internal void RemoveObject(GameObject obj)
        {
            lock (ObjectsSyncLock)
            {
                int index = obj.ObjectID - 1;
                if (index < 0)
                {
                    return;
                }

                if (obj is GamePlayer)
                    --_numPlayer;

                GameObject inPlace = _objects[obj.ObjectID - 1];
                if (inPlace == null)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("RemoveObject conflict! OID" + obj.ObjectID + " " + obj.Name + "(" + obj.CurrentRegionID + ") but there was no object at that slot");
                        log.Error(new StackTrace().ToString());
                    }

                    return;
                }

                if (obj != inPlace)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("RemoveObject conflict! OID" + obj.ObjectID + " " + obj.Name + "(" + obj.CurrentRegionID + ") but there was another object already " + inPlace.Name + " region:" + inPlace.CurrentRegionID + " state:" + inPlace.ObjectState);
                        log.Error(new StackTrace().ToString());
                    }

                    return;
                }

                if (_objects[index] != obj)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Object OID is already used by another object! (used by:" + _objects[index].ToString() + ")");
                }
                else
                {
                    _objects[index] = null;
                    _nextObjectSlot = index;
                    _objectsAllocatedSlots[index / 32] &= ~(uint)(1 << (index % 32));
                }

                obj.ObjectID = 0; // invalidate object id
                _objectsInRegion--;
            }
        }

        /// <summary>
        /// Gets the object with the specified ID
        /// </summary>
        /// <param name="id">The ID of the object to get</param>
        /// <returns>The object with the specified ID, null if it didn't exist</returns>
        public GameObject GetObject(ushort id)
        {
            if (_objects == null || id <= 0 || id > _objects.Length)
                return null;

            return _objects[id - 1];
        }

        /// <summary>
        /// Returns the zone that contains the specified x and y values
        /// </summary>
        /// <param name="x">X value for the zone you're retrieving</param>
        /// <param name="y">Y value for the zone you're retrieving</param>
        /// <returns>The zone you're retrieving or null if it couldn't be found</returns>
        public Zone GetZone(int x, int y)
        {
            int varX = x;
            int varY = y;
            foreach (Zone zone in _zones)
            {
                if (zone.XOffset <= varX && zone.YOffset <= varY && (zone.XOffset + zone.Width) > varX && (zone.YOffset + zone.Height) > varY)
                    return zone;
            }

            return null;
        }

        /// <summary>
        /// Gets the X offset for the specified zone
        /// </summary>
        /// <param name="x">X value for the zone's offset you're retrieving</param>
        /// <param name="y">Y value for the zone's offset you're retrieving</param>
        /// <returns>The X offset of the zone you specified or 0 if it couldn't be found</returns>
        public int GetXOffInZone(int x, int y)
        {
            Zone z = GetZone(x, y);
            return z == null ? 0 : x - z.XOffset;
        }

        /// <summary>
        /// Gets the Y offset for the specified zone
        /// </summary>
        /// <param name="x">X value for the zone's offset you're retrieving</param>
        /// <param name="y">Y value for the zone's offset you're retrieving</param>
        /// <returns>The Y offset of the zone you specified or 0 if it couldn't be found</returns>
        public int GetYOffInZone(int x, int y)
        {
            Zone z = GetZone(x, y);
            return z == null ? 0 : y - z.YOffset;
        }

        /// <summary>
        /// Check if this region is a capital city
        /// </summary>
        /// <returns>True, if region is a capital city, else false</returns>
        public virtual bool IsCapitalCity
        {
            get
            {
                switch (Skin)
                {
                    case 10: // Camelot City
                    case 101: // Jordheim
                    case 201: // Tir na Nog
                        return true; // Tir na Nog
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Check if this region is a housing zone
        /// </summary>
        /// <returns>True, if region is a housing zone, else false</returns>
        public virtual bool IsHousing
        {
            get
            {
                switch (Skin) // use the skin of the region
                {
                    case 2: // Housing alb
                    case 102: // Housing mid
                    case 202: // Housing hib
                        return true; // Housing hib
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Check if the given region is Atlantis.
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        public static bool IsAtlantis(int regionId)
        {
            return regionId is 30 or 73 or 130;
        }

        #endregion

        #region Area

        public virtual IArea AddArea(IArea area)
        {
            _areasLock.EnterWriteLock();

            try
            {
                ushort nextAreaID = 0;

                foreach (ushort areaID in Areas.Keys)
                {
                    if (areaID >= nextAreaID)
                        nextAreaID = (ushort) (areaID + 1);
                }

                area.ID = nextAreaID;
                Areas.Add(area.ID, area);

                foreach (Zone zone in Zones)
                {
                    if (area.IsIntersectingZone(zone))
                    {
                        if (!_zoneAreas.TryGetValue(zone, out List<IArea> zoneAreas))
                        {
                            zoneAreas = new();
                            _zoneAreas[zone] = zoneAreas;
                        }

                        zoneAreas.Add(area);
                    }
                }

                return area;
            }
            finally
            {
                _areasLock.ExitWriteLock();
            }
        }

        public virtual void RemoveArea(IArea area)
        {
            _areasLock.EnterWriteLock();

            try
            {
                if (!Areas.Remove(area.ID))
                    return;

                foreach (var zoneAreas in _zoneAreas.Values)
                    zoneAreas.Remove(area);
            }
            finally
            {
                _areasLock.ExitWriteLock();
            }
        }

        public virtual void GetAreasOfSpot(IPoint3D point, List<IArea> results)
        {
            Zone zone = GetZone(point.X, point.Y);

            if (zone != null)
                GetAreasOfZone(zone, point, true, results);
        }

        public virtual void GetAreasOfSpot(int x, int y, int z, List<IArea> results)
        {
            Zone zone = GetZone(x, y);

            if (zone != null)
                GetAreasOfZone(zone, x, y, z, results);
        }

        public virtual void GetAreasOfZone(Zone zone, IPoint3D p, List<IArea> results)
        {
            GetAreasOfZone(zone, p, true, results);
        }

        public virtual void GetAreasOfZone(Zone zone, IPoint3D p, bool checkZ, List<IArea> results)
        {
            results.Clear();

            if (zone == null)
                return;

            _areasLock.EnterReadLock();

            try
            {
                if (_zoneAreas.TryGetValue(zone, out List<IArea> zoneAreas))
                {
                    foreach (var area in zoneAreas)
                    {
                        if (area.IsContaining(p, checkZ))
                            results.Add(area);
                    }
                }
            }
            finally
            {
                _areasLock.ExitReadLock();
            }
        }

        public virtual void GetAreasOfZone(Zone zone, int x, int y, int z, List<IArea> results)
        {
            results.Clear();

            if (zone == null)
                return;

            _areasLock.EnterReadLock();

            try
            {
                if (_zoneAreas.TryGetValue(zone, out List<IArea> zoneAreas))
                {
                    foreach (var area in zoneAreas)
                    {
                        if (area.IsContaining(x, y, z))
                            results.Add(area);
                    }
                }
            }
            finally
            {
                _areasLock.ExitReadLock();
            }
        }

        public virtual List<IArea> GetAreasOfSpot(IPoint3D point)
        {
            var results = GameLoop.GetListForTick<IArea>();
            GetAreasOfSpot(point, results);
            return results;
        }

        public virtual List<IArea> GetAreasOfSpot(int x, int y, int z)
        {
            var results = GameLoop.GetListForTick<IArea>();
            GetAreasOfSpot(x, y, z, results);
            return results;
        }

        public virtual List<IArea> GetAreasOfZone(Zone zone, IPoint3D p, bool checkZ)
        {
            var results = GameLoop.GetListForTick<IArea>();
            GetAreasOfZone(zone, p, checkZ, results);
            return results;
        }

        public virtual List<IArea> GetAreasOfZone(Zone zone, int x, int y, int z)
        {
            var results = GameLoop.GetListForTick<IArea>();
            GetAreasOfZone(zone, x, y, z, results);
            return results;
        }

        #endregion

        #region Notify

        public virtual void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.Notify(e, sender, args);
        }

        public virtual void Notify(DOLEvent e, object sender)
        {
            Notify(e, sender, null);
        }

        public virtual void Notify(DOLEvent e)
        {
            Notify(e, null, null);
        }

        public virtual void Notify(DOLEvent e, EventArgs args)
        {
            Notify(e, null, args);
        }

        #endregion

        #region Get in radius

        public void GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) where T : GameObject
        {
            if (list.Count > 0)
            {
                if (log.IsErrorEnabled)
                    log.Error($"GetInRadius: list is not empty, clearing it.{Environment.NewLine}{Environment.StackTrace}");

                list.Clear();
            }

            Zone startingZone = GetZone(point.X, point.Y);

            if (startingZone == null)
                return;

            startingZone.GetObjectsInRadius(point, objectType, radius, list);
            uint sqRadius = (uint) radius * radius;

            foreach (Zone currentZone in _zones)
            {
                if (currentZone != startingZone && currentZone.ObjectCount > 0 && CheckShortestDistance(currentZone, point.X, point.Y, sqRadius))
                    currentZone.GetObjectsInRadius(point, objectType, radius, list);
            }
        }

        /// <summary>
        /// get the shortest distance from a point to a zone
        /// </summary>
        /// <param name="zone">The zone to check</param>
        /// <param name="x">X value of the point</param>
        /// <param name="y">Y value of the point</param>
        /// <param name="squareRadius">The square radius to compare the distance with</param>
        /// <returns>True if the distance is shorter false either</returns>
        private static bool CheckShortestDistance(Zone zone, int x, int y, uint squareRadius)
        {
            //  coordinates of zone borders
            int xLeft = zone.XOffset;
            int xRight = zone.XOffset + zone.Width;
            int yTop = zone.YOffset;
            int yBottom = zone.YOffset + zone.Height;
            long distance;

            if ((y >= yTop) && (y <= yBottom))
            {
                int xDiff = Math.Min(Math.Abs(x - xLeft), Math.Abs(x - xRight));
                distance = (long)xDiff * xDiff;
            }
            else
            {
                if ((x >= xLeft) && (x <= xRight))
                {
                    int yDiff = Math.Min(Math.Abs(y - yTop), Math.Abs(y - yBottom));
                    distance = (long)yDiff * yDiff;
                }
                else
                {
                    int xDiff = Math.Min(Math.Abs(x - xLeft), Math.Abs(x - xRight));
                    int yDiff = Math.Min(Math.Abs(y - yTop), Math.Abs(y - yBottom));
                    distance = (long)xDiff * xDiff + (long)yDiff * yDiff;
                }
            }

            return distance <= squareRadius;
        }

        [Obsolete("Deprecated. Use GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) instead.")]
        public List<GameStaticItem> GetItemsInRadius(Point3D point, ushort radius)
        {
            List<GameStaticItem> result = new();
            GetInRadius(point, eGameObjectType.ITEM, radius, result);
            return result;
        }

        [Obsolete("Deprecated. Use GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) instead.")]
        public List<GameNPC> GetNPCsInRadius(Point3D point, ushort radius)
        {
            List<GameNPC> result = new();
            GetInRadius(point, eGameObjectType.NPC, radius, result);
            return result;
        }

        [Obsolete("Deprecated. Use GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) instead.")]
        public List<GamePlayer> GetPlayersInRadius(Point3D point, ushort radius)
        {
            List<GamePlayer> result = new();
            GetInRadius(point, eGameObjectType.PLAYER, radius, result);
            return result;
        }

        [Obsolete("Deprecated. Use GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) instead.")]
        public List<GameDoorBase> GetDoorsInRadius(Point3D point, ushort radius)
        {
            List<GameDoorBase> result = new();
            GetInRadius(point, eGameObjectType.DOOR, radius, result);
            return result;
        }

        #endregion
    }
}
