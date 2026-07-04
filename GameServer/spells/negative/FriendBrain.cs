using DOL.GS;
using DOL.GS.Spells;
using DOL.GS.Effects;

namespace DOL.AI.Brain
{
	public class FriendBrain : StandardMobBrain
	{
		SpellHandler m_spellHandler = null;
		public FriendBrain(SpellHandler spellHandler) : base()
		{
			ThinkInterval = 3000;
			m_spellHandler = spellHandler;
		}

		protected override void CheckPlayerAggro()
		{
			if (m_spellHandler == null)
				return;

			foreach (var player in BuildPlayerAggroCandidateLoop())
			{
				if (AggroList.ContainsKey(player))
					continue; // add only new players
				if (!player.IsAlive || player.ObjectState != GameObject.eObjectState.Active || player.IsStealthed)
					continue;
				if (player.Steed != null)
					continue; //do not attack players on steed
				if (player == m_spellHandler.Caster)
					continue;
				if (!GameServer.ServerRules.IsAllowedToAttack(m_spellHandler.Caster, player, true))
					continue;

				AddToAggroList(player);
			}
		}

		protected override void CheckNpcAggro()
		{
			if (m_spellHandler == null)
				return;

			foreach (var npc in BuildNpcAggroCandidateLoop())
			{
				if (GameServer.ServerRules.IsAllowedToAttack(m_spellHandler.Caster, npc, true))
				{
					AddToAggroList(npc);
					return;
				}
			}
		}
	}
}
