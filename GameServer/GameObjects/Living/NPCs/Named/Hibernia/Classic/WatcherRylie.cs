using Core.GS.AI;
using Core.GS.GameUtils;

namespace Core.GS;

public class WatcherRylie : GameNpc
{
	public WatcherRylie() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167795);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		Faction = FactionMgr.GetFactionByID(79);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(79));
		WatcherRylieBrain sbrain = new WatcherRylieBrain();
		
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}