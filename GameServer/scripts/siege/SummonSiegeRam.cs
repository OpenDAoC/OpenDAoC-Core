/*
 *
 * Atlas -  Summon Siege Ram
 *
 */

using System.Collections.Generic;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonSiegeRam")]
    public class SummonSiegeRam : SpellHandler
    {
	    public SummonSiegeRam(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
	    public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            base.ApplyEffectOnTarget(target, effectiveness);

            GameSiegeRam ram = new GameSiegeRam();
            ram.X = Caster.X;
            ram.Y = Caster.Y;
            ram.Z = Caster.Z;
            ram.CurrentRegion = Caster.CurrentRegion;
            ram.Model = 2600;
            ram.Level = 1;
            ram.Name = "light siege ram";
            ram.Realm = Caster.Realm;
            ram.AddToWorld();
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!Caster.CurrentZone.IsRvR || Caster.CurrentRegion.IsDungeon)
            {
                MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(string.Format("  {0}", Spell.Description));

				return list;
			}
		}
    }
}
