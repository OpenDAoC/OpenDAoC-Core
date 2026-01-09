using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Logging;

namespace DOL.Tests.Unit.Gameserver
{
    public class FakePacketLib : PacketLib1124
    {
        public FakePacketLib() : base(null) { }

        public override bool SendLosCheckRequest(GameObject source, GameObject target, ILosCheckListener listener) { return true; }
        public override void SendMessage(string msg, eChatType type, eChatLoc loc) { }
        public override void SendUpdateIcons(System.Collections.IList changedEffects, ref int lastUpdateEffectsCount) { }
        public override void SendConcentrationList() { }
        public override void SendCharStatsUpdate() { }
        public override void SendUpdateWeaponAndArmorStats() { }
        public override void SendUpdateMaxSpeed() { }
        public override void SendEncumbrance() { }
        public override void SendStatusUpdate() { }
        public override void SendUpdateMoney() { }
    }

    public class FakeRegion : Region
    {
        public long FakeElapsedTime;

        public FakeRegion() : base(new RegionData()) { }

        public override ushort ID => 0;
    }

    public class FakeServer : GameServer
    {
        private IObjectDatabase database = new FakeDatabase();

        protected override IObjectDatabase DataBaseImpl => database;
        protected override void CheckAndInitDB() { }
        public void SetDatabase(IObjectDatabase database) { this.database = database; }

        public static void Load()
        {
            LoggerManager.InitializeWithExplicitLibrary(null, LogLibrary.None);
            LoadTestDouble(new FakeServer());
        }
    }

    public class FakeDatabase : IObjectDatabase
    {
        public IList<DataObject> SelectObjectReturns { get; set; } = new List<DataObject>();

        public bool AddObject(DataObject dataObject) => true;
        public bool AddObject(IEnumerable<DataObject> dataObjects) => true;
        public bool DeleteObject(DataObject dataObject) => true;
        public bool DeleteObject(IEnumerable<DataObject> dataObjects) => true;
        public string Escape(string rawInput) => string.Empty;
        public bool ExecuteNonQuery(string rawQuery) => true;
        public void FillObjectRelations(DataObject dataObject) { }
        public void FillObjectRelations(IEnumerable<DataObject> dataObjects) { }
        public TObject FindObjectByKey<TObject>(object key) where TObject : DataObject => default;
        public IList<TObject> FindObjectsByKey<TObject>(IEnumerable<object> keys) where TObject : DataObject => [];
        public int GetObjectCount<TObject>() where TObject : DataObject => 0;
        public int GetObjectCount<TObject>(string whereExpression) where TObject : DataObject => 0;
        public void RegisterDataObject(Type dataObjectType) { }
        public bool SaveObject(DataObject dataObject) => true;
        public bool SaveObject(IEnumerable<DataObject> dataObjects) => true;
        public IList<TObject> SelectAllObjects<TObject>() where TObject : DataObject => [];

        public TObject SelectObject<TObject>(WhereClause whereClause) where TObject : DataObject => (TObject)SelectObjectReturns.FirstOrDefault();
        public IList<TObject> SelectObjects<TObject>(WhereClause whereClause) where TObject : DataObject => (IList<TObject>)SelectObjectReturns;
        public IList<IList<TObject>> MultipleSelectObjects<TObject>(IEnumerable<WhereClause> whereClauseBatch) where TObject : DataObject => [];

        public bool UpdateInCache<TObject>(object key) where TObject : DataObject => true;
        public bool UpdateObjsInCache<TObject>(IEnumerable<object> keys) where TObject : DataObject => true;
    }
}
