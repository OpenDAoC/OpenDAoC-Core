﻿using System.Collections.Generic;
using DOL.GS.Keeps;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonSiegeBallista")]
    public class SummonSiegeBallista : SpellHandler
    {
	    public SummonSiegeBallista(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }


        public override bool StartSpell(GameLiving target)
        {
            if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon)
            {
			    MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
			    return false;
		    }
            
            foreach (AbstractArea area in Caster.CurrentAreas)
            {
	            if (area is KeepArea)
	            {
		            if (((KeepArea)area).Keep.IsPortalKeep)
		            {
			            MessageToCaster("You cannot use siege weapons here (PK)!", PacketHandler.eChatType.CT_SpellResisted);
			            return false;
		            }
	            }
            }

            return base.StartSpell(target);
        }    
	    public override void ApplyEffectOnTarget(GameLiving target)
        {
	        
	        if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon){
		        MessageToCaster("You cannot use siege weapons here!", PacketHandler.eChatType.CT_SpellResisted);
		        return;
	        }
	        
            base.ApplyEffectOnTarget(target);
            
            GameSiegeBallista bal = new GameSiegeBallista();
            bal.X = Caster.X;
            bal.Y = Caster.Y;
            bal.Z = Caster.Z;
            bal.Heading = Caster.Heading;
            bal.CurrentRegion = Caster.CurrentRegion;
            bal.Model = 0x0A55;
            bal.Level = 3;
            bal.Name = "field ballista";
            bal.Realm = Caster.Realm;
            bal.AddToWorld();
            if(Caster is GamePlayer player)
                bal.TakeControl(player);
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
