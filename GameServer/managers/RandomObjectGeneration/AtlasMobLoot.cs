using DOL.AI.Brain;

namespace DOL.GS
{

    /// <summary>
    /// ROGMobGenerator
    /// At the moment this generator only adds ROGs to the loot
    /// </summary>
    public class ROGMobGenerator : LootGeneratorBase
    {

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
                if (lvl < 1)
                {
                    lvl = 1;
                }

                if (player.Level < 50){
                    AtlasROGManager.GenerateROG(player, true);
                }

            }
            catch
            {
                return loot;
            }

            return loot;
        }
    }
}