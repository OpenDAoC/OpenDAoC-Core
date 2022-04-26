using System;
using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI.Brain
{
	public class JuggernautBrain : ControlledNpcBrain
	{
		protected readonly List<GameLiving> m_listDefensiveTarget;
		
		public JuggernautBrain(GameLiving owner) : base(owner)
		{
			m_listDefensiveTarget = new List<GameLiving>();
		}

		public List<GameLiving> ListDefensiveTarget
		{
			get { return m_listDefensiveTarget; }
		}
		
		/// <summary>
		/// [Ganrod] Nidel:
		/// Cast only Offensive or Defensive spell.
		/// <para>If Offensive spell is true, Defensive spell isn't casted.</para>
		/// </summary>
		public override void Think()
		{
            GamePlayer playerowner = GetPlayerOwner();

            long lastUpdate = 0;
            if (!playerowner.Client.GameObjectUpdateArray.TryGetValue(new Tuple<ushort, ushort>(Body.CurrentRegionID, (ushort)Body.ObjectID), out lastUpdate))
            {
                playerowner.Client.GameObjectUpdateArray.TryAdd(new Tuple<ushort, ushort>(Body.CurrentRegionID, (ushort)Body.ObjectID), lastUpdate);
            }

            if (playerowner != null && (GameTimer.GetTickCount() - playerowner.Client.GameObjectUpdateArray[new Tuple<ushort, ushort>(Body.CurrentRegionID, (ushort)Body.ObjectID)]) > ThinkInterval)
            {
                playerowner.Out.SendObjectUpdate(Body);
            }

            if (!CheckSpells(eCheckSpellType.Defensive))
            {
                AttackMostWanted();
            }
        }
	}
}