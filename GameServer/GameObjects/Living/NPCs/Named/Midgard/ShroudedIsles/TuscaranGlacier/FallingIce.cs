using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS;

public class FallingIce : GameNpc
{
    public FallingIce() : base() { }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
                || damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                || damageType == EDamageType.Slash)
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
            else
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }
    public override int MaxHealth
    {
        get { return 20000; }
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override ENpcFlags Flags { get => base.Flags; set => base.Flags = (ENpcFlags)12; }
    public override bool AddToWorld()
    {
        Model = 913;
        Name = "falling ice";
        Size = 100;
        Level = 70;
        MaxSpeedBase = 0;
        RespawnInterval = Util.Random(30000, 50000);

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        FallingIceBrain adds = new FallingIceBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;//load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
}