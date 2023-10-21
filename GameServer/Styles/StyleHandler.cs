using System.Collections.Generic;
using Core.GS.Packets.Clients;
using Core.GS.Styles;

namespace Core.GS.Spells
{
	[SpellHandler("StyleHandler")]
	public class StyleHandler : SpellHandler
	{
		public StyleHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(Spell.Description);

				GamePlayer player = Caster as GamePlayer;

				if (player != null)
				{
					list.Add(" ");

					Style style = SkillBase.GetStyleByID((int)Spell.Value, 0);
					if (style == null)
					{
						style = SkillBase.GetStyleByID((int)Spell.Value, player.PlayerClass.ID);
					}

					if (style != null)
					{
						DetailDisplayHandler.WriteStyleInfo(list, style, player.Client);
					}
					else
					{
						list.Add("Style not found.");
					}
				}

				return list;
			}
		}

	}


}

