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

				if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Shade))
					continue;

				player.Out.SendCheckLos(Body, player, new CheckLosResponse(LosCheckForAggroCallback));
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
