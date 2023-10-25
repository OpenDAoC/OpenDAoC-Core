using Core.GS.Enums;

namespace Core.GS.Keeps;

public class RelicGateMgr
{
	/// <summary>
	/// Albion Power Relic Wall - The first milegate can be opened by taking Caer Benowyc, Caer Berkstead, and Caer Boldiam. The second milegate on this wall can be opened by taking Caer Renaris after the other three have fallen.
	/// </summary>
	private static int[] ALB_POWER = new int[] { 50, 51, 53 };
	private static GameRelicDoor Door_Alb_Power = null;
	/// <summary>
	/// Albion Strength Relic Wall - The first milegate can be opened by taking Caer Benowyc, Caer Erasleigh, and Caer Sursbrooke. The second milegate on this wall is opened by taking Caer Hurbury in addition to the other three.
	/// </summary>
	private static int[] ALB_STRENGTH = new int[] { 50, 52, 54 };
	private static GameRelicDoor Door_Alb_Strength = null;
	/// <summary>
	/// Midgard Power Relic Wall - First milegate: Bledmeer Faste, Notmoor Faste, and Glenlock Faste. Second milegate: Those three and Arvakr Faste.
	/// </summary>
	private static int[] MID_POWER = new int[] { 75, 76, 79 };
	private static GameRelicDoor Door_Mid_Power = null;
	/// <summary>
	/// Midgard Strength Relic Wall - First milegate: Bledmeer Faste, Blendrake Faste, and Hlidskialf Faste. Second milegate: Those three and Fensalir Faste.
	/// </summary>
	private static int[] MID_STRENGTH = new int[] { 75, 78, 77 };
	private static GameRelicDoor Door_Mid_Strength = null;
	/// <summary>
	/// Hibernia Power Relic Wall - First milegate: Dun Crauchon, Dun Crimthain, and Dun nGed. Second milegate: Those three and Dun S caithag.
	/// </summary>
	private static int[] HIB_POWER = new int[] { 100, 101, 103 };
	private static GameRelicDoor Door_Hib_Power = null;
	/// <summary>
	/// Hibernia Strength Relic Wall - First milegate: Dun Crauchon, Dun Bolg, and Dun da Behnn. Second milegate: Those three and Dun Ailinne. 
	/// </summary>
	private static int[] HIB_STRENGTH = new int[] { 100, 102, 104 };
	private static GameRelicDoor Door_Hib_Strength = null;

	private static GameRelicDoor GetRelicGateByChain(int[] chain)
	{
		if (chain == ALB_POWER)
			return Door_Alb_Power;
		else if (chain == ALB_STRENGTH)
			return Door_Alb_Strength;
		else if (chain == MID_POWER)
			return Door_Mid_Power;
		else if (chain == MID_STRENGTH)
			return Door_Mid_Strength;
		else if (chain == HIB_POWER)
			return Door_Hib_Power;
		else if (chain == HIB_STRENGTH)
			return Door_Hib_Strength;
		return null;
	}

	public static void CheckKeeps()
	{
		CheckKeepChain(ALB_POWER);
	}

	private static void CheckKeepChain(int[] chain)
	{
		GameRelicDoor door = GetRelicGateByChain(chain);
		if (door == null)
			return;
		foreach (int keepid in chain)
		{
			AGameKeep keep = GameServer.KeepManager.GetKeepByID(keepid);
			if (keep == null)
				continue;

			//if the door is open, we see if we own a keep
			if (door.State == EDoorState.Open)
			{
				if (keep.Realm != keep.OriginalRealm)
					continue;

				door.CloseDoor();
			}
			//if the door is closed, we check if the line should open it
			else
			{
				if (keep.Realm == keep.OriginalRealm)
					continue;
				door.OpenDoor();
			}
		}
	}
}