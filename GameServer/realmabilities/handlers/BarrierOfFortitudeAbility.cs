using System;
using System.Reflection;
using System.Collections;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
	public class BarrierOfFortitudeAbility : TimedRealmAbility
	{
		public BarrierOfFortitudeAbility(DBAbility dba, int level) : base(dba, level) { }

        public const string BofBaSb = "RA_DAMAGE_DECREASE";

		int m_range = 1500;
		int m_duration = 30000; // 30s

		public override void Execute(GameLiving living)
		{
			GamePlayer player = living as GamePlayer;
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			/// [Atlas - Takii] We don't want this "does not stack" functionality in OF.
// 			if (player.TempProperties.getProperty(BofBaSb, false))
// 			{
// 				player.Out.SendMessage("You already an effect of that type!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
// 				return;
// 			}

			DisableSkill(living);
			ArrayList targets = new ArrayList();
			if (player.Group == null)
				targets.Add(player);
			else
			{
				foreach (GamePlayer grpplayer in player.Group.GetPlayersInTheGroup())
				{
					if (player.IsWithinRadius(grpplayer, m_range ) && grpplayer.IsAlive)
						targets.Add(grpplayer);
				}
			}
			bool success;
			foreach (GamePlayer target in targets)
			{
				//send spelleffect
				if (!target.IsAlive) continue;
				success = !target.TempProperties.getProperty(BofBaSb, false);
				foreach (GamePlayer visPlayer in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					SendCasterSpellEffectAndCastMessage(player, 7009, success);
					SendCasterSpellEffect(player, 1486, success);
					SendCasterSpellEffect(player, 10535, success);
				}
				
					
				if (success)
				{
					BunkerOfFaithECSEffect eff = null;
					if (target != null)
						 eff = new BunkerOfFaithECSEffect(new ECSGameEffectInitParams(target, m_duration, GetAbsorbAmount(), CreateSpell(player)));
					Console.WriteLine($"Effect {eff}");
				}
			}
		}
		public virtual SpellHandler CreateSpell(GameLiving caster)
		{
			m_dbspell = new DBSpell();
			m_dbspell.Name = "Bunker Of Faith";
			m_dbspell.Icon = 31119;
			m_dbspell.ClientEffect = 4242;
			m_dbspell.Damage = 0;
			m_dbspell.DamageType = 0;
			m_dbspell.Target = "Group";
			m_dbspell.Radius = 0;
			m_dbspell.Type = eSpellType.ArmorAbsorptionBuff.ToString();
			m_dbspell.Value = 50;
			m_dbspell.Duration = 30;
			m_dbspell.Pulse = 0;
			m_dbspell.PulsePower = 0;
			m_dbspell.Power = 0;
			m_dbspell.CastTime = 0;
			m_dbspell.EffectGroup = 0;
			m_dbspell.Frequency = 0;
			m_dbspell.Range = 1500;
			m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
			m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
			return new SpellHandler(caster, m_spell, m_spellline);
		}
    
		private DBSpell m_dbspell;
		private Spell m_spell = null;
		private SpellLine m_spellline;
		
		private byte CastSuccess(bool success)
		{
			if (success)
				return 1;
			else
				return 0;
		}
		public override int GetReUseDelay(int level)
		{
			return 600;
		}

        protected virtual int GetAbsorbAmount()
        {
			if (ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
            {
                switch (Level)
                {
                    case 1: return 10;
                    case 2: return 15;
                    case 3: return 20;
                    case 4: return 30;
                    case 5: return 40;
                }
            }
            else
            {
                switch (Level)
                {
                    case 1: return 10;
                    case 2: return 20;
                    case 3: return 40;
                }
            }
			return 0;
        }
    }
}
