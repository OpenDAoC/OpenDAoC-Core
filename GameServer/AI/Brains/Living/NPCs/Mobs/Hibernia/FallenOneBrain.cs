using System;

namespace Core.GS.AI.Brains;

public class FallenOneBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public FallenOneBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 300;
		ThinkInterval = 1000;
	}
	ushort oldModel;
	ENpcFlags oldFlags;
	bool changed;
	public override void Think()
	{
		MakeFallenOneVisible();
		base.Think();
	}
	private void MakeFallenOneVisible()
    {
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160689);
		if (Body.CurrentRegion.IsNightTime == false)
		{
			if (changed == false)
			{
				oldFlags = Body.Flags;
				Body.Flags ^= ENpcFlags.CANTTARGET;
				Body.Flags ^= ENpcFlags.DONTSHOWNAME;
				Body.Flags ^= ENpcFlags.PEACE;

				if (oldModel == 0)
					oldModel = Body.Model;

				Body.Model = 1;
				changed = true;
			}
		}
		if (Body.CurrentRegion.IsNightTime)
		{
			if (changed)
			{
				Body.Flags = (ENpcFlags)npcTemplate.Flags;
				Body.Model = Convert.ToUInt16(npcTemplate.Model);
				changed = false;
			}
		}
	}
}