using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.Keeps
{
	/// <summary>
	/// Class for the Relic Guard
	/// </summary>
    public class GuardFighterRelic : GameKeepGuard
	{
		/// <summary>
        /// Relic Guard needs more health
		/// </summary>
		public override int MaxHealth
		{
			get
			{
				return base.MaxHealth * 3;
			}
		}

        /// <summary>
        /// When Relic Guard Die and it isnt a keep reset (this killer) we call GuardRelicSpam function
        /// </summary>
        /// <param name="killer"></param>
        public override void Die(GameObject killer)
        {
            if (killer != this)
                GuardRelicSpam(this);
            base.Die(killer);
            if (RespawnInterval == -1)
                Delete();
        }

        #region Guard Spam

        /// <summary>
        /// Sends message to Realm for guard death with enemy count in area
        /// </summary>
        /// <param name="guard">The guard object</param>
        public static void GuardRelicSpam(GameKeepGuard guard)
        {
            int inArea = guard.GetEnemyCountInArea();
            string message = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GuardRelicSpam.Killed", guard.Component.Keep.Name, guard.Name, inArea);
            PlayerMgr.BroadcastMessage(message, guard.Realm);
        }

        #endregion

		public override long MoneyValue
		{
			get
			{
				if (this.Component != null && this.Component.Keep != null)
				{
					return this.Component.Keep.MoneyValue();
				}

				return base.MoneyValue;
			}
		}

		/// <summary>
        /// From a great distance, damage does not harm Relic Guard
		/// </summary>
		/// <param name="source">The source of the damage</param>
		/// <param name="damageType">The type of the damage</param>
		/// <param name="damageAmount">The amount of the damage</param>
		/// <param name="criticalAmount">The critical hit amount of damage</param>
		public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
		{
			int distance = 800;

			// check to make sure pets and pet casters are in range
			GamePlayer attacker = null;
			if (source is GamePlayer)
			{
				attacker = source as GamePlayer;
			}
			else if (source is GameNpc && (source as GameNpc).Brain != null && (source as GameNpc).Brain is IControlledBrain && (((source as GameNpc).Brain as IControlledBrain).Owner) is GamePlayer)
			{
				attacker = ((source as GameNpc).Brain as IControlledBrain).Owner as GamePlayer;
			}

			if ((attacker != null && IsWithinRadius(attacker, distance) == false) || IsWithinRadius(source, distance) == false)
			{
				if (attacker != null)
					attacker.Out.SendMessage(this.Name + " can't be attacked from this distance.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				return;
			}
			base.TakeDamage(source, damageType, damageAmount, criticalAmount);
		}
    }
}
