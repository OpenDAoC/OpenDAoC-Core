using System;
using DOL.Database;
using DOL.Events;

namespace DOL.GS.Spells
{
    /// <summary>
    /// NOTE: PLEASE CHECK YOUR SPELL ID FOR JAVELIN OR CREATE YOUR OWN ITEM
    /// </summary>
    [SpellHandler(eSpellType.GoldenSpearJavelin)]
    public class GoldenSpearJavelin : SummonItemSpellHandler
    {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private DbItemTemplate _artefJavelin;

        public GoldenSpearJavelin(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            _artefJavelin = GameServer.Database.FindObjectByKey<DbItemTemplate>("Artef_Javelin") ?? Javelin;
            items.Add (GameInventoryItem.Create(_artefJavelin));
        }

        private DbItemTemplate Javelin
        {
            get
            {
                _artefJavelin = (DbItemTemplate) GameServer.Database.FindObjectByKey<DbItemTemplate>("Artef_Javelin");
                if(_artefJavelin == null)
                {
                    if(log.IsWarnEnabled) log.Warn("Could not find Artef_Javelin, loading it ...");
                    _artefJavelin = new DbItemTemplate();
                    _artefJavelin.Id_nb = "Artef_Javelin";
                    _artefJavelin.Name = "Golden Javelin";
                    _artefJavelin.Level = 50;
                    _artefJavelin.MaxDurability = 50000;
                    _artefJavelin.MaxCondition = 50000;
                    _artefJavelin.Quality = 100;
                    _artefJavelin.Object_Type = (int) eObjectType.Magical;
                    _artefJavelin.Item_Type = 41;
                    _artefJavelin.Model = 23;
                    _artefJavelin.IsPickable = false;
                    _artefJavelin.IsDropable = false;
                    _artefJavelin.CanDropAsLoot = false;
                    _artefJavelin.IsTradable = false;
                    _artefJavelin.MaxCount = 1;
                    _artefJavelin.PackSize = 1;
                    _artefJavelin.Charges = 5;
                    _artefJavelin.MaxCharges = 5;
                    _artefJavelin.SpellID = 38076;
                }
                return _artefJavelin;
            }
        }

        public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);
            GameEventMgr.AddHandler(Caster, GamePlayerEvent.Quit, OnPlayerLeft);
        }

        private static void OnPlayerLeft(DOLEvent e,object sender,EventArgs arguments)
        {
            if(!(sender is GamePlayer)) return;
            GamePlayer player = sender as GamePlayer;
            lock (player.Inventory.Lock)
            {
                var items = player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
                foreach(DbInventoryItem invItem in items)
                {
                    if(invItem.Id_nb.Equals("Artef_Javelin"))
                    {
                        player.Inventory.RemoveItem(invItem);
                    }
                }
            }
            GameEventMgr.RemoveHandler(sender, GamePlayerEvent.Quit, OnPlayerLeft);
        }
    }
}
