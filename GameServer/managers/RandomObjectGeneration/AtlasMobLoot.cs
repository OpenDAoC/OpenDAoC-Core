using DOL.AI.Brain;
using System.Collections.Generic;

namespace DOL.GS
{

    /// <summary>
    /// ROGMobGenerator
    /// At the moment this generator only adds ROGs to the loot
    /// </summary>
    public class ROGMobGenerator : LootGeneratorBase
    {

        //base chance in %
        public static ushort BASE_ROG_CHANCE = 15;


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

                eCharacterClass classForLoot = (eCharacterClass)player.CharacterClass.ID;
                // allow the leader to decide the loot realm
                if (player.Group != null)
                {
                    player = player.Group.Leader;
                    classForLoot = GetRandomClassFromGroup(player.Group);
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

        private eCharacterClass GetRandomClassFromGroup(Group group)
        {
            List<eCharacterClass> validClasses = new List<eCharacterClass>();
            foreach (GamePlayer player in group.GetMembersInTheGroup())
            {
                validClasses.Add((eCharacterClass)player.CharacterClass.ID);
            }

            return validClasses[Util.Random(validClasses.Count - 1)];

        }
    }