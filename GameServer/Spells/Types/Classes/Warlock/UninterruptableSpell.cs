using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Spells
{
    [SpellHandler("Uninterruptable")]
	public class UninterruptableSpell : PrimerSpell
	{
        public static string WARLOCK_UNINTERRUPTABLE_SPELL = "WARLOCK_UNINTERRUPTABLE_SPELL";

       	public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget)) return false;
            GameSpellEffect RangeSpell = SpellHandler.FindEffectOnTarget(Caster, "Range");
  			if(RangeSpell != null) { MessageToCaster("You already preparing a Range spell", EChatType.CT_System); return false; }
            GameSpellEffect PowerlessSpell = SpellHandler.FindEffectOnTarget(Caster, "Powerless");
  			if(PowerlessSpell != null) { MessageToCaster("You already preparing a Powerless spell", EChatType.CT_System); return false; }
            GameSpellEffect UninterruptableSpell = SpellHandler.FindEffectOnTarget(Caster, "Uninterruptable");
            if (UninterruptableSpell != null) { MessageToCaster("You must finish casting Uninterruptable before you can cast it again", EChatType.CT_System); return false; }
            return true;
		}
        public override void FinishSpellCast(GameLiving target)
        {
            Caster.TempProperties.SetProperty(WARLOCK_UNINTERRUPTABLE_SPELL, Spell);

            base.FinishSpellCast(target);


        }
		/// <summary>
        /// Calculates the power to cast the spell
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public override int PowerCost(GameLiving target)
        { 
            double basepower = m_spell.Power; //<== defined a basevar first then modified this base-var to tell %-costs from absolut-costs

            // percent of maxPower if less than zero
			if (basepower < 0)
			{
				if (Caster is GamePlayer && ((GamePlayer)Caster).PlayerClass.ManaStat != EStat.UNDEFINED)
				{
					GamePlayer player = Caster as GamePlayer;
					basepower = player.CalculateMaxMana(player.Level, player.GetBaseStat(player.PlayerClass.ManaStat)) * basepower * -0.01;
				}
				else
				{
					basepower = Caster.MaxMana * basepower * -0.01;
				}
			}
            return (int) basepower;
        }


		public override bool CasterIsAttacked(GameLiving attacker)
		{
			return false;
		}
        #region Devle Info
        public override IList<string> DelveInfo
        {
            get
            {
            	var list = new List<string>(16);

                //Name
                list.Add("Name: " + Spell.Name);
                list.Add("");

                //Description
                list.Add("Description: " + Spell.Description);
                list.Add("");

                //SpellType
                list.Add("Type: " + Spell.SpellType);

                double value = 100 - Spell.Value;// 100 - 60 = 40% reduction
                //Value
                if (Spell.Value != 0)
                    list.Add("Spell effectiveness reduced: " + value + "%");

                //Target
                list.Add("Target: " + Spell.Target);

                //Duration
                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add("Duration: Permanent.");
                else if (Spell.Duration > 60000)
                    list.Add(string.Format("Duration: {0}:{1} min", Spell.Duration / 60000, (Spell.Duration % 60000 / 1000).ToString("00")));
                else if (Spell.Duration != 0)

                //Cost
                list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));

                //Cast
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
                return list;
            }
        #endregion
        }
		// constructor
		public UninterruptableSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
