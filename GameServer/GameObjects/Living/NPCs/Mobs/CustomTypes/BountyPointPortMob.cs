using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Scripts;

public class BountyPointPortMob : GameNpc
{
    #region Variables/Properties

    GameLiving m_BPPortMobTarget;

    public GameLiving BPPortMobTarget
    {
        get
        {
            return m_BPPortMobTarget;
        }
        set
        {
            m_BPPortMobTarget = value;
        }
    }

    public override int MaxHealth
    {
        get
        {
            return base.MaxHealth * 2;
        }
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * 2.5;
    }

    /// <summary>
    /// Gets or sets the base maxspeed of this living
    /// </summary>
/* Fix later Zycron      public override int MaxSpeedBase
    {
        get
        {
            return 191 + (Level * 2);
        }
        set
        {
            m_maxSpeedBase = value;
        }
    }
*/

    /// <summary>
    /// Melee Attack Range.
    /// </summary>
    public override int AttackRange
    {
        get
        {
            //Normal mob attacks have 200 ...
            return 400;
        }
        set { }
    }

    #endregion
    public override void Die(GameObject killer)
    {
        base.Die(killer);
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE)) //3600 units
        {
            player.ReceiveItem(this, "ML1token");// Adds an item into player inventory
            player.Out.SendMessage("An item has appeared in your inventory!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            player.GainBountyPoints((this.Level * 5));// grants BP at Server BP rate * 5 * level of mob
            player.MoveTo(90, 51597, 38366, 10858, 3281);// Moved player to specified location
        }
            DropLoot(killer);

            StartRespawn();
    }
}