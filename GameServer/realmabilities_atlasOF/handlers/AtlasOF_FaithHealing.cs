using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// 100% group heal
	/// </summary>
	public class AtlasOF_FaithHealing : TimedRealmAbility
	{

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public AtlasOF_FaithHealing(DBAbility dba, int level) : base(dba, level) { }

		public int Range = 2000;

		public override int MaxLevel { get { return 1; } }

		public override int CostForUpgrade(int level)
		{
			return 14;
		}

		
		
        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			bool didHeal = false;

			if (living.Group != null)
			{
				Point3D livingCurrentSpot = new Point3D(living.X, living.Y, living.Z);
				foreach (var player in living.Group.GetPlayersInTheGroup())
				{
					if (player.GetDistanceTo(livingCurrentSpot) <= Range)
					{
						int healAmount = player.MaxHealth;
						int healed = player.ChangeHealth(living, eHealthChangeType.Spell, healAmount);

						if (healed > 0) didHeal = true;
						SendSpellEffectsToLiving(living, didHeal);
						if (didHeal && living is GamePlayer pl)
						{
							if(player == pl)
								pl.Out.SendMessage("You heal yourself for " + healed + " hit points", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);	
							else
								pl.Out.SendMessage("You heal " + player.Name + " for " + healed + " hit points", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
						}
							
					}
						
				}
			}
			else
			{
				int healAmount = living.MaxHealth;
				int healed = living.ChangeHealth(living, eHealthChangeType.Spell, healAmount);
				if (healed > 0) didHeal = true;
				SendSpellEffectsToLiving(living, didHeal);
				if(didHeal && living is GamePlayer pla) pla.Out.SendMessage("You heal yourself for " + healed + " hit points", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				
				
			}
			
			if(didHeal) DisableSkill(living);
			
		}

        private async void SendSpellEffectsToLiving(GameLiving living, bool didHeal)
        {
	        SendCasterSpellEffectAndCastMessage(living, 10535, didHeal);
	        await Task.Delay(2000);
	        SendCasterSpellEffect(living, 3011, didHeal);
        }

		public override int GetReUseDelay(int level)
		{
			return 1800;	// 900 = 15 min / 1800 = 30 min
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("Value: 100%");
			list.Add("Target: Group");
			list.Add("Casting time: instant");
		}
	}
}