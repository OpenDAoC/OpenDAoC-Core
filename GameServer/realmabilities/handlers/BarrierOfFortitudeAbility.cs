using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.Effects;
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
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
				return;

			GamePlayer player = living as GamePlayer;
			IEnumerable<GamePlayer> playersInGroup;
			bool success;

			SendCastMessage(player);

			if (player.Group != null)
				playersInGroup = player.Group.GetPlayersInTheGroup().Where(x => x.IsAlive && player.IsWithinRadius(player, m_range));
			else
				playersInGroup = new List<GamePlayer>() { player };

			foreach (GamePlayer playerInGroup in playersInGroup)
			{
				if (!playerInGroup.IsAlive)
					continue;

				success = !playerInGroup.TempProperties.getProperty(BofBaSb, false);

				SendCasterSpellEffect(playerInGroup, 7009, success);
				SendCasterSpellEffect(playerInGroup, 1486, success);
				SendCasterSpellEffect(playerInGroup, 10535, success);
				
				if (success)
					new BunkerOfFaithECSEffect(new ECSGameEffectInitParams(playerInGroup, m_duration, GetAbsorbAmount(), CreateSpell(player)));
			}

			DisableSkill(player);
		}

		public virtual SpellHandler CreateSpell(GameLiving caster)
		{
			m_dbspell = new DBSpell();
			m_dbspell.Name = "Bunker Of Faith";
			m_dbspell.Icon = 7132;
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
