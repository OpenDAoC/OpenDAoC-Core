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

using System;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Zo' Arkat summoning
    /// </summary>
    [SpellHandlerAttribute("ZoSummon")]
    public class BracerOfZo : SpellHandler
    {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override bool IsUnPurgeAble { get { return true; } }
		
		public ZoarkatPet[] Demons = new ZoarkatPet[3];

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			Console.WriteLine($"just before demon summon");
			new DemonSummonECSEffect(new ECSGameEffectInitParams(target, Spell.Duration, Effectiveness, this));
			Console.WriteLine($"just after demon summon");
		}

		public override int CalculateSpellResistChance(GameLiving target) { return 0; }
        public BracerOfZo(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    
    [SpellHandlerAttribute("Bedazzlement")]
    public class ZoDebuffSpellHandler : DualStatDebuff
    {
		public override eProperty Property1 { get { return eProperty.FumbleChance; } }
		public override eProperty Property2 { get { return eProperty.SpellFumbleChance; } }

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);
			target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}
		
        public ZoDebuffSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}

namespace DOL.GS
{
    public class ZoarkatPet : GameSummonedPet
	{
		public override int MaxHealth
        {
            get { return Level*10; }
        }
		public override void OnAttackedByEnemy(AttackData ad) { }
		public ZoarkatPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
	}
}
