/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 ***************Made by Scrubtasticx*******************************************
 */
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

        private static ItemTemplate m_token_many = GameServer.Database.FindObjectByKey<ItemTemplate>("token_many");

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