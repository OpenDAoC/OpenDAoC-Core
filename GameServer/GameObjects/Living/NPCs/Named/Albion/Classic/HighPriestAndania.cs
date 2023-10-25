using System;
using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS;

public class HighPriestAndania : GameNpc
{
	public HighPriestAndania() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12276);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		HighPriestAndaniaBrain sbrain = new HighPriestAndaniaBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(2500))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_SystemWindow);
		}
	}
	public override void Die(GameObject killer)
    {
		BroadcastMessage(String.Format("The {0} says, \"The {1} vanishes and his final words linger in the air, 'You may have defeated us here, but we shall meet again someday!'\"",Name,Name));
		base.Die(killer);
    }
}