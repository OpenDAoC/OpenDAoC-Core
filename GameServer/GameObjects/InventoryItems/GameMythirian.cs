﻿using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using log4net;

namespace DOL.GS
{
	public class GameMythirian : GameInventoryItem
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private GameMythirian() { }

		public GameMythirian(DbItemTemplate template)
			: base(template)
		{
		}

		public GameMythirian(DbItemUnique template)
			: base(template)
		{
		}

		public GameMythirian(DbInventoryItem item)
			: base(item)
		{
		}

		public override bool CanEquip(GamePlayer player)
		{
			if (base.CanEquip(player))
			{
				if (Type_Damage <= player.ChampionLevel)
				{
					return true;
				}
				player.Out.SendMessage("You do not meet the Champion Level requirement to equip this item.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			return false;
		}

		#region Overrides

		public override void OnEquipped(GamePlayer player)
		{
			if (this.Name.ToLower().Contains("ektaktos"))
			{
				player.CanBreathUnderWater = true;
				player.Out.SendMessage("You find yourself able to breathe water like air!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			base.OnEquipped(player);
		}

		public override void OnUnEquipped(GamePlayer player)
		{
			if (this.Name.ToLower().Contains("ektaktos") && SpellHelper.FindEffectOnTarget(player, typeof(WaterBreathingSpell)) == null)
			{
				player.CanBreathUnderWater = false;
				player.Out.SendMessage("With a gulp and a gasp you realize that you are unable to breathe underwater any longer!", EChatType.CT_SpellExpires, EChatLoc.CL_SystemWindow);
			}
			base.OnUnEquipped(player);
		}
		#endregion

	}
}
