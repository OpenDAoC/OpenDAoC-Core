using System;

namespace Core.GS;

public class SummonDjinnStone : DjinnStone
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Creates and summons the djinn if it isn't already up.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;

		if (Djinn == null)
		{
			try
			{
				Djinn = new SummonedDjinn(this);
			}
			catch (Exception e)
			{
				log.Warn(String.Format("Unable to create ancient bound djinn: {0}", e.Message));
				return false;
			}
		}

		if (!Djinn.IsSummoned)
			Djinn.Summon();

		return true;
	}
}