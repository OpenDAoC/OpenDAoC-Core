using Core.AI.Brain;

namespace Core.GS;

public class Yadda : GameNpc
{
    public Yadda() : base() { }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168085);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;

        YaddaBrain sbrain = new YaddaBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false;//load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
}