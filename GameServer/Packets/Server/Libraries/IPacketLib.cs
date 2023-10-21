using System;
using System.Collections;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Housing;
using Core.GS.Keeps;
using Core.GS.Quests;

namespace Core.GS.PacketHandler
{
	/// <summary>
	/// The RegionEntry structure
	/// </summary>
	public struct RegionEntry
	{
		/// <summary>
		/// Region expansion
		/// </summary>
		public int expansion;

		/// <summary>
		/// Port client receives on
		/// </summary>
		public string fromPort;

		/// <summary>
		/// Region id
		/// </summary>
		public ushort id;

		/// <summary>
		/// Region IP address
		/// </summary>
		public string ip;

		/// <summary>
		/// Name of the region
		/// </summary>
		public string name;

		/// <summary>
		/// Port the region receives on
		/// </summary>
		public string toPort;
	} ;

	public delegate void CustomDialogResponse(GamePlayer player, byte response);

	public delegate void CheckLOSResponse(GamePlayer player, ushort response, ushort targetOID);
	public delegate void CheckLOSMgrResponse(GamePlayer player, ushort response, ushort sourceOID, ushort targetOID);

	public interface IPacketLib
	{
		/// <summary>
		/// The bow prepare animation
		/// </summary>
		int BowPrepare { get; }

		/// <summary>
		/// The bow shoot animation
		/// </summary>
		int BowShoot { get; }

		/// <summary>
		/// one dual weapon hit animation
		/// </summary>
		int OneDualWeaponHit { get; }

		/// <summary>
		/// both dual weapons hit animation
		/// </summary>
		int BothDualWeaponHit { get; }

		byte GetPacketCode(EServerPackets packetCode);
		void SendTCP(GsTcpPacketOut packet);
		void SendTCP(byte[] buf);
		void SendTCPRaw(GsTcpPacketOut packet);
		void SendUDP(GsUdpPacketOut packet);
		void SendUDP(byte[] buf);
		void SendUDPRaw(GsUdpPacketOut packet);
		// warlock
		void SendWarlockChamberEffect(GamePlayer player);
		void SendVersionAndCryptKey();
		void SendLoginDenied(ELoginError et);
		void SendLoginGranted();
		void SendLoginGranted(byte color);
		void SendSessionID();
		void SendPingReply(ulong timestamp, ushort sequence);
		void SendRealm(ERealm realm);
		void SendCharacterOverview(ERealm realm);
		void SendDupNameCheckReply(string name, byte result);
		void SendBadNameCheckReply(string name, bool bad);
		void SendAttackMode(bool attackState);
		void SendCharCreateReply(string name);
		void SendCharStatsUpdate();
		void SendCharResistsUpdate();
		void SendRegions(ushort region);
		void SendGameOpenReply();
		void SendPlayerPositionAndObjectID();
		void SendPlayerJump(bool headingOnly);
		void SendPlayerInitFinished(byte mobs);
		void SendUDPInitReply();
		void SendTime();
		void SendMessage(string msg, EChatType type, EChatLoc loc);
		void SendPlayerCreate(GamePlayer playerToCreate);
		void SendObjectGuildID(GameObject obj, GuildUtil guild);
		void SendPlayerQuit(bool totalOut);
		void SendObjectRemove(GameObject obj);
		void SendObjectCreate(GameObject obj);
		void SendDebugMode(bool on);
		void SendModelChange(GameObject obj, ushort newModel);
		void SendModelAndSizeChange(GameObject obj, ushort newModel, byte newSize);
		void SendModelAndSizeChange(ushort objectId, ushort newModel, byte newSize);
		void SendEmoteAnimation(GameObject obj, EEmote emote);
		void SendNPCCreate(GameNpc npc);
		void SendLivingEquipmentUpdate(GameLiving living);
		void SendRegionChanged();
		void SendUpdatePoints();
		void SendUpdateMoney();
		void SendUpdateMaxSpeed();
		void SendDelveInfo(string info);

		void SendCombatAnimation(GameObject attacker, GameObject defender, ushort weaponID, ushort shieldID, int style,
		                         byte stance, byte result, byte targetHealthPercent);

		void SendStatusUpdate();
		void SendStatusUpdate(byte sittingFlag);
		void SendSpellCastAnimation(GameLiving spellCaster, ushort spellID, ushort castingTime);

		void SendSpellEffectAnimation(GameObject spellCaster, GameObject spellTarget, ushort spellid, ushort boltTime,
		                              bool noSound, byte success);

		void SendRiding(GameObject rider, GameObject steed, bool dismount);
		void SendFindGroupWindowUpdate(GamePlayer[] list);
		void SendGroupInviteCommand(GamePlayer invitingPlayer, string inviteMessage);

		void SendDialogBox(EDialogCode code, ushort data1, ushort data2, ushort data3, ushort data4, EDialogType type,
		                   bool autoWrapText, string message);

		void SendCustomDialog(string msg, CustomDialogResponse callback);
		void SendCheckLOS(GameObject Checker, GameObject Target, CheckLOSResponse callback);
		void SendCheckLOS(GameObject source, GameObject target, CheckLOSMgrResponse callback);
		void SendGuildLeaveCommand(GamePlayer invitingPlayer, string inviteMessage);
		void SendGuildInviteCommand(GamePlayer invitingPlayer, string inviteMessage);
		void SendQuestOfferWindow(GameNpc questNPC, GamePlayer player, RewardQuest quest);
		void SendQuestRewardWindow(GameNpc questNPC, GamePlayer player, RewardQuest quest);
		void SendQuestOfferWindow(GameNpc questNPC, GamePlayer player, DataQuest quest);
		void SendQuestRewardWindow(GameNpc questNPC, GamePlayer player, DataQuest quest);
		void SendQuestSubscribeCommand(GameNpc invitingNPC, ushort questid, string inviteMessage);
		void SendQuestAbortCommand(GameNpc abortingNPC, ushort questid, string abortMessage);
		void SendGroupWindowUpdate();
		void SendGroupMemberUpdate(bool updateIcons, bool updateMap, GameLiving living);
		void SendGroupMembersUpdate(bool updateIcons, bool updateMap);
		void SendInventoryItemsUpdate(ICollection<DbInventoryItem> itemsToUpdate);
		void SendInventorySlotsUpdate(ICollection<int> slots);
		void SendInventoryItemsUpdate(EInventoryWindowType windowType, ICollection<DbInventoryItem> itemsToUpdate);
		void SendInventoryItemsUpdate(IDictionary<int, DbInventoryItem> updateItems, EInventoryWindowType windowType);
		void SendDoorState(Region region, GameDoorBase door);
		void SendMerchantWindow(MerchantTradeItems itemlist, EMerchantWindowType windowType);
		void SendTradeWindow();
		void SendCloseTradeWindow();
		void SendPlayerDied(GamePlayer killedPlayer, GameObject killer);
		void SendPlayerRevive(GamePlayer revivedPlayer);
		void SendPlayerForgedPosition(GamePlayer player);
		void SendUpdatePlayer();
		void SendUpdatePlayerSkills();
		void SendUpdateWeaponAndArmorStats();
		void SendCustomTextWindow(string caption, IList<string> text);
		void SendPlayerTitles();
		void SendPlayerTitleUpdate(GamePlayer player);
		void SendEncumberance();
		void SendAddFriends(string[] friendNames);
		void SendRemoveFriends(string[] friendNames);
		void SendTimerWindow(string title, int seconds);
		void SendCloseTimerWindow();
		void SendCustomTrainerWindow(int type, List<Tuple<Specialization, List<Tuple<Skill, byte>>>> tree);
		void SendChampionTrainerWindow(int type);
		void SendTrainerWindow();
		void SendInterruptAnimation(GameLiving living);
		void SendDisableSkill(ICollection<Tuple<Skill, int>> skills);
		void SendUpdateIcons(IList changedEffects, ref int lastUpdateEffectsCount);
		void SendLevelUpSound();
		void SendRegionEnterSound(byte soundId);
		void SendDebugMessage(string format, params object[] parameters);
		void SendDebugPopupMessage(string format, params object[] parameters);
		void SendEmblemDialogue();
		void SendWeather(uint x, uint width, ushort speed, ushort fogdiffusion, ushort intensity);
		void SendPlayerModelTypeChange(GamePlayer player, byte modelType);
		void SendObjectDelete(GameObject obj);
		void SendObjectDelete(ushort oid);
		void SendObjectUpdate(GameObject obj);
		void SendQuestListUpdate();
		void SendQuestUpdate(AQuest quest);
		void SendQuestRemove(byte index);
		void SendConcentrationList();
		void SendUpdateCraftingSkills();
		void SendChangeTarget(GameObject newTarget);
		void SendChangeGroundTarget(Point3D newTarget);
		void SendPetWindow(GameLiving pet, EPetWindowAction windowAction, EAggressionState aggroState, EWalkState walkState);
		void SendPlaySound(ESoundType soundType, ushort soundID);
		void SendNPCsQuestEffect(GameNpc npc, EQuestIndicator indicator);
		void SendMasterLevelWindow(byte ml);
		void SendHexEffect(GamePlayer player, byte effect1, byte effect2, byte effect3, byte effect4, byte effect5);
		void SendRvRGuildBanner(GamePlayer player, bool show);
		void SendSiegeWeaponAnimation(GameSiegeWeapon siegeWeapon);
		void SendSiegeWeaponFireAnimation(GameSiegeWeapon siegeWeapon, int timer);
		void SendSiegeWeaponCloseInterface();
		void SendSiegeWeaponInterface(GameSiegeWeapon siegeWeapon, int time);
		void SendLivingDataUpdate(GameLiving living, bool updateStrings);
		void SendSoundEffect(ushort soundId, ushort zoneId, ushort x, ushort y, ushort z, ushort radius);
		//keep
		void SendKeepInfo(IGameKeep keep);
		void SendKeepRealmUpdate(IGameKeep keep);
		void SendKeepRemove(IGameKeep keep);
		void SendKeepComponentInfo(IGameKeepComponent keepComponent);
		void SendKeepComponentDetailUpdate(IGameKeepComponent keepComponent);
		void SendKeepComponentRemove(IGameKeepComponent keepComponent);
		void SendKeepClaim(IGameKeep keep, byte flag);
		void SendKeepComponentUpdate(IGameKeep keep, bool LevelUp);
		void SendKeepComponentInteract(IGameKeepComponent component);
		void SendKeepComponentHookPoint(IGameKeepComponent component, int selectedHookPointIndex);
		void SendClearKeepComponentHookPoint(IGameKeepComponent component, int selectedHookPointIndex);
		void SendHookPointStore(GameKeepHookPoint hookPoint);
		void SendWarmapUpdate(ICollection<IGameKeep> list);
		void SendWarmapDetailUpdate(List<List<byte>> fights, List<List<byte>> groups);
		void SendWarmapBonuses();

		//housing
		void SendHouse(House house);
		void SendHouseOccupied(House house, bool flagHouseOccuped);
		void SendRemoveHouse(House house);
		void SendGarden(House house);
		void SendGarden(House house, int i);
		void SendEnterHouse(House house);
		void SendExitHouse(House house, ushort unknown = 0);
		void SendFurniture(House house);
		void SendFurniture(House house, int i);
		void SendHousePayRentDialog(string title);
		void SendToggleHousePoints(House house);
		void SendRentReminder(House house);
		void SendMarketExplorerWindow(IList<DbInventoryItem> items, byte page, byte maxpage);
		void SendMarketExplorerWindow();
		void SendConsignmentMerchantMoney(long money);
		void SendHouseUsersPermissions(House house);

		void SendStarterHelp();
		void SendPlayerFreeLevelUpdate();

		void SendMovingObjectCreate(GameMovingObject obj);
		void SendSetControlledHorse(GamePlayer player);
		void SendControlledHorse(GamePlayer player, bool flag);
		void CheckLengthHybridSkillsPacket(ref GsTcpPacketOut pak, ref int maxSkills, ref int first);
		void SendNonHybridSpellLines();
		void SendCrash(string str);
		void SendRegionColorScheme();
		void SendRegionColorScheme(byte color);
		void SendVampireEffect(GameLiving living, bool show);
		void SendXFireInfo(byte flag);
		void SendMinotaurRelicMapRemove(byte id);
		void SendMinotaurRelicMapUpdate(byte id, ushort region, int x, int y, int z);
		void SendMinotaurRelicWindow(GamePlayer player, int spell, bool flag);
		void SendMinotaurRelicBarUpdate(GamePlayer player, int xp);

		/// <summary>
		/// Makes a specific UI Part "blink"
		/// </summary>
		/// <param name="flag">The UI part as byte (See ePanel enum for details)</param>
		void SendBlinkPanel(byte flag);
	}
}