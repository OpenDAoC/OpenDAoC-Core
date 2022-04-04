/*
 *
 * Atlas -  Summon Siege Catapult
 *
 */

using System.Collections.Generic;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonSiegeCatapult")]
    public class SummonSiegeCatapult : SpellHandler
    {
	    public SummonSiegeCatapult(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
	    public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
	        
	        if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon){
		        MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
		        return;
	        }
	        
            base.ApplyEffectOnTarget(target, effectiveness);
            
            GameSiegeCatapult cat = new GameSiegeCatapult();
            cat.X = Caster.X;
            cat.Y = Caster.Y;
            cat.Z = Caster.Z;
            cat.CurrentRegion = Caster.CurrentRegion;
            cat.Model = 0xA26;
            cat.Level = 3;
            cat.Name = "catapult";
            cat.Realm = Caster.Realm;
            cat.AddToWorld();
            
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
	        if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon){
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
