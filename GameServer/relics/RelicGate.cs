// In /home/opendaoc/OpenDAoC-Core/GameServer/relics/RelicGate.cs

using System.Collections;
using System;
using DOL.GS;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Keeps;


namespace DOL.GS.Keeps
{
	/// <summary>
	    /// relic keep door in world
	    /// </summary>
	public class RelicGate : GameDoorBase
	{
		#region properties

		public override string Name
		{
			get
			{
				return "Relic Gate";
			}
		}

		public override uint Flag
		{
			get
			{
				return 4; // Relic Gate Flag
			}
		}

		#endregion

		#region function override

		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			return;
		}

		public override int ChangeHealth(GameObject changeSource, eHealthChangeType healthChangeType, int changeAmount)
		{
			return 0;
		}

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			if (player.IsMezzed || player.IsStunned)
			{
				player.Out.SendMessage("You are mesmerized or stunned!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return false;
			}


			if (GameServer.ServerRules.IsSameRealm(player, this, true) || player.Client.Account.PrivLevel != 1)
			{
				Point2D point;

				//calculate x y
				if (IsObjectInFront(player, 180))
					point = this.GetPointFromHeading(this.Heading, -150);
				else
					point = this.GetPointFromHeading(this.Heading, 150);

				//move player
				player.MoveTo(CurrentRegionID, point.X, point.Y, player.Z, player.Heading);
			}
			return base.Interact(player);
		}


		public override IList GetExamineMessages(GamePlayer player)
		{
			IList list = base.GetExamineMessages(player);
			string text = "You select the " + Name + ".";
			if (this.Realm == player.Realm)
				text = text + " It belongs to your realm.";
			else
			{
				text = text + " It belongs to an enemy realm!";
			}
			list.Add(text);
			return list;
		}

		public override string GetName(int article, bool firstLetterUppercase)
		{
			return "the " + base.GetName(article, firstLetterUppercase);
		}

		#endregion

		#region Save/load DB

		public override void LoadFromDatabase(DataObject obj)
		{
			base.LoadFromDatabase(obj);

			// Da wir wissen, dass der DoorMgr uns das DbDoor-Objekt übergibt, 
			// casten wir es, um auf die interne ID (vom Typ int) zuzugreifen.
			DbDoor dbDoor = obj as DbDoor;
			if (dbDoor != null)
			{
				// InternalID ist vom Typ INT, und das passt nun zur Methode in RelicGateMgr
				RelicGateMgr.AssignRelicDoor(this, dbDoor.InternalID);
			}
			// Wenn dbDoor == null, war das Objekt kein DbDoor (sehr ungewöhnlich),
			// daher wird in diesem Fall die Zuweisung übersprungen.
		}

		#endregion

		public override void Open(GameLiving opener = null)
		{
			// Vom Manager gesteuert, nicht von Spielern
		}

		public override void Close(GameLiving closer = null)
		{
			// Vom Manager gesteuert, nicht von Spielern
		}

		// Diese Methoden implementieren die eigentliche Statusänderung (vom RelicGateMgr aufgerufen)
		public virtual void OpenDoor()
		{
			State = eDoorState.Open;
		}

		public virtual void CloseDoor()
		{
			State = eDoorState.Closed;
		}

		// Whisper/Say-Logik beibehalten
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!base.WhisperReceive(source, str))
				return false;

			if (source is GamePlayer == false)
				return false;

			str = str.ToLower();

			if (str.Contains("enter") || str.Contains("exit"))
				Interact(source as GamePlayer);
			return true;
		}

		public override bool SayReceive(GameLiving source, string str)
		{
			if (!base.SayReceive(source, str))
				return false;

			if (source is GamePlayer == false)
				return false;

			str = str.ToLower();

			if (str.Contains("enter") || str.Contains("exit"))
				Interact(source as GamePlayer);
			return true;
		}

		public void NPCManipulateDoorRequest(GameNPC npc, bool open)
		{ }
	}
}