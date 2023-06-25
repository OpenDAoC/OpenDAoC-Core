﻿using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.Tests.Unit.Gameserver
{
    public class FakePacketLib : PacketLib1124
    {
        public FakePacketLib() : base(null) { }

        public override void SendCheckLOS(GameObject Checker, GameObject Target, CheckLOSResponse callback) { }
        public override void SendMessage(string msg, eChatType type, eChatLoc loc) { }
        public override void SendUpdateIcons(System.Collections.IList changedEffects, ref int lastUpdateEffectsCount) { }
        public override void SendConcentrationList() { }
        public override void SendCharStatsUpdate() { }
        public override void SendUpdateWeaponAndArmorStats() { }
        public override void SendUpdateMaxSpeed() { }
        public override void SendEncumberance() { }
        public override void SendStatusUpdate() { }
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
        public override byte[] AcquirePacketBuffer() => new byte[] { };
        public void SetDatabase(IObjectDatabase database) { this.database = database; }

        public static void Load() => LoadTestDouble(new FakeServer());
    }

    public class FakeDatabase : IObjectDatabase
    {
        public IList<DataObject> SelectObjectReturns { get; set; } = new List<DataObject>();

        public bool AddObject(DataObject dataObject) => throw new NotImplementedException();
        public bool AddObject(IEnumerable<DataObject> dataObjects) => throw new NotImplementedException();
        public bool DeleteObject(DataObject dataObject) => throw new NotImplementedException();
        public bool DeleteObject(IEnumerable<DataObject> dataObjects) => throw new NotImplementedException();
        public string Escape(string rawInput) => throw new NotImplementedException();
        public bool ExecuteNonQuery(string rawQuery) => throw new NotImplementedException();
        public void FillObjectRelations(DataObject dataObject) => throw new NotImplementedException();
        public void FillObjectRelations(IEnumerable<DataObject> dataObject) => throw new NotImplementedException();
        public TObject FindObjectByKey<TObject>(object key) where TObject : DataObject => throw new NotImplementedException();
        public IList<TObject> FindObjectsByKey<TObject>(IEnumerable<object> keys) where TObject : DataObject => throw new NotImplementedException();
        public int GetObjectCount<TObject>() where TObject : DataObject => throw new NotImplementedException();
        public int GetObjectCount<TObject>(string whereExpression) where TObject : DataObject => throw new NotImplementedException();
        public void RegisterDataObject(Type dataObjectType) => throw new NotImplementedException();
        public bool SaveObject(DataObject dataObject) => true;
        public bool SaveObject(IEnumerable<DataObject> dataObjects) => throw new NotImplementedException();
        public IList<TObject> SelectAllObjects<TObject>() where TObject : DataObject => throw new NotImplementedException();

        public TObject SelectObject<TObject>(WhereClause whereClause) where TObject : DataObject => (TObject)SelectObjectReturns.FirstOrDefault();
        public IList<TObject> SelectObjects<TObject>(WhereClause whereClause) where TObject : DataObject => (IList<TObject>)SelectObjectReturns;
        public IList<IList<TObject>> MultipleSelectObjects<TObject>(IEnumerable<WhereClause> whereClauseBatch) where TObject : DataObject => throw new NotImplementedException();

        public bool UpdateInCache<TObject>(object key) where TObject : DataObject => false;
        public bool UpdateObjsInCache<TObject>(IEnumerable<object> keys) where TObject : DataObject => throw new NotImplementedException();
    }

    public class UtilChanceIsHundredPercent : Util
    {
        protected override int RandomImpl(int min, int max) => 100;

        public static void Enable()
        {
            Util.LoadTestDouble(new UtilChanceIsHundredPercent());
        }
    }
}
