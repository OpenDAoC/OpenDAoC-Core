using DOL.AI.Brain;
using DOL.Database;

namespace DOL.GS
{
    /// <summary>
    /// LootGeneratorExpOrb
    /// At the moment this generator only adds RedemptionOrb to the loot
    /// </summary>
    public class LootGeneratorExpOrb : LootGeneratorBase
    {

        private static string _currencyID = ServerProperties.Properties.ALT_CURRENCY_ID;
        private static DbItemTemplate m_token_many = GameServer.Database.FindObjectByKey<DbItemTemplate>(_currencyID);

        /// <summary>
        /// Generate loot for given mob
        /// </summary>
        /// <param name="mob"></param>
        /// <param name="killer"></param>
        /// <returns></returns>
        public override LootList GenerateLoot(GameNPC mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);

            try
            {
                GamePlayer player = killer as GamePlayer;
                if (killer is GameNPC && ((GameNPC)killer).Brain is IControlledBrain)
                {
                    player = ((ControlledNpcBrain)((GameNPC)killer).Brain).GetPlayerOwner();
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
}