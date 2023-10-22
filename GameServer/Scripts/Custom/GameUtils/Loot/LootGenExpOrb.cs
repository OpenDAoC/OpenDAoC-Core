using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS.Scripts.Custom;

/// <summary>
/// LootGeneratorExpOrb
/// At the moment this generator only adds RedemptionOrb to the loot
/// </summary>
public class LootGeneratorExpOrb : LootGeneratorBase
{

    private static string _currencyID = ServerProperty.ALT_CURRENCY_ID;
    private static DbItemTemplate m_token_many = GameServer.Database.FindObjectByKey<DbItemTemplate>(_currencyID);

    /// <summary>
    /// Generate loot for given mob
    /// </summary>
    /// <param name="mob"></param>
    /// <param name="killer"></param>
    /// <returns></returns>
    public override LootList GenerateLoot(GameNpc mob, GameObject killer)
    {
        LootList loot = base.GenerateLoot(mob, killer);

        try
        {
            GamePlayer player = killer as GamePlayer;
            if (killer is GameNpc && ((GameNpc)killer).Brain is IControlledBrain)
            {
                player = ((ControlledNpcBrain)((GameNpc)killer).Brain).GetPlayerOwner();
            }

            if (player == null)
            {
                return loot;
            }
            
            int killedcon = (int)player.GetConLevel(mob) + 3;

            if (killedcon <= 0)
            {
                return loot;
            }

            int lvl = mob.Level + 1;

            int maxcount = Util.Random(player.Level, lvl);
            loot.AddFixed(m_token_many, maxcount);

        }
        catch
        {
            return loot;
        }

        return loot;
    }
}