/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// All stats debuff spell handler
    /// </summary>
    [SpellHandler("AllStatsPercentDebuff")]
	public class AllStatsPercentDebuff : SpellHandler
	{
        protected int StrDebuff = 0;
        protected int DexDebuff = 0;
        protected int ConDebuff = 0;
        protected int EmpDebuff = 0;
        protected int QuiDebuff = 0;
        protected int IntDebuff = 0;
        protected int ChaDebuff = 0;
        protected int PieDebuff = 0;

		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect); 
			//effect.Owner.DebuffCategory[(int)eProperty.Dexterity] += (int)m_spell.Value;
            double percentValue = (m_spell.Value) / 100;
            StrDebuff = (int)((double)effect.Owner.GetModified(EProperty.Strength) * percentValue);
            DexDebuff = (int)((double)effect.Owner.GetModified(EProperty.Dexterity) * percentValue);
            ConDebuff = (int)((double)effect.Owner.GetModified(EProperty.Constitution) * percentValue);
            EmpDebuff = (int)((double)effect.Owner.GetModified(EProperty.Empathy) * percentValue);
            QuiDebuff = (int)((double)effect.Owner.GetModified(EProperty.Quickness) * percentValue);
            IntDebuff = (int)((double)effect.Owner.GetModified(EProperty.Intelligence) * percentValue);
            ChaDebuff = (int)((double)effect.Owner.GetModified(EProperty.Charisma) * percentValue);
            PieDebuff = (int)((double)effect.Owner.GetModified(EProperty.Piety) * percentValue);
            

            effect.Owner.DebuffCategory[(int)EProperty.Dexterity] += DexDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Strength] += StrDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Constitution] += ConDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Piety] += PieDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Empathy] += EmpDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Quickness] += QuiDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Intelligence] += IntDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Charisma] += ChaDebuff;

			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Out.SendCharStatsUpdate();
				player.UpdateEncumberance();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
			}
		}
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
            double percentValue = (m_spell.Value) / 100;

            effect.Owner.DebuffCategory[(int)EProperty.Dexterity] -= DexDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Strength] -= StrDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Constitution] -= ConDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Piety] -= PieDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Empathy] -= EmpDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Quickness] -= QuiDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Intelligence] -= IntDebuff;
            effect.Owner.DebuffCategory[(int)EProperty.Charisma] -= ChaDebuff;

			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Out.SendCharStatsUpdate();
				player.UpdateEncumberance();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
			}
			return base.OnEffectExpires(effect, noMessages);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);
			if (target.Realm == 0 || Caster.Realm == 0)
			{
				target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
			}
			else
			{
				target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
			}
			if (target is GameNPC)
			{
				IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
				if (aggroBrain != null)
					aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
			}
		}
        public AllStatsPercentDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
