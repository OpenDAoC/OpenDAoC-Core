/*
 * Please make note of the AddToWorld function,
 * aswell as the Die function.
 *
 * Add To World:
 * - you MUST set the mobs spawn location
 * - you MUST name your mob
 * - you MUST set the Relic associated by Relic ID, or Relic InternalID.
 * - you MUST call the LockRelic(); method.
 *
 * Die:
 * - you MUST call the UnlockRelic(); method.
 *
 *
 * Other than these things, you are free to do whatever you want.
 * Classes should inherit from "BaseProtector"
 *
 */

using Core.GS.World;

namespace Core.GS;

//all classes should inherit from BaseProtector.
public class ArrektosProtector : BaseProtector
{
    public const string ALREADY_GOT_HELP = "ALREADY_GOT_HELP";

    public override bool AddToWorld()
    {
        //foreman fogo doesn't leave the room.
        TetherRange = 1000;

        X = 49293;
        Y = 42208;
        Z = 27562;
        Heading = 2057;
        CurrentRegionID = 245;

        Flags = 0;

        Level = 56;
        Model = 2249; //undead Minotaur
        Name = "Forge Foreman Fogo";
        Size = 65;

        //get the relic by its ID, and lock it!
        Relic = MinotaurRelicMgr.GetRelic(1);
        LockRelic();

        TempProperties.SetProperty(ALREADY_GOT_HELP, false);

        return base.AddToWorld();
    }
    public override void StartAttack(GameObject target)
    {
        if (!TempProperties.GetProperty<bool>(ALREADY_GOT_HELP))
        {
            foreach (GameNpc npc in GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                //on initial attack, all fireborn in range add!
                if (npc.Name == "minotaur fireborn")
                npc.attackComponent.RequestStartAttack(target);
            }

            TempProperties.SetProperty(ALREADY_GOT_HELP, true);
        }

        base.StartAttack(target);
    }
    public override void Die(GameObject killer)
    {
        base.Die(killer);

        TempProperties.SetProperty(ALREADY_GOT_HELP, false);

        //when the protector is dead, the relic should be unlocked!
        UnlockRelic();

        //another thing is that most of these mobs drop 1 time drops
        //i haven't added support for this, but someone will eventually.
    }
}