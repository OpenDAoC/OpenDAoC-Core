using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS.Keeps
{
	/// <summary>
	/// Class to manage the clothing of the guards
	/// </summary>
	public class ClothingMgr
	{
		//Declare the inventory template
		#region Albion
		public static GameNpcInventoryTemplate Albion_Archer = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Albion_Caster = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Albion_Fighter = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Albion_Commander = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Albion_Healer = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Albion_Stealther = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Albion_Lord = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Albion_Merchant = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Relic_Albion_Lord = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Albion_FighterPK = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Albion_ArcherPK = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Albion_CasterPK = new GameNpcInventoryTemplate();
		#endregion
		#region Midgard
		public static GameNpcInventoryTemplate Midgard_Archer = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Midgard_Caster = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Midgard_Fighter = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Midgard_Commander = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Midgard_Merchant = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Midgard_Healer = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Midgard_Hastener = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Midgard_Stealther = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Midgard_Lord = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Relic_Midgard_Lord = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Midgard_FighterPK = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Midgard_ArcherPK = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Midgard_CasterPK = new GameNpcInventoryTemplate();
		#endregion
		#region Hibernia
		public static GameNpcInventoryTemplate Hibernia_Archer = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Hibernia_Caster = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Hibernia_Fighter = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Hibernia_Commander = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Hibernia_Merchant = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Hibernia_Healer = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Hibernia_Stealther = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Hibernia_Lord = new GameNpcInventoryTemplate();
        public static GameNpcInventoryTemplate Relic_Hibernia_Lord = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Hibernia_FighterPK = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Hibernia_ArcherPK = new GameNpcInventoryTemplate();
		public static GameNpcInventoryTemplate Hibernia_CasterPK = new GameNpcInventoryTemplate();
		#endregion

		/// <summary>
		/// Method to load all the templates into memory
		/// </summary>
		public static void LoadTemplates()
		{
			#region Albion
            #region Archer
            if (!Albion_Archer.LoadFromDatabase("albion_archer"))
            {
                Albion_Archer.AddNPCEquipment(EInventorySlot.Cloak, 92);
                Albion_Archer.AddNPCEquipment(EInventorySlot.TorsoArmor, 728);
                Albion_Archer.AddNPCEquipment(EInventorySlot.LegsArmor, 663);
                Albion_Archer.AddNPCEquipment(EInventorySlot.ArmsArmor, 664);
                Albion_Archer.AddNPCEquipment(EInventorySlot.HandsArmor, 665);
                Albion_Archer.AddNPCEquipment(EInventorySlot.FeetArmor, 666);
                Albion_Archer.AddNPCEquipment(EInventorySlot.DistanceWeapon, 849);
                Albion_Archer.AddNPCEquipment(EInventorySlot.RightHandWeapon, 653);
                Albion_Archer.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 59);
                Albion_Archer = Albion_Archer.CloseTemplate();
                Albion_Archer.GetItem(EInventorySlot.DistanceWeapon).Hand = (int)EHandFlag.Two;
                Albion_Archer.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
            }
            #endregion
            #region Caster
            if (!Albion_Caster.LoadFromDatabase("albion_caster"))
            {
                Albion_Caster.AddNPCEquipment(EInventorySlot.Cloak, 92);
                Albion_Caster.AddNPCEquipment(EInventorySlot.TorsoArmor, 58);
                Albion_Caster.AddNPCEquipment(EInventorySlot.HandsArmor, 142);
                Albion_Caster.AddNPCEquipment(EInventorySlot.FeetArmor, 143);
                Albion_Caster.AddNPCEquipment(EInventorySlot.RightHandWeapon, 13);
                Albion_Caster.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1170);
                Albion_Caster = Albion_Caster.CloseTemplate();
                Albion_Caster.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Fighter
            if (!Albion_Fighter.LoadFromDatabase("albion_fighter"))
            {
                Albion_Fighter.AddNPCEquipment(EInventorySlot.Cloak, 92); 
                Albion_Fighter.AddNPCEquipment(EInventorySlot.TorsoArmor, 662);
                Albion_Fighter.AddNPCEquipment(EInventorySlot.LegsArmor, 663);
                Albion_Fighter.AddNPCEquipment(EInventorySlot.ArmsArmor, 664);
                Albion_Fighter.AddNPCEquipment(EInventorySlot.HandsArmor, 665);
                Albion_Fighter.AddNPCEquipment(EInventorySlot.FeetArmor, 666);
                Albion_Fighter.AddNPCEquipment(EInventorySlot.HeadArmor, 95);
                Albion_Fighter.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 60);
                Albion_Fighter.AddNPCEquipment(EInventorySlot.RightHandWeapon, 10);
                Albion_Fighter.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 649);
                Albion_Fighter = Albion_Fighter.CloseTemplate();
                Albion_Fighter.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Albion_Fighter.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Commander
            if (!Albion_Commander.LoadFromDatabase("albion_commander"))
            {
	            Albion_Commander.AddNPCEquipment(EInventorySlot.Cloak, 676);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.TorsoArmor, 662);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.LegsArmor, 663);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.ArmsArmor, 664);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.HandsArmor, 665);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.FeetArmor, 666);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.HeadArmor, 95);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 60);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.RightHandWeapon, 10);
	            Albion_Commander.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 649);
	            Albion_Commander = Albion_Commander.CloseTemplate();
	            Albion_Commander.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
	            Albion_Commander.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Merchant
            if (!Albion_Merchant.LoadFromDatabase("albion_guard_merchant"))
            {
	            
	            Albion_Merchant.AddNPCEquipment(EInventorySlot.TorsoArmor, 51, 0);
	            Albion_Merchant.AddNPCEquipment(EInventorySlot.LegsArmor, 52, 0);
	            Albion_Merchant.AddNPCEquipment(EInventorySlot.ArmsArmor, 53, 0);
	            Albion_Merchant.AddNPCEquipment(EInventorySlot.HandsArmor, 80, 0);
	            Albion_Merchant.AddNPCEquipment(EInventorySlot.FeetArmor, 54, 0);
	            Albion_Merchant.AddNPCEquipment(EInventorySlot.RightHandWeapon, 5);
	            Albion_Merchant = Albion_Merchant.CloseTemplate();
	            Albion_Merchant.GetItem(EInventorySlot.RightHandWeapon).Object_Type = (int) EObjectType.SlashingWeapon;
            }
            #endregion
            #region Relic Lord
            if (!Relic_Albion_Lord.LoadFromDatabase("relic_albion_lord"))
            {
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.Cloak, 676); //676
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.TorsoArmor, 662);
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.LegsArmor, 663);
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.ArmsArmor, 664);
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.HandsArmor, 665);
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.FeetArmor, 666);
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.HeadArmor, 95);
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 60);
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.RightHandWeapon, 10);
                Relic_Albion_Lord.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 649);
                Relic_Albion_Lord = Relic_Albion_Lord.CloseTemplate();
                Relic_Albion_Lord.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Relic_Albion_Lord.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Lord
            if (!Albion_Lord.LoadFromDatabase("albion_lord"))
            {
                Albion_Lord.AddNPCEquipment(EInventorySlot.Cloak, 676); //676
                Albion_Lord.AddNPCEquipment(EInventorySlot.TorsoArmor, 662);
                Albion_Lord.AddNPCEquipment(EInventorySlot.LegsArmor, 663);
                Albion_Lord.AddNPCEquipment(EInventorySlot.ArmsArmor, 664);
                Albion_Lord.AddNPCEquipment(EInventorySlot.HandsArmor, 665);
                Albion_Lord.AddNPCEquipment(EInventorySlot.FeetArmor, 666);
                Albion_Lord.AddNPCEquipment(EInventorySlot.HeadArmor, 95);
                Albion_Lord.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 60);
                Albion_Lord.AddNPCEquipment(EInventorySlot.RightHandWeapon, 10);
                Albion_Lord.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 649);
                Albion_Lord.AddNPCEquipment(EInventorySlot.DistanceWeapon, 132);
                Albion_Lord = Albion_Lord.CloseTemplate();
                Albion_Lord.GetItem(EInventorySlot.DistanceWeapon).Hand = (int)EHandFlag.Two;
                Albion_Lord.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Albion_Lord.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Healer
            if (!Albion_Healer.LoadFromDatabase("albion_healer"))
            {
                Albion_Healer.AddNPCEquipment(EInventorySlot.Cloak, 92);
                Albion_Healer.AddNPCEquipment(EInventorySlot.TorsoArmor, 713);
                Albion_Healer.AddNPCEquipment(EInventorySlot.LegsArmor, 663);
                Albion_Healer.AddNPCEquipment(EInventorySlot.ArmsArmor, 664);
                Albion_Healer.AddNPCEquipment(EInventorySlot.HandsArmor, 665);
                Albion_Healer.AddNPCEquipment(EInventorySlot.FeetArmor, 666);
                Albion_Healer.AddNPCEquipment(EInventorySlot.HeadArmor, 94);
                Albion_Healer.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 61);
                Albion_Healer.AddNPCEquipment(EInventorySlot.RightHandWeapon, 3282);
                Albion_Healer = Albion_Healer.CloseTemplate();
                Albion_Fighter.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
            }
            #endregion
            #region Stealther
            if (!Albion_Stealther.LoadFromDatabase("albion_stealther"))
            {
                Albion_Stealther.AddNPCEquipment(EInventorySlot.Cloak, 92);
                Albion_Stealther.AddNPCEquipment(EInventorySlot.TorsoArmor, 792);
                Albion_Stealther.AddNPCEquipment(EInventorySlot.LegsArmor, 663);
                Albion_Stealther.AddNPCEquipment(EInventorySlot.ArmsArmor, 664);
                Albion_Stealther.AddNPCEquipment(EInventorySlot.HandsArmor, 665);
                Albion_Stealther.AddNPCEquipment(EInventorySlot.FeetArmor, 666);
                Albion_Stealther.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 653);
                Albion_Stealther.AddNPCEquipment(EInventorySlot.RightHandWeapon, 653);
                Albion_Stealther.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 653);
                Albion_Stealther = Albion_Stealther.CloseTemplate();
                Albion_Stealther.GetItem(EInventorySlot.LeftHandWeapon).Hand = (int)EHandFlag.Left;
                Albion_Stealther.GetItem(EInventorySlot.LeftHandWeapon).SlotPosition = Slot.LEFTHAND;
            }
            #endregion
            #region PK
            //portal keep
			Albion_FighterPK.LoadFromDatabase("alb_fighter_pk");
			Albion_ArcherPK.LoadFromDatabase("alb_archer_pk");
			Albion_CasterPK.LoadFromDatabase("alb_caster_pk");
			#endregion
			#endregion
			#region Midgard
            #region Archer
            if (!Midgard_Archer.LoadFromDatabase("midgard_archer"))
            {
                Midgard_Archer.AddNPCEquipment(EInventorySlot.Cloak, 677);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.TorsoArmor, 668);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.LegsArmor, 2943);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.ArmsArmor, 2944);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.HandsArmor, 2945);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.FeetArmor, 2946);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.HeadArmor, 2874);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.DistanceWeapon, 1037);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 328);
                Midgard_Archer.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 59);
                Midgard_Archer = Midgard_Archer.CloseTemplate();
                Midgard_Archer.GetItem(EInventorySlot.DistanceWeapon).Hand = (int)EHandFlag.Two;
                Midgard_Archer.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
                Midgard_Archer.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
            }
            #endregion
            #region Caster
            if (!Midgard_Caster.LoadFromDatabase("midgard_caster"))
            {
                Midgard_Caster.AddNPCEquipment(EInventorySlot.Cloak, 677); 
                Midgard_Caster.AddNPCEquipment(EInventorySlot.TorsoArmor, 98);
                Midgard_Caster.AddNPCEquipment(EInventorySlot.HandsArmor, 142);
                Midgard_Caster.AddNPCEquipment(EInventorySlot.FeetArmor, 143);
                Midgard_Caster.AddNPCEquipment(EInventorySlot.RightHandWeapon, 13);
                Midgard_Caster.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 566);
                Midgard_Caster = Midgard_Caster.CloseTemplate();
                Midgard_Caster.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Fighter
            if (!Midgard_Fighter.LoadFromDatabase("midgard_fighter"))
            {
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.Cloak, 677); 
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.TorsoArmor, 668);
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.LegsArmor, 2943);
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.ArmsArmor, 2944);
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.HandsArmor, 2945);
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.FeetArmor, 2946);
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.HeadArmor, 2874);
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 60);
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.RightHandWeapon, 313);
                Midgard_Fighter.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 572);
                Midgard_Fighter = Midgard_Fighter.CloseTemplate();
                Midgard_Fighter.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Midgard_Fighter.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Commander
            if (!Midgard_Commander.LoadFromDatabase("midgard_commander"))
            {
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.Cloak, 677);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.TorsoArmor, 668);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.LegsArmor, 2943);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.ArmsArmor, 2944);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.HandsArmor, 2945);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.FeetArmor, 2946);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.HeadArmor, 2874);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 60);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.RightHandWeapon, 313);
	            Midgard_Commander.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 572);
	            Midgard_Commander = Midgard_Commander.CloseTemplate();
	            Midgard_Commander.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
	            Midgard_Commander.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Merchant
            if (!Midgard_Merchant.LoadFromDatabase("midgard_guard_merchant"))
            {
	            Midgard_Merchant.AddNPCEquipment(EInventorySlot.TorsoArmor, 250, 0);
	            Midgard_Merchant.AddNPCEquipment(EInventorySlot.LegsArmor, 251, 0);
	            Midgard_Merchant.AddNPCEquipment(EInventorySlot.ArmsArmor, 252, 0);
	            Midgard_Merchant.AddNPCEquipment(EInventorySlot.HandsArmor, 253, 0);
	            Midgard_Merchant.AddNPCEquipment(EInventorySlot.FeetArmor, 254, 0);
	            Midgard_Merchant.AddNPCEquipment(EInventorySlot.RightHandWeapon, 312);
	            Midgard_Merchant = Midgard_Merchant.CloseTemplate();
	            Midgard_Merchant.GetItem(EInventorySlot.RightHandWeapon).Object_Type = (int) EObjectType.Sword;
            }
            #endregion
            #region Lord
            if (!Midgard_Lord.LoadFromDatabase("midgard_lord"))
            {
                Midgard_Lord.AddNPCEquipment(EInventorySlot.Cloak, 677);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.TorsoArmor, 668);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.LegsArmor, 2943);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.ArmsArmor, 2944);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.HandsArmor, 2945);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.FeetArmor, 2946);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.HeadArmor, 2874);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 60);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.RightHandWeapon, 313);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 572);
                Midgard_Lord.AddNPCEquipment(EInventorySlot.DistanceWeapon, 564);
                Midgard_Lord = Midgard_Lord.CloseTemplate();
                Midgard_Lord.GetItem(EInventorySlot.DistanceWeapon).Hand = (int)EHandFlag.Two;
                Midgard_Lord.GetItem(EInventorySlot.DistanceWeapon).Object_Type = (int)EObjectType.Longbow;
                Midgard_Lord.GetItem(EInventorySlot.DistanceWeapon).SlotPosition = Slot.RANGED;
                Midgard_Lord.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Midgard_Lord.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Relic Lord
            if (!Relic_Midgard_Lord.LoadFromDatabase("relic_midgard_lord"))
            {
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.Cloak, 677);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.TorsoArmor, 668);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.LegsArmor, 2943);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.ArmsArmor, 2944);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.HandsArmor, 2945);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.FeetArmor, 2946);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.HeadArmor, 2874);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 60);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.RightHandWeapon, 313);
                Relic_Midgard_Lord.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 572);
                Relic_Midgard_Lord = Relic_Midgard_Lord.CloseTemplate();
                Relic_Midgard_Lord.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Relic_Midgard_Lord.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Healer
            if (!Midgard_Healer.LoadFromDatabase("midgard_healer"))
            {
                Midgard_Healer.AddNPCEquipment(EInventorySlot.Cloak, 677); 
                Midgard_Healer.AddNPCEquipment(EInventorySlot.TorsoArmor, 668);
                Midgard_Healer.AddNPCEquipment(EInventorySlot.LegsArmor, 2943);
                Midgard_Healer.AddNPCEquipment(EInventorySlot.ArmsArmor, 2944);
                Midgard_Healer.AddNPCEquipment(EInventorySlot.HandsArmor, 2945);
                Midgard_Healer.AddNPCEquipment(EInventorySlot.FeetArmor, 2946);
                Midgard_Healer.AddNPCEquipment(EInventorySlot.HeadArmor, 2874);
                Midgard_Healer.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 59);
                Midgard_Healer.AddNPCEquipment(EInventorySlot.RightHandWeapon, 3335);
                Midgard_Healer.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 3336);
                Midgard_Healer = Midgard_Healer.CloseTemplate();
                Midgard_Healer.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
            }
            #endregion
            #region Hastener
            if (!Midgard_Hastener.LoadFromDatabase("midgard_hastener"))
            {
                Midgard_Hastener.AddNPCEquipment(EInventorySlot.Cloak, 443, 43);
                Midgard_Hastener.AddNPCEquipment(EInventorySlot.TorsoArmor, 230);
                Midgard_Hastener.AddNPCEquipment(EInventorySlot.HandsArmor, 233);
                Midgard_Hastener.AddNPCEquipment(EInventorySlot.FeetArmor, 234);
                Midgard_Hastener.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 228);
                Midgard_Hastener = Midgard_Hastener.CloseTemplate();
                Midgard_Hastener.GetItem(EInventorySlot.LeftHandWeapon).Hand = (int)EHandFlag.Left;
                Midgard_Hastener.GetItem(EInventorySlot.LeftHandWeapon).SlotPosition = Slot.LEFTHAND;
            }
            #endregion
            #region Stealther
            if (!Midgard_Stealther.LoadFromDatabase("midgard_stealther"))
            {
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.Cloak, 677); 
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.TorsoArmor, 668);
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.LegsArmor, 2943);
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.ArmsArmor, 2944);
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.HandsArmor, 2945);
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.FeetArmor, 2946);
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.HeadArmor, 335);
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 573);
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.RightHandWeapon, 573);
                Midgard_Stealther.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 577);
                Midgard_Stealther = Midgard_Stealther.CloseTemplate();
                Midgard_Stealther.GetItem(EInventorySlot.LeftHandWeapon).Hand = (int)EHandFlag.Left;
                Albion_Stealther.GetItem(EInventorySlot.LeftHandWeapon).SlotPosition = Slot.LEFTHAND;
            }
            #endregion
			#region PK
			Midgard_FighterPK.LoadFromDatabase("mid_fighter_pk");
			Midgard_ArcherPK.LoadFromDatabase("mid_archer_pk");
			Midgard_CasterPK.LoadFromDatabase("mid_caster_pk");
			#endregion
			#endregion
			#region Hibernia
            #region Archer
            if (!Hibernia_Archer.LoadFromDatabase("hibernia_archer"))
            {
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.Cloak, 678);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.TorsoArmor, 667);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.LegsArmor, 989);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.ArmsArmor, 990);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.HandsArmor, 991);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.FeetArmor, 992);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.HeadArmor, 1207);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.DistanceWeapon, 919);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.RightHandWeapon, 643);
                Hibernia_Archer.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 643);
                Hibernia_Archer = Hibernia_Archer.CloseTemplate();
                Hibernia_Archer.GetItem(EInventorySlot.DistanceWeapon).Hand = (int)EHandFlag.Two;
                Hibernia_Archer.GetItem(EInventorySlot.DistanceWeapon).Object_Type = (int)EObjectType.RecurvedBow;
                Hibernia_Archer.GetItem(EInventorySlot.DistanceWeapon).SlotPosition = Slot.RANGED;
                Hibernia_Archer.GetItem(EInventorySlot.LeftHandWeapon).Hand = (int)EHandFlag.Left;
            }
            #endregion
            #region Caster
            if (!Hibernia_Caster.LoadFromDatabase("hibernia_caster"))
            {
                Hibernia_Caster.AddNPCEquipment(EInventorySlot.Cloak, 678);
                Hibernia_Caster.AddNPCEquipment(EInventorySlot.TorsoArmor, 97);
                Hibernia_Caster.AddNPCEquipment(EInventorySlot.HandsArmor, 142);
                Hibernia_Caster.AddNPCEquipment(EInventorySlot.FeetArmor, 143);
                Hibernia_Caster.AddNPCEquipment(EInventorySlot.RightHandWeapon, 13);
                Hibernia_Caster.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1176);
                Hibernia_Caster = Hibernia_Caster.CloseTemplate();
                Hibernia_Caster.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Fighter
            if (!Hibernia_Fighter.LoadFromDatabase("hibernia_fighter"))
            {
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.Cloak, 678);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.TorsoArmor, 667);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.LegsArmor, 989);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.ArmsArmor, 990);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.HandsArmor, 991);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.FeetArmor, 992);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.HeadArmor, 1207);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 79);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.RightHandWeapon, 897);
                Hibernia_Fighter.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 476);
                Hibernia_Fighter = Hibernia_Fighter.CloseTemplate();
                Hibernia_Fighter.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Hibernia_Fighter.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Commander
            if (!Hibernia_Commander.LoadFromDatabase("hibernia_commander"))
            {
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.Cloak, 678);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.TorsoArmor, 667);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.LegsArmor, 989);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.ArmsArmor, 990);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.HandsArmor, 991);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.FeetArmor, 992);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.HeadArmor, 1207);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 79);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.RightHandWeapon, 897);
	            Hibernia_Commander.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 476);
	            Hibernia_Commander = Hibernia_Commander.CloseTemplate();
	            Hibernia_Commander.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
	            Hibernia_Commander.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Merchant
            if (!Hibernia_Merchant.LoadFromDatabase("hibernia_guard_merchant"))
            {
	            Hibernia_Merchant.AddNPCEquipment(EInventorySlot.TorsoArmor, 363, 0);
	            Hibernia_Merchant.AddNPCEquipment(EInventorySlot.LegsArmor, 364, 0);
	            Hibernia_Merchant.AddNPCEquipment(EInventorySlot.ArmsArmor, 365, 0);
	            Hibernia_Merchant.AddNPCEquipment(EInventorySlot.HandsArmor, 366, 0);
	            Hibernia_Merchant.AddNPCEquipment(EInventorySlot.FeetArmor, 367, 0);
	            Hibernia_Merchant.AddNPCEquipment(EInventorySlot.RightHandWeapon, 447);
	            Hibernia_Merchant = Hibernia_Merchant.CloseTemplate();
	            Hibernia_Merchant.GetItem(EInventorySlot.RightHandWeapon).Object_Type = (int) EObjectType.Blades;
            }
            #endregion
            #region Lord
            if (!Hibernia_Lord.LoadFromDatabase("hibernia_lord"))
            {
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.Cloak, 678); 
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.TorsoArmor, 667);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.LegsArmor, 989);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.ArmsArmor, 990);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.HandsArmor, 991);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.FeetArmor, 992);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.HeadArmor, 1207);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 79);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.RightHandWeapon, 897);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 476);
                Hibernia_Lord.AddNPCEquipment(EInventorySlot.DistanceWeapon, 471);
                Hibernia_Lord = Hibernia_Lord.CloseTemplate();
                Hibernia_Lord.GetItem(EInventorySlot.DistanceWeapon).Hand = (int)EHandFlag.Two;
                Hibernia_Lord.GetItem(EInventorySlot.DistanceWeapon).Object_Type = (int)EObjectType.CompositeBow;
                Hibernia_Lord.GetItem(EInventorySlot.DistanceWeapon).SlotPosition = Slot.RANGED;
                Hibernia_Lord.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Hibernia_Lord.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Relic Lord
            if (!Relic_Hibernia_Lord.LoadFromDatabase("relic_hibernia_lord"))
            {
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.Cloak, 678);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.TorsoArmor, 667);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.LegsArmor, 989);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.ArmsArmor, 990);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.HandsArmor, 991);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.FeetArmor, 992);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.HeadArmor, 1207);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 79);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.RightHandWeapon, 897);
                Relic_Hibernia_Lord.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 476);
                Relic_Hibernia_Lord = Relic_Hibernia_Lord.CloseTemplate();
                Relic_Hibernia_Lord.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
                Relic_Hibernia_Lord.GetItem(EInventorySlot.TwoHandWeapon).Hand = (int)EHandFlag.Two;
            }
            #endregion
            #region Healer
            if (!Hibernia_Healer.LoadFromDatabase("hibernia_healer"))
            {
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.Cloak, 678);
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.TorsoArmor, 667);
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.LegsArmor, 989);
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.ArmsArmor, 990);
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.HandsArmor, 991);
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.FeetArmor, 992);
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.HeadArmor, 1207);
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 59);
                Hibernia_Healer.AddNPCEquipment(EInventorySlot.RightHandWeapon, 3247);
                Hibernia_Healer = Hibernia_Healer.CloseTemplate();
                Hibernia_Healer.GetItem(EInventorySlot.LeftHandWeapon).Object_Type = (int)EObjectType.Shield;
            }
            #endregion
            #region Stealther
            if (!Hibernia_Stealther.LoadFromDatabase("hibernia_stealther"))
            {
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.Cloak, 678);
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.TorsoArmor, 667);
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.LegsArmor, 989);
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.ArmsArmor, 990);
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.HandsArmor, 991);
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.FeetArmor, 992);
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 2685);
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.RightHandWeapon, 2685);
                Hibernia_Stealther.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 2687);
                Hibernia_Stealther = Hibernia_Stealther.CloseTemplate();
                Hibernia_Stealther.GetItem(EInventorySlot.LeftHandWeapon).Hand = (int)EHandFlag.Left;
                Albion_Stealther.GetItem(EInventorySlot.LeftHandWeapon).SlotPosition = Slot.LEFTHAND;
            }
            #endregion
            #region PK
			Hibernia_FighterPK.LoadFromDatabase("hib_fighter_pk");
			Hibernia_ArcherPK.LoadFromDatabase("hib_archer_pk");
			Hibernia_CasterPK.LoadFromDatabase("hib_caster_pk");
			#endregion
			#endregion
		}

		/// <summary>
		/// Method to equip a guard
		/// </summary>
		/// <param name="guard">The guard object</param>
		public static void EquipGuard(GameKeepGuard guard)
		{
			if(!ServerProperty.AUTOEQUIP_GUARDS_LOADED_FROM_DB && !guard.LoadedFromScript)
			{
				return;
			}
            if (guard is FrontierHastener || guard is GateKeeperIn || guard is GateKeeperOut)
            {
                switch (guard.Realm)
                {
                    case ERealm.None:
                    case ERealm.Albion:
                    case ERealm.Hibernia:
                    case ERealm.Midgard:
                        {
                            guard.Inventory = ClothingMgr.Midgard_Hastener.CloneTemplate();
                            break;
                        }
                }
            }

			switch (guard.ModelRealm)
			{
				case ERealm.None:
				case ERealm.Albion:
					{
						if (guard is GuardFighter)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Albion_FighterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Albion_Fighter.CloneTemplate();
						}
						else if (guard is GuardCommander)
							guard.Inventory = ClothingMgr.Albion_Commander.CloneTemplate();
						else if (guard is GuardFighterRelic)
                            guard.Inventory = ClothingMgr.Albion_FighterPK.CloneTemplate();
						else if (guard is GuardLord || guard is MissionMaster)
							guard.Inventory = ClothingMgr.Albion_Lord.CloneTemplate();
						else if (guard is GuardMerchant)
							guard.Inventory = ClothingMgr.Albion_Merchant.CloneTemplate();
						else if (guard is GuardCurrencyMerchant)
							guard.Inventory = ClothingMgr.Albion_Merchant.CloneTemplate();
						else if (guard is GuardHealer)
							guard.Inventory = ClothingMgr.Albion_Healer.CloneTemplate();
						else if (guard is GuardArcher)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Albion_ArcherPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Albion_Archer.CloneTemplate();
						}
						else if (guard is GuardCaster)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Albion_CasterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Albion_Caster.CloneTemplate();
						}
						else if (guard is GuardStealther)
							guard.Inventory = ClothingMgr.Albion_Stealther.CloneTemplate();
						break;
					}
				case ERealm.Midgard:
					{
						if (guard is GuardFighter)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Midgard_FighterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Midgard_Fighter.CloneTemplate();
						}
						else if (guard is GuardCommander)
							guard.Inventory = ClothingMgr.Midgard_Commander.CloneTemplate();
                        else if (guard is GuardFighterRelic)
                            guard.Inventory = ClothingMgr.Midgard_FighterPK.CloneTemplate();
                        else if (guard is GuardLord|| guard is MissionMaster)
							guard.Inventory = ClothingMgr.Midgard_Lord.CloneTemplate();
						else if (guard is GuardMerchant)
							guard.Inventory = ClothingMgr.Midgard_Merchant.CloneTemplate();
						else if (guard is GuardCurrencyMerchant)
							guard.Inventory = ClothingMgr.Midgard_Merchant.CloneTemplate();
						else if (guard is GuardHealer)
							guard.Inventory = ClothingMgr.Midgard_Healer.CloneTemplate();
						else if (guard is GuardArcher)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Midgard_ArcherPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Midgard_Archer.CloneTemplate();
						}
						else if (guard is GuardCaster)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Midgard_CasterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Midgard_Caster.CloneTemplate();
						}
						else if (guard is GuardStealther)
							guard.Inventory = ClothingMgr.Midgard_Stealther.CloneTemplate();
						break;
					}
				case ERealm.Hibernia:
					{
						if (guard is GuardFighter)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Hibernia_FighterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Hibernia_Fighter.CloneTemplate();
						}
						else if (guard is GuardCommander)
							guard.Inventory = ClothingMgr.Hibernia_Commander.CloneTemplate();
                        else if (guard is GuardFighterRelic)
                            guard.Inventory = ClothingMgr.Hibernia_FighterPK.CloneTemplate();
                        else if (guard is GuardLord || guard is MissionMaster)
							guard.Inventory = ClothingMgr.Hibernia_Lord.CloneTemplate();
						else if (guard is GuardMerchant)
							guard.Inventory = ClothingMgr.Hibernia_Merchant.CloneTemplate();
						else if (guard is GuardCurrencyMerchant)
							guard.Inventory = ClothingMgr.Hibernia_Merchant.CloneTemplate();
						else if (guard is GuardHealer)
							guard.Inventory = ClothingMgr.Hibernia_Healer.CloneTemplate();
						else if (guard is GuardArcher)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Hibernia_ArcherPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Hibernia_Archer.CloneTemplate();
						}
						else if (guard is GuardCaster)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Hibernia_CasterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Hibernia_Caster.CloneTemplate();
						}
						else if (guard is GuardStealther)
							guard.Inventory = ClothingMgr.Hibernia_Stealther.CloneTemplate();
						break;
					}
			}
			if (guard.Inventory == null)
			{
				GameServer.KeepManager.Log.Warn("Clothing Manager: Guard Inventory is null for " + guard.GetType().ToString());
				return;
			}
			GameNpcInventoryTemplate template = guard.Inventory as GameNpcInventoryTemplate;
			guard.Inventory = new GameNpcInventory(template);

			const int renegadeArmorColor = 19;

			DbInventoryItem item = null;
			item = guard.Inventory.GetItem(EInventorySlot.TorsoArmor);
			if (item != null)
			{
				if (guard.Realm != ERealm.None)
				{
					item.Extension = (int)EArmorExtension.Five;
				}
				else
				{
					item.Extension = (int)EArmorExtension.Four;
					item.Color = renegadeArmorColor;
				}
			}
			item = guard.Inventory.GetItem(EInventorySlot.HandsArmor);
			if (item != null)
			{
				if (guard.Realm != ERealm.None)
				{
					item.Extension = (int)EArmorExtension.Five;
				}
				else
				{
					item.Extension = (int)EArmorExtension.Four;
					item.Color = renegadeArmorColor;
				}
			}
			item = guard.Inventory.GetItem(EInventorySlot.FeetArmor);
			if (item != null)
			{
				if (guard.Realm != ERealm.None)
				{
					item.Extension = (int)EArmorExtension.Five;
				}
				else
				{
					item.Extension = (int)EArmorExtension.Four;
					item.Color = renegadeArmorColor;
				}
			}


			if (guard.Realm == ERealm.None)
			{
				item = guard.Inventory.GetItem(EInventorySlot.Cloak);
				if (item != null)
				{
					item.Model = 3632;
					item.Color = renegadeArmorColor;
				}
				item = guard.Inventory.GetItem(EInventorySlot.TorsoArmor);
				if (item != null)
				{
					item.Color = renegadeArmorColor;
				}
				item = guard.Inventory.GetItem(EInventorySlot.ArmsArmor);
				if (item != null)
				{
					item.Color = renegadeArmorColor;
				}
				item = guard.Inventory.GetItem(EInventorySlot.LegsArmor);
				if (item != null)
				{
					item.Color = renegadeArmorColor;
				}
			}

			// set the active slot
			// casters use two handed weapons as default
            // archers use distance weapons as default
			if (guard is GuardCaster)
				guard.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			else if (guard is GuardArcher)
				guard.SwitchWeapon(EActiveWeaponSlot.Distance);
            else if ((guard is GuardFighter || guard is GuardCommander || guard is GuardLord || guard is GuardFighterRelic) && Util.Chance(50))
				guard.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			else guard.SwitchWeapon(EActiveWeaponSlot.Standard);
		}

		/// <summary>
		/// Method to Set an Emblem to a Guards equipment
		/// </summary>
		/// <param name="guard">The guard object</param>
		public static void SetEmblem(GameKeepGuard guard)
		{
			if (guard.Inventory == null)
				return;
			if (guard.Component == null)
				return;
			int emblem = 0;
            if (guard.Component.Keep != null && guard.Component.Keep.Guild != null)
			{
				emblem = guard.Component.Keep.Guild.Emblem;
			}
			DbInventoryItem cloak = guard.Inventory.GetItem(EInventorySlot.Cloak);
			if (cloak != null)
			{
				cloak.Emblem = emblem;

				if (cloak.Emblem != 0)
					cloak.Model = 558; // change to a model that looks ok with an emblem
			}
			DbInventoryItem shield = guard.Inventory.GetItem(EInventorySlot.LeftHandWeapon);
			if (shield != null)
			{
				shield.Emblem = emblem;
			}
			guard.UpdateNPCEquipmentAppearance();
		}
	}
}
