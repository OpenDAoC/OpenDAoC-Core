using System;
using System.Collections;
using System.Collections.Generic;
using Core.Events;
using Core.GS.Keeps;

namespace Core.GS.Quests
{
	public class CaptureMission : AMission
	{
		private AGameKeep m_keep = null;

		public CaptureMission(ECaptureType type, object owner, string hint)
			: base(owner)
		{
			ERealm realm = ERealm.None;
			if (owner is GroupUtil)
				realm = (owner as GroupUtil).Leader.Realm;
			else if (owner is GamePlayer)
				realm = (owner as GamePlayer).Realm;

			ArrayList list = new ArrayList();

			switch (type)
			{
				case ECaptureType.Tower:
					{
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
						break;
					}
				case ECaptureType.Keep:
					{
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
							if (keep is GameKeep && keep.Realm != realm)
								list.Add(keep);
						}
						break;
					}
			}

			if (list.Count > 0)
			{
				if (hint != "")
				{
					foreach (AGameKeep keep in list)
					{
						if (keep.Name.ToLower().Contains(hint))
						{
							m_keep = keep;
							break;
						}
					}
				}

				if (m_keep == null)
					m_keep = list[Util.Random(list.Count - 1)] as AGameKeep;
			}

			GameEventMgr.AddHandler(KeepEvent.KeepTaken, new CoreEventHandler(Notify));
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			if (e != KeepEvent.KeepTaken)
				return;

			KeepEventArgs kargs = args as KeepEventArgs;

			if (kargs.Keep != m_keep)
				return;

			GamePlayer testPlayer = null;
			if (m_owner is GamePlayer)
				testPlayer = m_owner as GamePlayer;
			else if (m_owner is GroupUtil)
				testPlayer = (m_owner as GroupUtil).Leader;

			if (testPlayer != null)
			{
				foreach (AbstractArea area in testPlayer.CurrentAreas)
				{
					if (area is KeepArea && (area as KeepArea).Keep == m_keep)
					{
						FinishMission();
					}
				}
			}

			ExpireMission();
		}

		public override void FinishMission()
		{
			base.FinishMission();
			GameEventMgr.RemoveHandler(KeepEvent.KeepTaken, new CoreEventHandler(Notify));
		}

		public override void ExpireMission()
		{
			base.ExpireMission();
			GameEventMgr.RemoveHandler(KeepEvent.KeepTaken, new CoreEventHandler(Notify));
		}

		public override string Description
		{
			get
			{
				if (m_keep == null)
					return "Keep is null when trying to send the description";
				else return "Capture " + m_keep.Name;
			}
		}

		public override long RewardRealmPoints
		{
			get
			{
				if (m_keep is GameKeep)
					return 1500;
				else if (m_keep is GameKeepTower)
					return 250 + (m_keep.Level * 50);
				else return 0;
			}
		}
	}
}