using Core.GS.AI.Brains;
using Core.GS.GameUtils;

namespace Core.GS;

public class BotonidSeedling : GameNpc
{
    public BotonidSeedling() : base()
    {
    }

    public BotonidSeedling(ABrain defaultBrain) : base(defaultBrain)
    {
    }

    public BotonidSeedling(INpcTemplate template) : base(template)
    {
    }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165666);
        LoadTemplate(npcTemplate);

        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;

        //seedling
        Model = 818;
        Size = 9;
        Name = "botonid seedling";

        Faction = FactionMgr.GetFactionByID(69);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(69));

        BotonidBrain sBrain = new BotonidBrain();
        SetOwnBrain(sBrain);

        //1.30min
        RespawnInterval = 90000;

        base.AddToWorld();
        return true;

        // 818 scaled to 9
    }
}