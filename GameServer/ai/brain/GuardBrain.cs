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
 */
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain
{
	public class GuardBrain : StandardMobBrain
	{
		public override int ThinkInterval => 2000;
		public override int AggroLevel => 90;
		public override int AggroRange => 750;

		public GuardBrain() : base() { }

		protected override void CheckPlayerAggro()
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
			{
				if (!CanAggroTarget(player))
					continue;
				if (player.IsStealthed || player.Steed != null)
					continue;

				player.Out.SendCheckLOS(Body, player, new CheckLOSResponse(LosCheckForAggroCallback));
				// We don't know if the LoS check will be positive, so we have to ask other players
			}
		}

		protected override void CheckNPCAggro()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
			{
				if (!CanAggroTarget(npc))
					continue;
				if ((npc.Flags & GameNPC.eFlags.FLYING) != 0)
					continue;

				AddToAggroList(npc, npc.Level << 1);
				// No LoS check, we just attack what's in range
				return;
			}
		}

		/// <summary>
		/// We override this because we want guards to attack even gray npcs
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public override bool CanAggroTarget(GameLiving target)
		{
			return AggroLevel > 0 && GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
		}
	}
}
