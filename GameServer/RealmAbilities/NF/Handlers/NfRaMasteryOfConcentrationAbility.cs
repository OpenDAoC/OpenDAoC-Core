using System;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
	public class NfRaMasteryOfConcentrationAbility : TimedRealmAbility
	{
        public NfRaMasteryOfConcentrationAbility(DbAbility dba, int level) : base(dba, level) { }
		public const Int32 Duration = 30 * 1000;

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer caster = living as GamePlayer;

			if (caster == null)
				return;

			NfRaMasteryOfConcentrationEffect MoCEffect = caster.EffectList.GetOfType<NfRaMasteryOfConcentrationEffect>();
			if (MoCEffect != null)
			{
				MoCEffect.Cancel(false);
				return;
			}
			
			// Check for the RA5L on the Sorceror: he cannot cast MoC when the other is up
			NfRaShieldOfImmunityEffect ra5l = caster.EffectList.GetOfType<NfRaShieldOfImmunityEffect>();
			if (ra5l != null)
			{
				caster.Out.SendMessage("You cannot currently use this ability", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				return;
			}
			
			SendCasterSpellEffectAndCastMessage(living, 7007, true);
			foreach (GamePlayer player in caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{

                if ( caster.IsWithinRadius( player, WorldMgr.INFO_DISTANCE ) )
				{
					if (player == caster)
					{
						player.MessageToSelf("You cast " + this.Name + "!", EChatType.CT_Spell);
						player.MessageToSelf("You become steadier in your casting abilities!", EChatType.CT_Spell);
					}
					else
					{
						player.MessageFromArea(caster, caster.Name + " casts a spell!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
						player.Out.SendMessage(caster.Name + "'s castings have perfect poise!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
				}
			}

			DisableSkill(living);

			new NfRaMasteryOfConcentrationEffect(Duration).Start(caster);
		}
        public override int GetReUseDelay(int level)
        {
            return 600;
        }
        
        public virtual int GetAmountForLevel(int level)
		{
        	if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
        	{
        		switch(level)
        		{
        			case 1: return 25;
        			case 2: return 35;
        			case 3: return 50;
        			case 4: return 60;
        			case 5: return 75;
        		}
        	}
        	else
        	{
         		switch(level)
        		{
        			case 1: return 25;
        			case 2: return 50;
        			case 3: return 75;
        		}       		
        	}
        	return 25;
		}
	}
}
