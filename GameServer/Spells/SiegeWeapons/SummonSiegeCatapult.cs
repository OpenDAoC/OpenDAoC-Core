using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Keeps;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells;

[SpellHandler("SummonSiegeCatapult")]
public class SummonSiegeCatapult : SpellHandler
{
    public SummonSiegeCatapult(GameLiving caster, Spell spell, SpellLine line)
        : base(caster, spell, line) { }

    public override bool StartSpell(GameLiving target)
    {
        if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon)
        {
		    MessageToCaster("You cannot use siege weapons here!", EChatType.CT_SpellResisted);
		    return false;
	    }
        
        foreach (AArea area in Caster.CurrentAreas)
        {
            if (area is KeepArea)
            {
	            if (((KeepArea)area).Keep.IsPortalKeep)
	            {
		            MessageToCaster("You cannot use siege weapons here (PK)!", EChatType.CT_SpellResisted);
		            return false;
	            }
            }
        }

        //Only allow one treb/catapult in the radius
        ushort catapultSummonRadius = 500;
		foreach (GameNpc npc in Caster.GetNPCsInRadius(catapultSummonRadius))
		{
			if (npc is GameSiegeCatapult)
			{
				MessageToCaster("You are too close to another trebuchet or catapult and cannot summon here!", EChatType.CT_SpellResisted);
                return false;
			}
		}

        return base.StartSpell(target);
    }
    
    public override void ApplyEffectOnTarget(GameLiving target)
    {
        
        if (!Caster.CurrentZone.IsOF || Caster.CurrentRegion.IsDungeon){
	        MessageToCaster("You cannot use siege weapons here!", EChatType.CT_SpellResisted);
	        return;
        }
        
        base.ApplyEffectOnTarget(target);
        
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
	        MessageToCaster("You cannot use siege weapons here!", EChatType.CT_SpellResisted);
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