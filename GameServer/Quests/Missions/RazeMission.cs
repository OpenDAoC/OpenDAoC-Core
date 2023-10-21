using System;
using System.Collections;
using System.Collections.Generic;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Keeps;

namespace Core.GS.Quests
{
	public class RazeMission : AMission
	{
		private AGameKeep m_keep = null;

		public RazeMission(object owner)
			: base(owner)
		{
			ERealm realm = 0;
			if (owner is GroupUtil)
				realm = (owner as GroupUtil).Leader.Realm;
			else if (owner is GamePlayer)
				realm = (owner as GamePlayer).Realm;

			ArrayList list = new ArrayList();

			ICollection<AGameKeep> keeps;
			if (owner is GroupUtil)
				keeps = GameServer.KeepManager.GetKeepsOfRegion((owner as GroupUtil).Leader.CurrentRegionID);
			else if (owner is GamePlayer)
				keeps = GameServer.KeepManager.GetKeepsOfRegion((owner as GamePlayer).CurrentRegionID);
			else keeps = new List<AGameKeep>();

			foreach (AGameKeep keep in keeps)
			{
				if (keep.IsPortalKeep)
					continue;
				if (keep is GameKeepTower && keep.Realm != realm)
					list.Add(keep);
			}

			if (list.Count > 0)
				m_keep = list[Util.Random(list.Count - 1)] as AGameKeep;

			GameEventMgr.AddHandler(KeepEvent.TowerRaized, new CoreEventHandler(Notify));
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			if (e != KeepEvent.TowerRaized)
				return;

			KeepEventArgs kargs = args as KeepEventArgs;

			if (kargs.Keep != m_keep)
				return;

			FinishMission();
		}

		public override void FinishMission()
		{
			base.FinishMission();
			GameEventMgr.RemoveHandler(KeepEvent.TowerRaized, new CoreEventHandler(Notify));
		}

		public override void ExpireMission()
		{
			base.ExpireMission();
			GameEventMgr.RemoveHandler(KeepEvent.TowerRaized, new CoreEventHandler(Notify));
		}

		public override string Description
		{
			get
			{
				if (m_keep == null)
					return "Keep is null when trying to send the description";
				else return "Raze " + m_keep.Name;
			}
		}
	}
}