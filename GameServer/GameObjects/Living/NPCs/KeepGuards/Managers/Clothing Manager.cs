using DOL.Database;

namespace DOL.GS.Keeps
{
	/// <summary>
	/// This is a convieniance enum for for inventory item hand flag
	/// </summary>
	public enum eHandFlag
	{
		/// <summary>
		/// Right Handed Weapon
		/// </summary>
		Right = 0,
		/// <summary>
		/// Two Handed Weapon
		/// </summary>
		Two = 1,
		/// <summary>
		/// Left Handed Weapon
		/// </summary>
		Left = 2,
	}

	/// <summary>
	/// This enum is used to tell us what extension level we want the armor to be
	/// </summary>
	public enum eExtension
	{ 
		/// <summary>
		/// Armor Extension 2
		/// </summary>
		Two = 2,
		/// <summary>
		/// Armor Extension 3
		/// </summary>
		Three = 3,
		/// <summary>
		/// Armor Extension 4
		/// </summary>
		Four = 4,
		/// <summary>
		/// Armor Extension 5
		/// </summary>
		Five = 5,
	}

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
                Albion_Archer.AddNPCEquipment(eInventorySlot.Cloak, 92);
                Albion_Archer.AddNPCEquipment(eInventorySlot.TorsoArmor, 728);
                Albion_Archer.AddNPCEquipment(eInventorySlot.LegsArmor, 663);
                Albion_Archer.AddNPCEquipment(eInventorySlot.ArmsArmor, 664);
                Albion_Archer.AddNPCEquipment(eInventorySlot.HandsArmor, 665);
                Albion_Archer.AddNPCEquipment(eInventorySlot.FeetArmor, 666);
                Albion_Archer.AddNPCEquipment(eInventorySlot.DistanceWeapon, 849);
                Albion_Archer.AddNPCEquipment(eInventorySlot.RightHandWeapon, 653);
                Albion_Archer.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 59);
                Albion_Archer = Albion_Archer.CloseTemplate();
                Albion_Archer.GetItem(eInventorySlot.DistanceWeapon).Hand = (int)eHandFlag.Two;
                Albion_Archer.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
            }
            #endregion
            #region Caster
            if (!Albion_Caster.LoadFromDatabase("albion_caster"))
            {
                Albion_Caster.AddNPCEquipment(eInventorySlot.Cloak, 92);
                Albion_Caster.AddNPCEquipment(eInventorySlot.TorsoArmor, 58);
                Albion_Caster.AddNPCEquipment(eInventorySlot.HandsArmor, 142);
                Albion_Caster.AddNPCEquipment(eInventorySlot.FeetArmor, 143);
                Albion_Caster.AddNPCEquipment(eInventorySlot.RightHandWeapon, 13);
                Albion_Caster.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1170);
                Albion_Caster = Albion_Caster.CloseTemplate();
                Albion_Caster.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Fighter
            if (!Albion_Fighter.LoadFromDatabase("albion_fighter"))
            {
                Albion_Fighter.AddNPCEquipment(eInventorySlot.Cloak, 92); 
                Albion_Fighter.AddNPCEquipment(eInventorySlot.TorsoArmor, 662);
                Albion_Fighter.AddNPCEquipment(eInventorySlot.LegsArmor, 663);
                Albion_Fighter.AddNPCEquipment(eInventorySlot.ArmsArmor, 664);
                Albion_Fighter.AddNPCEquipment(eInventorySlot.HandsArmor, 665);
                Albion_Fighter.AddNPCEquipment(eInventorySlot.FeetArmor, 666);
                Albion_Fighter.AddNPCEquipment(eInventorySlot.HeadArmor, 95);
                Albion_Fighter.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 60);
                Albion_Fighter.AddNPCEquipment(eInventorySlot.RightHandWeapon, 10);
                Albion_Fighter.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 649);
                Albion_Fighter = Albion_Fighter.CloseTemplate();
                Albion_Fighter.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Albion_Fighter.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Commander
            if (!Albion_Commander.LoadFromDatabase("albion_commander"))
            {
	            Albion_Commander.AddNPCEquipment(eInventorySlot.Cloak, 676);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.TorsoArmor, 662);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.LegsArmor, 663);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.ArmsArmor, 664);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.HandsArmor, 665);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.FeetArmor, 666);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.HeadArmor, 95);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 60);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.RightHandWeapon, 10);
	            Albion_Commander.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 649);
	            Albion_Commander = Albion_Commander.CloseTemplate();
	            Albion_Commander.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
	            Albion_Commander.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Merchant
            if (!Albion_Merchant.LoadFromDatabase("albion_guard_merchant"))
            {
	            
	            Albion_Merchant.AddNPCEquipment(eInventorySlot.TorsoArmor, 51, 0);
	            Albion_Merchant.AddNPCEquipment(eInventorySlot.LegsArmor, 52, 0);
	            Albion_Merchant.AddNPCEquipment(eInventorySlot.ArmsArmor, 53, 0);
	            Albion_Merchant.AddNPCEquipment(eInventorySlot.HandsArmor, 80, 0);
	            Albion_Merchant.AddNPCEquipment(eInventorySlot.FeetArmor, 54, 0);
	            Albion_Merchant.AddNPCEquipment(eInventorySlot.RightHandWeapon, 5);
	            Albion_Merchant = Albion_Merchant.CloseTemplate();
	            Albion_Merchant.GetItem(eInventorySlot.RightHandWeapon).Object_Type = (int) eObjectType.SlashingWeapon;
            }
            #endregion
            #region Relic Lord
            if (!Relic_Albion_Lord.LoadFromDatabase("relic_albion_lord"))
            {
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.Cloak, 676); //676
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.TorsoArmor, 662);
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.LegsArmor, 663);
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.ArmsArmor, 664);
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.HandsArmor, 665);
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.FeetArmor, 666);
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.HeadArmor, 95);
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 60);
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.RightHandWeapon, 10);
                Relic_Albion_Lord.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 649);
                Relic_Albion_Lord = Relic_Albion_Lord.CloseTemplate();
                Relic_Albion_Lord.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Relic_Albion_Lord.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Lord
            if (!Albion_Lord.LoadFromDatabase("albion_lord"))
            {
                Albion_Lord.AddNPCEquipment(eInventorySlot.Cloak, 676); //676
                Albion_Lord.AddNPCEquipment(eInventorySlot.TorsoArmor, 662);
                Albion_Lord.AddNPCEquipment(eInventorySlot.LegsArmor, 663);
                Albion_Lord.AddNPCEquipment(eInventorySlot.ArmsArmor, 664);
                Albion_Lord.AddNPCEquipment(eInventorySlot.HandsArmor, 665);
                Albion_Lord.AddNPCEquipment(eInventorySlot.FeetArmor, 666);
                Albion_Lord.AddNPCEquipment(eInventorySlot.HeadArmor, 95);
                Albion_Lord.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 60);
                Albion_Lord.AddNPCEquipment(eInventorySlot.RightHandWeapon, 10);
                Albion_Lord.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 649);
                Albion_Lord.AddNPCEquipment(eInventorySlot.DistanceWeapon, 132);
                Albion_Lord = Albion_Lord.CloseTemplate();
                Albion_Lord.GetItem(eInventorySlot.DistanceWeapon).Hand = (int)eHandFlag.Two;
                Albion_Lord.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Albion_Lord.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Healer
            if (!Albion_Healer.LoadFromDatabase("albion_healer"))
            {
                Albion_Healer.AddNPCEquipment(eInventorySlot.Cloak, 92);
                Albion_Healer.AddNPCEquipment(eInventorySlot.TorsoArmor, 713);
                Albion_Healer.AddNPCEquipment(eInventorySlot.LegsArmor, 663);
                Albion_Healer.AddNPCEquipment(eInventorySlot.ArmsArmor, 664);
                Albion_Healer.AddNPCEquipment(eInventorySlot.HandsArmor, 665);
                Albion_Healer.AddNPCEquipment(eInventorySlot.FeetArmor, 666);
                Albion_Healer.AddNPCEquipment(eInventorySlot.HeadArmor, 94);
                Albion_Healer.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 61);
                Albion_Healer.AddNPCEquipment(eInventorySlot.RightHandWeapon, 3282);
                Albion_Healer = Albion_Healer.CloseTemplate();
                Albion_Fighter.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
            }
            #endregion
            #region Stealther
            if (!Albion_Stealther.LoadFromDatabase("albion_stealther"))
            {
                Albion_Stealther.AddNPCEquipment(eInventorySlot.Cloak, 92);
                Albion_Stealther.AddNPCEquipment(eInventorySlot.TorsoArmor, 792);
                Albion_Stealther.AddNPCEquipment(eInventorySlot.LegsArmor, 663);
                Albion_Stealther.AddNPCEquipment(eInventorySlot.ArmsArmor, 664);
                Albion_Stealther.AddNPCEquipment(eInventorySlot.HandsArmor, 665);
                Albion_Stealther.AddNPCEquipment(eInventorySlot.FeetArmor, 666);
                Albion_Stealther.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 653);
                Albion_Stealther.AddNPCEquipment(eInventorySlot.RightHandWeapon, 653);
                Albion_Stealther.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 653);
                Albion_Stealther = Albion_Stealther.CloseTemplate();
                Albion_Stealther.GetItem(eInventorySlot.LeftHandWeapon).Hand = (int)eHandFlag.Left;
                Albion_Stealther.GetItem(eInventorySlot.LeftHandWeapon).SlotPosition = Slot.LEFTHAND;
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
                Midgard_Archer.AddNPCEquipment(eInventorySlot.Cloak, 677);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.TorsoArmor, 668);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.LegsArmor, 2943);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.ArmsArmor, 2944);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.HandsArmor, 2945);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.FeetArmor, 2946);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.HeadArmor, 2874);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.DistanceWeapon, 1037);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 328);
                Midgard_Archer.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 59);
                Midgard_Archer = Midgard_Archer.CloseTemplate();
                Midgard_Archer.GetItem(eInventorySlot.DistanceWeapon).Hand = (int)eHandFlag.Two;
                Midgard_Archer.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
                Midgard_Archer.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
            }
            #endregion
            #region Caster
            if (!Midgard_Caster.LoadFromDatabase("midgard_caster"))
            {
                Midgard_Caster.AddNPCEquipment(eInventorySlot.Cloak, 677); 
                Midgard_Caster.AddNPCEquipment(eInventorySlot.TorsoArmor, 98);
                Midgard_Caster.AddNPCEquipment(eInventorySlot.HandsArmor, 142);
                Midgard_Caster.AddNPCEquipment(eInventorySlot.FeetArmor, 143);
                Midgard_Caster.AddNPCEquipment(eInventorySlot.RightHandWeapon, 13);
                Midgard_Caster.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 566);
                Midgard_Caster = Midgard_Caster.CloseTemplate();
                Midgard_Caster.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Fighter
            if (!Midgard_Fighter.LoadFromDatabase("midgard_fighter"))
            {
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.Cloak, 677); 
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.TorsoArmor, 668);
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.LegsArmor, 2943);
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.ArmsArmor, 2944);
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.HandsArmor, 2945);
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.FeetArmor, 2946);
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.HeadArmor, 2874);
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 60);
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.RightHandWeapon, 313);
                Midgard_Fighter.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 572);
                Midgard_Fighter = Midgard_Fighter.CloseTemplate();
                Midgard_Fighter.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Midgard_Fighter.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Commander
            if (!Midgard_Commander.LoadFromDatabase("midgard_commander"))
            {
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.Cloak, 677);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.TorsoArmor, 668);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.LegsArmor, 2943);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.ArmsArmor, 2944);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.HandsArmor, 2945);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.FeetArmor, 2946);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.HeadArmor, 2874);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 60);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.RightHandWeapon, 313);
	            Midgard_Commander.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 572);
	            Midgard_Commander = Midgard_Commander.CloseTemplate();
	            Midgard_Commander.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
	            Midgard_Commander.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Merchant
            if (!Midgard_Merchant.LoadFromDatabase("midgard_guard_merchant"))
            {
	            Midgard_Merchant.AddNPCEquipment(eInventorySlot.TorsoArmor, 250, 0);
	            Midgard_Merchant.AddNPCEquipment(eInventorySlot.LegsArmor, 251, 0);
	            Midgard_Merchant.AddNPCEquipment(eInventorySlot.ArmsArmor, 252, 0);
	            Midgard_Merchant.AddNPCEquipment(eInventorySlot.HandsArmor, 253, 0);
	            Midgard_Merchant.AddNPCEquipment(eInventorySlot.FeetArmor, 254, 0);
	            Midgard_Merchant.AddNPCEquipment(eInventorySlot.RightHandWeapon, 312);
	            Midgard_Merchant = Midgard_Merchant.CloseTemplate();
	            Midgard_Merchant.GetItem(eInventorySlot.RightHandWeapon).Object_Type = (int) eObjectType.Sword;
            }
            #endregion
            #region Lord
            if (!Midgard_Lord.LoadFromDatabase("midgard_lord"))
            {
                Midgard_Lord.AddNPCEquipment(eInventorySlot.Cloak, 677);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.TorsoArmor, 668);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.LegsArmor, 2943);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.ArmsArmor, 2944);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.HandsArmor, 2945);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.FeetArmor, 2946);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.HeadArmor, 2874);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 60);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.RightHandWeapon, 313);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 572);
                Midgard_Lord.AddNPCEquipment(eInventorySlot.DistanceWeapon, 564);
                Midgard_Lord = Midgard_Lord.CloseTemplate();
                Midgard_Lord.GetItem(eInventorySlot.DistanceWeapon).Hand = (int)eHandFlag.Two;
                Midgard_Lord.GetItem(eInventorySlot.DistanceWeapon).Object_Type = (int)eObjectType.Longbow;
                Midgard_Lord.GetItem(eInventorySlot.DistanceWeapon).SlotPosition = Slot.RANGED;
                Midgard_Lord.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Midgard_Lord.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Relic Lord
            if (!Relic_Midgard_Lord.LoadFromDatabase("relic_midgard_lord"))
            {
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.Cloak, 677);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.TorsoArmor, 668);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.LegsArmor, 2943);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.ArmsArmor, 2944);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.HandsArmor, 2945);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.FeetArmor, 2946);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.HeadArmor, 2874);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 60);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.RightHandWeapon, 313);
                Relic_Midgard_Lord.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 572);
                Relic_Midgard_Lord = Relic_Midgard_Lord.CloseTemplate();
                Relic_Midgard_Lord.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Relic_Midgard_Lord.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Healer
            if (!Midgard_Healer.LoadFromDatabase("midgard_healer"))
            {
                Midgard_Healer.AddNPCEquipment(eInventorySlot.Cloak, 677); 
                Midgard_Healer.AddNPCEquipment(eInventorySlot.TorsoArmor, 668);
                Midgard_Healer.AddNPCEquipment(eInventorySlot.LegsArmor, 2943);
                Midgard_Healer.AddNPCEquipment(eInventorySlot.ArmsArmor, 2944);
                Midgard_Healer.AddNPCEquipment(eInventorySlot.HandsArmor, 2945);
                Midgard_Healer.AddNPCEquipment(eInventorySlot.FeetArmor, 2946);
                Midgard_Healer.AddNPCEquipment(eInventorySlot.HeadArmor, 2874);
                Midgard_Healer.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 59);
                Midgard_Healer.AddNPCEquipment(eInventorySlot.RightHandWeapon, 3335);
                Midgard_Healer.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 3336);
                Midgard_Healer = Midgard_Healer.CloseTemplate();
                Midgard_Healer.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
            }
            #endregion
            #region Hastener
            if (!Midgard_Hastener.LoadFromDatabase("midgard_hastener"))
            {
                Midgard_Hastener.AddNPCEquipment(eInventorySlot.Cloak, 443, 43);
                Midgard_Hastener.AddNPCEquipment(eInventorySlot.TorsoArmor, 230);
                Midgard_Hastener.AddNPCEquipment(eInventorySlot.HandsArmor, 233);
                Midgard_Hastener.AddNPCEquipment(eInventorySlot.FeetArmor, 234);
                Midgard_Hastener.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 228);
                Midgard_Hastener = Midgard_Hastener.CloseTemplate();
                Midgard_Hastener.GetItem(eInventorySlot.LeftHandWeapon).Hand = (int)eHandFlag.Left;
                Midgard_Hastener.GetItem(eInventorySlot.LeftHandWeapon).SlotPosition = Slot.LEFTHAND;
            }
            #endregion
            #region Stealther
            if (!Midgard_Stealther.LoadFromDatabase("midgard_stealther"))
            {
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.Cloak, 677); 
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.TorsoArmor, 668);
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.LegsArmor, 2943);
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.ArmsArmor, 2944);
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.HandsArmor, 2945);
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.FeetArmor, 2946);
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.HeadArmor, 335);
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 573);
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.RightHandWeapon, 573);
                Midgard_Stealther.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 577);
                Midgard_Stealther = Midgard_Stealther.CloseTemplate();
                Midgard_Stealther.GetItem(eInventorySlot.LeftHandWeapon).Hand = (int)eHandFlag.Left;
                Albion_Stealther.GetItem(eInventorySlot.LeftHandWeapon).SlotPosition = Slot.LEFTHAND;
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
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.Cloak, 678);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.TorsoArmor, 667);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.LegsArmor, 989);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.ArmsArmor, 990);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.HandsArmor, 991);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.FeetArmor, 992);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.HeadArmor, 1207);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.DistanceWeapon, 919);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.RightHandWeapon, 643);
                Hibernia_Archer.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 643);
                Hibernia_Archer = Hibernia_Archer.CloseTemplate();
                Hibernia_Archer.GetItem(eInventorySlot.DistanceWeapon).Hand = (int)eHandFlag.Two;
                Hibernia_Archer.GetItem(eInventorySlot.DistanceWeapon).Object_Type = (int)eObjectType.RecurvedBow;
                Hibernia_Archer.GetItem(eInventorySlot.DistanceWeapon).SlotPosition = Slot.RANGED;
                Hibernia_Archer.GetItem(eInventorySlot.LeftHandWeapon).Hand = (int)eHandFlag.Left;
            }
            #endregion
            #region Caster
            if (!Hibernia_Caster.LoadFromDatabase("hibernia_caster"))
            {
                Hibernia_Caster.AddNPCEquipment(eInventorySlot.Cloak, 678);
                Hibernia_Caster.AddNPCEquipment(eInventorySlot.TorsoArmor, 97);
                Hibernia_Caster.AddNPCEquipment(eInventorySlot.HandsArmor, 142);
                Hibernia_Caster.AddNPCEquipment(eInventorySlot.FeetArmor, 143);
                Hibernia_Caster.AddNPCEquipment(eInventorySlot.RightHandWeapon, 13);
                Hibernia_Caster.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1176);
                Hibernia_Caster = Hibernia_Caster.CloseTemplate();
                Hibernia_Caster.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Fighter
            if (!Hibernia_Fighter.LoadFromDatabase("hibernia_fighter"))
            {
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.Cloak, 678);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.TorsoArmor, 667);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.LegsArmor, 989);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.ArmsArmor, 990);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.HandsArmor, 991);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.FeetArmor, 992);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.HeadArmor, 1207);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 79);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.RightHandWeapon, 897);
                Hibernia_Fighter.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 476);
                Hibernia_Fighter = Hibernia_Fighter.CloseTemplate();
                Hibernia_Fighter.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Hibernia_Fighter.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Commander
            if (!Hibernia_Commander.LoadFromDatabase("hibernia_commander"))
            {
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.Cloak, 678);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.TorsoArmor, 667);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.LegsArmor, 989);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.ArmsArmor, 990);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.HandsArmor, 991);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.FeetArmor, 992);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.HeadArmor, 1207);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 79);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.RightHandWeapon, 897);
	            Hibernia_Commander.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 476);
	            Hibernia_Commander = Hibernia_Commander.CloseTemplate();
	            Hibernia_Commander.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
	            Hibernia_Commander.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Merchant
            if (!Hibernia_Merchant.LoadFromDatabase("hibernia_guard_merchant"))
            {
	            Hibernia_Merchant.AddNPCEquipment(eInventorySlot.TorsoArmor, 363, 0);
	            Hibernia_Merchant.AddNPCEquipment(eInventorySlot.LegsArmor, 364, 0);
	            Hibernia_Merchant.AddNPCEquipment(eInventorySlot.ArmsArmor, 365, 0);
	            Hibernia_Merchant.AddNPCEquipment(eInventorySlot.HandsArmor, 366, 0);
	            Hibernia_Merchant.AddNPCEquipment(eInventorySlot.FeetArmor, 367, 0);
	            Hibernia_Merchant.AddNPCEquipment(eInventorySlot.RightHandWeapon, 447);
	            Hibernia_Merchant = Hibernia_Merchant.CloseTemplate();
	            Hibernia_Merchant.GetItem(eInventorySlot.RightHandWeapon).Object_Type = (int) eObjectType.Blades;
            }
            #endregion
            #region Lord
            if (!Hibernia_Lord.LoadFromDatabase("hibernia_lord"))
            {
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.Cloak, 678); 
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.TorsoArmor, 667);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.LegsArmor, 989);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.ArmsArmor, 990);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.HandsArmor, 991);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.FeetArmor, 992);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.HeadArmor, 1207);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 79);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.RightHandWeapon, 897);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 476);
                Hibernia_Lord.AddNPCEquipment(eInventorySlot.DistanceWeapon, 471);
                Hibernia_Lord = Hibernia_Lord.CloseTemplate();
                Hibernia_Lord.GetItem(eInventorySlot.DistanceWeapon).Hand = (int)eHandFlag.Two;
                Hibernia_Lord.GetItem(eInventorySlot.DistanceWeapon).Object_Type = (int)eObjectType.CompositeBow;
                Hibernia_Lord.GetItem(eInventorySlot.DistanceWeapon).SlotPosition = Slot.RANGED;
                Hibernia_Lord.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Hibernia_Lord.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Relic Lord
            if (!Relic_Hibernia_Lord.LoadFromDatabase("relic_hibernia_lord"))
            {
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.Cloak, 678);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.TorsoArmor, 667);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.LegsArmor, 989);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.ArmsArmor, 990);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.HandsArmor, 991);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.FeetArmor, 992);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.HeadArmor, 1207);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 79);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.RightHandWeapon, 897);
                Relic_Hibernia_Lord.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 476);
                Relic_Hibernia_Lord = Relic_Hibernia_Lord.CloseTemplate();
                Relic_Hibernia_Lord.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
                Relic_Hibernia_Lord.GetItem(eInventorySlot.TwoHandWeapon).Hand = (int)eHandFlag.Two;
            }
            #endregion
            #region Healer
            if (!Hibernia_Healer.LoadFromDatabase("hibernia_healer"))
            {
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.Cloak, 678);
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.TorsoArmor, 667);
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.LegsArmor, 989);
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.ArmsArmor, 990);
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.HandsArmor, 991);
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.FeetArmor, 992);
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.HeadArmor, 1207);
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 59);
                Hibernia_Healer.AddNPCEquipment(eInventorySlot.RightHandWeapon, 3247);
                Hibernia_Healer = Hibernia_Healer.CloseTemplate();
                Hibernia_Healer.GetItem(eInventorySlot.LeftHandWeapon).Object_Type = (int)eObjectType.Shield;
            }
            #endregion
            #region Stealther
            if (!Hibernia_Stealther.LoadFromDatabase("hibernia_stealther"))
            {
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.Cloak, 678);
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.TorsoArmor, 667);
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.LegsArmor, 989);
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.ArmsArmor, 990);
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.HandsArmor, 991);
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.FeetArmor, 992);
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 2685);
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.RightHandWeapon, 2685);
                Hibernia_Stealther.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 2687);
                Hibernia_Stealther = Hibernia_Stealther.CloseTemplate();
                Hibernia_Stealther.GetItem(eInventorySlot.LeftHandWeapon).Hand = (int)eHandFlag.Left;
                Albion_Stealther.GetItem(eInventorySlot.LeftHandWeapon).SlotPosition = Slot.LEFTHAND;
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
			if(!ServerProperties.Properties.AUTOEQUIP_GUARDS_LOADED_FROM_DB && !guard.LoadedFromScript)
			{
				return;
			}
            if (guard is FrontierHastener || guard is GateKeeperIn || guard is GateKeeperOut)
            {
                switch (guard.Realm)
                {
                    case eRealm.None:
                    case eRealm.Albion:
                    case eRealm.Hibernia:
                    case eRealm.Midgard:
                        {
                            guard.Inventory = ClothingMgr.Midgard_Hastener.CloneTemplate();
                            break;
                        }
                }
            }

			switch (guard.ModelRealm)
			{
				case eRealm.None:
				case eRealm.Albion:
					{
						if (guard is GuardFighter)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Albion_FighterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Albion_Fighter.CloneTemplate();
						}
						else if (guard is GuardCommander)
							guard.Inventory = ClothingMgr.Albion_Commander.CloneTemplate();
						else if (guard is GuardFighterRK)
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
				case eRealm.Midgard:
					{
						if (guard is GuardFighter)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Midgard_FighterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Midgard_Fighter.CloneTemplate();
						}
						else if (guard is GuardCommander)
							guard.Inventory = ClothingMgr.Midgard_Commander.CloneTemplate();
                        else if (guard is GuardFighterRK)
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
				case eRealm.Hibernia:
					{
						if (guard is GuardFighter)
						{
							if (guard.IsPortalKeepGuard || guard.Level == 255)
								guard.Inventory = ClothingMgr.Hibernia_FighterPK.CloneTemplate();
							else guard.Inventory = ClothingMgr.Hibernia_Fighter.CloneTemplate();
						}
						else if (guard is GuardCommander)
							guard.Inventory = ClothingMgr.Hibernia_Commander.CloneTemplate();
                        else if (guard is GuardFighterRK)
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
			guard.Inventory = new GameNPCInventory(template);

			const int renegadeArmorColor = 19;

			DbInventoryItem item = null;
			item = guard.Inventory.GetItem(eInventorySlot.TorsoArmor);
			if (item != null)
			{
				if (guard.Realm != eRealm.None)
				{
					item.Extension = (int)eExtension.Five;
				}
				else
				{
					item.Extension = (int)eExtension.Four;
					item.Color = renegadeArmorColor;
				}
			}
			item = guard.Inventory.GetItem(eInventorySlot.HandsArmor);
			if (item != null)
			{
				if (guard.Realm != eRealm.None)
				{
					item.Extension = (int)eExtension.Five;
				}
				else
				{
					item.Extension = (int)eExtension.Four;
					item.Color = renegadeArmorColor;
				}
			}
			item = guard.Inventory.GetItem(eInventorySlot.FeetArmor);
			if (item != null)
			{
				if (guard.Realm != eRealm.None)
				{
					item.Extension = (int)eExtension.Five;
				}
				else
				{
					item.Extension = (int)eExtension.Four;
					item.Color = renegadeArmorColor;
				}
			}


			if (guard.Realm == eRealm.None)
			{
				item = guard.Inventory.GetItem(eInventorySlot.Cloak);
				if (item != null)
				{
					item.Model = 3632;
					item.Color = renegadeArmorColor;
				}
				item = guard.Inventory.GetItem(eInventorySlot.TorsoArmor);
				if (item != null)
				{
					item.Color = renegadeArmorColor;
				}
				item = guard.Inventory.GetItem(eInventorySlot.ArmsArmor);
				if (item != null)
				{
					item.Color = renegadeArmorColor;
				}
				item = guard.Inventory.GetItem(eInventorySlot.LegsArmor);
				if (item != null)
				{
					item.Color = renegadeArmorColor;
				}
			}

			// set the active slot
			// casters use two handed weapons as default
            // archers use distance weapons as default
			if (guard is GuardCaster)
				guard.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
			else if (guard is GuardArcher)
				guard.SwitchWeapon(eActiveWeaponSlot.Distance);
            else if ((guard is GuardFighter || guard is GuardCommander || guard is GuardLord || guard is GuardFighterRK) && Util.Chance(50))
				guard.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
			else guard.SwitchWeapon(eActiveWeaponSlot.Standard);
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
			DbInventoryItem cloak = guard.Inventory.GetItem(eInventorySlot.Cloak);
			if (cloak != null)
			{
				cloak.Emblem = emblem;

				if (cloak.Emblem != 0)
					cloak.Model = 558; // change to a model that looks ok with an emblem
			}
			DbInventoryItem shield = guard.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
			if (shield != null)
			{
				shield.Emblem = emblem;
			}
			guard.UpdateNPCEquipmentAppearance();
		}
	}
}
