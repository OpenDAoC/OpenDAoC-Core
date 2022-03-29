/*
 *
 * Atlas -  Summon Siege Trebuchet
 *
 */

using System.Collections.Generic;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonSiegeTrebuchet")]
    public class SummonSiegeTrebuchet : SpellHandler
    {
	    public SummonSiegeTrebuchet(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
	    public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            base.ApplyEffectOnTarget(target, effectiveness);
            
            GameSiegeTrebuchet tre = new GameSiegeTrebuchet();
            tre.X = Caster.X;
            tre.Y = Caster.Y;
            tre.Z = Caster.Z;
            tre.CurrentRegion = Caster.CurrentRegion;
            tre.Model = 0xA2E;
            tre.Level = 3;
            tre.Name = "trebuchet";
            tre.Realm = Caster.Realm;
            tre.AddToWorld();
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
