/*
 *
 * Atlas -  Summon Siege Catapult
 *
 */

using System.Collections.Generic;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonSiegeCatapult")]
    public class SummonSiegeCatapult : SpellHandler
    {
	    public SummonSiegeCatapult(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override bool StartSpell(GameLiving target)
        {
            if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon)
            {
			    MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
			    return false;
		    }

            //Only allow one treb/catapult in the radius
            ushort catapultSummonRadius = 500;
			foreach (GameNPC npc in Caster.CurrentRegion.GetNPCsInRadius(Caster.X, Caster.Y, Caster.Z, catapultSummonRadius, false, false))
			{
				if (npc is GameSiegeCatapult)
				{
					MessageToCaster("You are too close to another trebuchet or catapult and cannot summon here!", PacketHandler.eChatType.CT_SpellResisted);
                    return false;
				}
			}

            return base.StartSpell(target);
        }
        
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
            cat.Heading = Caster.Heading;
            cat.CurrentRegion = Caster.CurrentRegion;
            cat.Model = 0xA26;
            cat.Level = 3;
            cat.Name = "catapult";
            cat.Realm = Caster.Realm;
            cat.AddToWorld();
            if(Caster is GamePlayer player)
                cat.TakeControl(player);
            
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
