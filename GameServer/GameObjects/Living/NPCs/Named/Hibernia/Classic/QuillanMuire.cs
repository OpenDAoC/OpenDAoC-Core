using Core.GS.AI.Brains;
using Core.GS.GameUtils;

namespace Core.GS;

public class QuillanMuire : GameNpc
{
	public QuillanMuire() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165094);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		Faction = FactionMgr.GetFactionByID(782);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(782));

		QuillanMuireBrain sbrain = new QuillanMuireBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}

#region Muire herbalist
public class MuireHerbalist : GameNpc
{
	public MuireHerbalist() : base() { }

	#region Stats
	public override short Constitution { get => base.Constitution; set => base.Constitution = 100; }
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 180; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "Muire herbalist";
		Level = (byte)Util.Random(18, 19);
		Model = 446;
		Size = 52;
		Faction = FactionMgr.GetFactionByID(782);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(782));
		MuireHerbalistBrain sbrain = new MuireHerbalistBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}
#endregion Muire herbalist