using System;
using System.Collections;
using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Effects;
using DOL.Events;
using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
	public class TheEmptyMindAbility : TimedRealmAbility
	{
		public TheEmptyMindAbility(DBAbility dba, int level) : base(dba, level) { }

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			foreach (GamePlayer t_player in living.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
			{
				if (t_player == living && living is GamePlayer)
				{
					(living as GamePlayer).Out.SendMessage("You clear your mind and become more resistant to magic damage!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
				else
				{
					t_player.Out.SendMessage(living.Name + " casts a spell!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
			}

            int effectiveness = GetEffectiveness();

			//new TheEmptyMindEffect(effectiveness, GetDuration()).Start(living);
			new StatBuffECSEffect(new ECSGameEffectInitParams(living, 30000, 1, CreateSpell(living)));
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 600;
		}

        protected virtual int GetDuration()
        {
            return 45000;
        }

        protected virtual int GetEffectiveness()
        {
			if (ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
            {
                switch (Level)
                {
                    case 1: return 10;
                    case 2: return 15;
                    case 3: return 20;
                    case 4: return 25;
                    case 5: return 30;
                    default: return 0;
                }
            }
            else
            {
                switch (Level)
                {
                    case 1: return 0;
                    case 2: return 20;
                    case 3: return 30;
                    default: return 0;
                }
            }
        }
        
        public virtual SpellHandler CreateSpell(GameLiving caster)
        {
	        DBSpell dbspell = new DBSpell();
	        dbspell.Name = "The Empty Mind";
	        dbspell.Icon = 4232;
	        dbspell.ClientEffect = 4232;
	        dbspell.Damage = 0;
	        dbspell.DamageType = 0;
	        dbspell.Target = "Self";
	        dbspell.Radius = 0;
	        dbspell.Type = eSpellType.AllMagicResistBuff.ToString();
	        dbspell.Value = GetEffectiveness();
	        dbspell.Duration = 30;
	        dbspell.Pulse = 0;
	        dbspell.PulsePower = 0;
	        dbspell.Power = 0;
	        dbspell.CastTime = 0;
	        dbspell.EffectGroup = 0;
	        dbspell.Frequency = 0;
	        dbspell.Range = 1500;
	        Spell spell = new Spell(dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
	        SpellLine line = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
	        return new SpellHandler(caster, spell, line);
        }
    }
}
