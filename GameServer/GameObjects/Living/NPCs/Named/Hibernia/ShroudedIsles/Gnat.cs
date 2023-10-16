using DOL.AI.Brain;

namespace DOL.GS.Scripts;

public class Gnat : GameNpc
{
    public Gnat() : base()
    {
    }

    public static GameNpc SI_Gnat = new GameNpc();

    public override bool AddToWorld()
    {
        Model = 917;
        Name = "Gnat";
        Size = 50;
        Level = 35;
        Gender = EGender.Neutral;

        BodyType = 6; // Humanoid
        MaxDistance = 1500;
        TetherRange = 2000;
        RoamingRange = 400;
        GnatBrain sBrain = new GnatBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        GnatBrain.spawnants = true;
        base.AddToWorld();
        return true;
    }
}

public class GnatAnts : GameNpc
{
    public GnatAnts() : base()
    {
    }

    public static GameNpc SI_Gnatants = new GameNpc();

    public override int MaxHealth
    {
        get { return 450 * Constitution / 100; }
    }

    public override bool AddToWorld()
    {
        Model = 115;
        Name = "fiery ant";
        MeleeDamageType = EDamageType.Thrust;
        RoamingRange = 350;
        RespawnInterval = -1;
        MaxDistance = 1500;
        TetherRange = 2000;
        IsWorthReward = false; //worth no reward
        Size = (byte) Util.Random(8, 12);
        Level = (byte) Util.Random(30, 34);
        Realm = ERealm.None;
        GnatAntsBrain adds = new GnatAntsBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }

    public override void Die(GameObject killer)
    {
        base.Die(null); //null to not gain experience
    }
}