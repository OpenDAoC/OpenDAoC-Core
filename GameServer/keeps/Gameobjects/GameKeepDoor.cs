using System;
using System.Collections;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS.Keeps
{
    /// <summary>
    /// keep door in world
    /// </summary>
    public class GameKeepDoor : GameDoorBase, IKeepItem
    {
        private const int DOOR_CLOSE_THRESHOLD = 15;
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region properties

        private int _oldMaxHealth;
        private byte _oldHealthPercent;

        private bool _relicMessage75;
        private bool _relicMessage50;
        private bool _relicMessage25;

        public int OwnerKeepID => DoorId / 100000 % 1000;
        public int TowerNum => DoorId / 10000 % 10;
        public int KeepID => OwnerKeepID + TowerNum * 256;
        public int ComponentID => DoorId / 100 % 100;
        public int DoorIndex => DoorId % 10;

        /// <summary>
        /// This flag is send in packet(keep door = 4, regular door = 0)
        /// </summary>
        public override uint Flag
        {
            get => 4;
            set { }
        }

        /// <summary>
        /// Get the realm of the keep door from keep owner
        /// </summary>
        public override eRealm Realm => Component == null || Component.Keep == null ? eRealm.None : Component.Keep.Realm;

        /// <summary>
        /// The level of door is keep level now
        /// </summary>
        public override byte Level => Component == null || Component.Keep == null ? (byte) 0 : Component.Keep.Level;

        public bool IsRelic => Component.Keep.IsRelic;

        public void UpdateLevel()
        {
            if (MaxHealth != _oldMaxHealth)
            {
                if (_oldMaxHealth > 0)
                    Health = (int)Math.Ceiling(Health * MaxHealth / (double)_oldMaxHealth);

                _oldMaxHealth = MaxHealth;
            }

            SaveIntoDatabase();
        }

        public override bool IsAttackableDoor
        {
            get
            {
                if (Component == null || Component.Keep == null)
                    return false;

                if (Component.Keep is GameKeepTower)
                {
                    if (DoorIndex == 1)
                        return true;
                }
                else if (Component.Keep is GameKeep or RelicGameKeep)
                    return !IsPostern;

                return false;
            }
        }

        public override int Health
        {
            get => !IsAttackableDoor ? 0 : base.Health;
            set
            {
                base.Health = value;

                if (HealthPercent > DOOR_CLOSE_THRESHOLD && State == eDoorState.Open)
                    CloseDoor();
            }
        }

        public override int RealmPointsValue => 0;

        public override long ExperienceValue => 0;

        public override string Name
        {
            get
            {
                string name;

                if (IsAttackableDoor)
                    name = IsRelic ? "Relic Gate" : "Keep Door";
                else
                    name = "Postern Door";

                if (Properties.ENABLE_DEBUG)
                    name += " ( C:" + ComponentID + " T:" + TemplateID + ")";

                return name;
            }
        }

        protected string m_templateID;
        public string TemplateID => m_templateID;

        protected GameKeepComponent m_component;
        public GameKeepComponent Component
        {
            get { return m_component; }
            set { m_component = value; }
        }

        protected DbKeepPosition m_position;
        public DbKeepPosition Position
        {
            get { return m_position; }
            set { m_position = value; }
        }

        #endregion

        #region function override

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (damageAmount > 0 && IsAlive)
            {
                Component.Keep.LastAttackedByEnemyTick = CurrentRegion.Time;
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);

                //only on hp change
                if (_oldHealthPercent != HealthPercent)
                {
                    _oldHealthPercent = HealthPercent;

                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        ClientService.UpdateObjectForPlayer(player, this);
                }
            }

            if (!IsRelic)
                return;

            if (HealthPercent == 25)
            {
                if (!_relicMessage25)
                {
                    _relicMessage25 = true;
                    BroadcastRelicGateDamage();
                }
            }

            if (HealthPercent == 50)
            {
                if (!_relicMessage50)
                {
                    _relicMessage50 = true;
                    BroadcastRelicGateDamage();
                }
            }

            if (HealthPercent == 75)
            {
                if (!_relicMessage75)
                {
                    _relicMessage75 = true;
                    BroadcastRelicGateDamage();
                }
            }
        }

        private void BroadcastRelicGateDamage()
        {
            string message = $"{Component.Keep.Name} is under attack!";

            foreach (GamePlayer player in ClientService.Instance.GetPlayersOfRealm(Realm))
            {
                player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
                player.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            if (Properties.DISCORD_ACTIVE && !string.IsNullOrEmpty(Properties.DISCORD_RVR_WEBHOOK_ID))
                GameRelicPad.BroadcastDiscordRelic(message, Realm, Component.Keep.Name);
        }

        public override void ModifyAttack(AttackData attackData)
        {
            if (attackData.DamageType is eDamageType.GM)
                return;

            int toughness = Component.Keep is GameKeepTower ? Properties.SET_TOWER_DOOR_TOUGHNESS : Properties.SET_KEEP_DOOR_TOUGHNESS;
            GameLiving source = attackData.Attacker;
            int baseDamage = attackData.Damage;
            int styleDamage = attackData.StyleDamage;
            int criticalDamage = attackData.CriticalDamage;

            if (source is GamePlayer)
            {
                baseDamage = GetAdjustedDamage(baseDamage, toughness, Component.Keep.Level);
                styleDamage = GetAdjustedDamage(styleDamage, toughness, Component.Keep.Level);
                criticalDamage = GetAdjustedDamage(criticalDamage, toughness, Component.Keep.Level);
            }
            else if (source is GameNPC npcSource)
            {
                if (!Properties.DOORS_ALLOWPETATTACK)
                {
                    attackData.AttackResult = eAttackResult.NotAllowed_ServerRules;
                    baseDamage = 0;
                    styleDamage = 0;
                    criticalDamage = 0;
                }
                else
                {
                    baseDamage = GetAdjustedDamage(baseDamage, toughness, Component.Keep.Level);
                    styleDamage = GetAdjustedDamage(styleDamage, toughness, Component.Keep.Level);
                    criticalDamage = GetAdjustedDamage(criticalDamage, toughness, Component.Keep.Level);

                    if (npcSource.Brain is AI.Brain.IControlledBrain brain && brain.Owner is GamePlayer player)
                    {
                        double multiplier = (eCharacterClass) player.CharacterClass.ID is eCharacterClass.Theurgist or eCharacterClass.Animist ?
                            Properties.PET_SPAM_DAMAGE_MULTIPLIER :
                            Properties.PET_DAMAGE_MULTIPLIER;

                        baseDamage = (int) (baseDamage * multiplier);
                        styleDamage = (int) (styleDamage * multiplier);
                        criticalDamage = (int) (criticalDamage * multiplier);
                    }
                }
            }

            attackData.Damage = baseDamage;
            attackData.StyleDamage = styleDamage;
            attackData.CriticalDamage = criticalDamage;

            static int GetAdjustedDamage(int damage, int toughness, int level)
            {
                return (damage - damage * 5 * level / 100) * toughness / 100;
            }
        }

        /// <summary>
        /// This function is called from the ObjectInteractRequestHandler
        /// It teleport player in the keep if player and keep have the same realm
        /// </summary>
        /// <param name="player">GamePlayer that interacts with this object</param>
        /// <returns>false if interaction is prevented</returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (player.IsMezzed)
            {
                player.Out.SendMessage("You are mesmerized!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.IsStunned)
            {
                player.Out.SendMessage("You are stunned!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (!GameServer.KeepManager.IsEnemy(this, player) || player.Client.Account.PrivLevel != 1)
            {
                int keepz = Z, distance = 0;

                //calculate distance
                //normal door
                if (DoorIndex == 1)
                    distance = 150;
                //side or internal door
                else
                    distance = 100;

                //calculate Z
                if (Component.Keep is GameKeepTower && !Component.Keep.IsPortalKeep)
                {
                    //when entering a tower, we need to raise Z
                    //portal keeps are considered towers too, so we check component count
                    if (IsObjectInFront(player, 180))
                    {
                        if (DoorId == 1)
                            keepz = Z + 83;
                        else
                            distance = 150;
                    }
                }
                else
                {
                    //when entering a keeps inner door, we need to raise Z
                    if (IsObjectInFront(player, 180))
                    {
                        //To find out if a door is the keeps inner door, we compare the distance between
                        //the component for the keep and the component for the gate
                        int keepdistance = int.MaxValue;
                        int gatedistance = int.MaxValue;

                        foreach (GameKeepComponent c in Component.Keep.KeepComponents)
                        {
                            if ((GameKeepComponent.eComponentSkin)c.Skin == GameKeepComponent.eComponentSkin.Keep)
                                keepdistance = GetDistanceTo(c);

                            if ((GameKeepComponent.eComponentSkin)c.Skin == GameKeepComponent.eComponentSkin.Gate)
                                gatedistance = GetDistanceTo(c);

                            //when these are filled we can stop the search
                            if (keepdistance != int.MaxValue && gatedistance != int.MaxValue)
                                break;
                        }

                        if (DoorIndex == 1 && keepdistance < gatedistance)
                            keepz = Z + 92;//checked in game with lvl 1 keep
                    }
                }

                Point2D keepPoint;

                //calculate x y
                if (IsObjectInFront(player, 180))
                    keepPoint = GetPointFromHeading(Heading, -distance );
                else
                    keepPoint = GetPointFromHeading(Heading, distance );

                //move player
                player.MoveTo(CurrentRegionID, keepPoint.X, keepPoint.Y, keepz, player.Heading);
            }

            return base.Interact(player);
        }

        public override IList GetExamineMessages(GamePlayer player)
        {
            /*
             * You select the Keep Gate. It belongs to your realm.
             * You target [the Keep Gate]
             * 
             * You select the Keep Gate. It belongs to an enemy realm and can be attacked!
             * You target [the Keep Gate]
             * 
             * You select the Postern Door. It belongs to an enemy realm!
             * You target [the Postern Door]
             */

            IList list = base.GetExamineMessages(player);
            string text = "You select the " + Name + ".";

            if (!GameServer.KeepManager.IsEnemy(this, player))
                text = text + " It belongs to your realm.";
            else
            {
                if (IsAttackableDoor)
                    text = text + " It belongs to an enemy realm and can be attacked!";
                else
                    text = text + " It belongs to an enemy realm!";
            }

            list.Add(text);

            ChatUtil.SendDebugMessage(player, "Health = " + Health);

            if (IsAttackableDoor)
            {
                // Attempt to fix issue where some players see door as closed when it should be broken open
                // if you target a door it will re-broadcast it's state

                if (Health <= 0 && State != eDoorState.Open)
                    State = eDoorState.Open;

                ClientService.UpdateObjectForPlayer(player, this);
            }

            return list;
        }

        public override string GetName(int article, bool firstLetterUppercase)
        {
            return "the " + base.GetName(article, firstLetterUppercase);
        }

        public override void StartHealthRegeneration()
        {
            if (!IsAttackableDoor)
                return;

            if ((m_repairTimer != null && m_repairTimer.IsAlive) || Health >= MaxHealth)
                return;

            m_repairTimer = new ECSGameTimer(this);
            m_repairTimer.Callback = new ECSGameTimer.ECSTimerCallback(RepairTimerCallback);
            m_repairTimer.Start(REPAIR_INTERVAL);
        }

        public void DeleteObject()
        {
            RemoveTimers();

            if (Component != null)
            {
                Component.Keep?.Doors.Remove(ObjectID.ToString());
                Component.Delete();
            }

            Component = null;
            Position = null;
            base.Delete();
            CurrentRegion = null;
        }

        public virtual void RemoveTimers()
        {
            if (m_repairTimer != null)
            {
                m_repairTimer.Stop();
                m_repairTimer = null;
            }
        }

        #endregion

        #region Save/load DB

        /// <summary>
        /// save the keep door object in DB
        /// </summary>
        public override void SaveIntoDatabase()
        {
            // I guess we're not creating doors automatically.
            if (DbDoor == null)
                return;

            // `DbDoor.Health` isn't updated automatically.
            if (DbDoor.Health != Health)
                DbDoor.Health = Health;

            GameServer.Database.SaveObject(DbDoor);
        }

        /// <summary>
        /// load the keep door object from DB object
        /// </summary>
        /// <param name="obj"></param>
        public override void LoadFromDatabase(DataObject obj)
        {
            if (obj is not DbDoor dbDoor)
                return;

            base.LoadFromDatabase(obj);

            if (ObjectState is not eObjectState.Active)
                return;

            foreach (AbstractArea area in CurrentAreas)
            {
                if (area is KeepArea keepArea)
                {
                    string sKey = dbDoor.InternalID.ToString();

                    if (!keepArea.Keep.Doors.ContainsKey(sKey))
                    {
                        Component = new GameKeepComponent();
                        Component.Keep = keepArea.Keep;
                        keepArea.Keep.Doors.Add(sKey, this);
                    }

                    break;
                }
            }

            // HealthPercent relies on MaxHealth, which returns 0 if used before adding the door to the world and setting Component.Keep
            // Keep doors are always closed if they have more than DOOR_CLOSE_THRESHOLD% health. Otherwise the value is retrieved from the DB.
            // Postern doors are always closed.
            if (IsPostern || HealthPercent > DOOR_CLOSE_THRESHOLD)
                State = eDoorState.Closed;
            else
                State = (eDoorState) dbDoor.State;

            StartHealthRegeneration();
            DoorMgr.RegisterDoor(this);
        }

        public virtual void LoadFromPosition(DbKeepPosition pos, GameKeepComponent component)
        {
            m_templateID = pos.TemplateID;
            m_component = component;

            PositionMgr.LoadKeepItemPosition(pos, this);
            component.Keep.Doors[m_templateID] = this;

            _oldMaxHealth = MaxHealth;
            m_health = MaxHealth;
            m_name = "Keep Door";
            _oldHealthPercent = HealthPercent;
            DoorId = GenerateDoorID();
            m_model = 0xFFFF;
            State = eDoorState.Closed;

            if (AddToWorld())
            {
                StartHealthRegeneration();
                DoorMgr.RegisterDoor(this);
            }
            else
                log.Error("Failed to load keep door from keepposition_id =" + pos.ObjectId + ". Component SkinID=" + component.Skin + ". KeepID=" + component.Keep.KeepID);
        }

        public void MoveToPosition(DbKeepPosition position) { }

        public int GenerateDoorID()
        {
            int doortype = 7;
            int ownerKeepID = 0;
            int towerIndex = 0;

            if (m_component.Keep is GameKeepTower)
            {
                GameKeepTower tower = m_component.Keep as GameKeepTower;

                if (tower.Keep != null)
                    ownerKeepID = tower.Keep.KeepID;
                else
                    ownerKeepID = tower.OwnerKeepID;

                towerIndex = tower.KeepID >> 8;
            }
            else
                ownerKeepID = m_component.Keep.KeepID;

            int componentID = m_component.ID;

            //index not sure yet
            int doorIndex = Position.TemplateType;
            int id = 0;
            //add door type
            id += doortype * 100000000;
            id += ownerKeepID * 100000;
            id += towerIndex * 10000;
            id += componentID * 100;
            id += doorIndex;
            return id;
        }

        #endregion

        /// <summary>
        /// This function is called when door "die" to open door
        /// </summary>
        public override void Die(GameObject killer)
        {
            base.Die(killer);

            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                player.Out.SendMessage($"The {Name} is broken!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            State = eDoorState.Open;
            BroadcastDoorStatus();
            SaveIntoDatabase();
        }

        /// <summary>
        /// This method is called when door is repair or keep is reset
        /// </summary>
        public virtual void CloseDoor()
        {
            State = eDoorState.Closed;
            BroadcastDoorStatus();
        }

        /// <summary>
        /// boradcast the door status to all player near the door
        /// </summary>
        public virtual void BroadcastDoorStatus()
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                ClientService.UpdateObjectForPlayer(player, this);
                player.Out.SendDoorState(CurrentRegion, this);
            }
        }

        protected ECSGameTimer m_repairTimer;
        protected const int REPAIR_INTERVAL = 30 * 60 * 1000;

        public int RepairTimerCallback(ECSGameTimer timer)
        {
            if (Component == null || Component.Keep == null)
                return 0;

            if (HealthPercent >= 100)
                return 0;
            else if (!Component.Keep.InCombat)
                Repair(MaxHealth / 100 * 5);

            return REPAIR_INTERVAL;
        }

        /// <summary>
        /// This Function is called when door has been repaired
        /// </summary>
        /// <param name="amount">how many HP is repaired</param>
        public void Repair(int amount)
        {
            Health = Math.Min(Health + amount, MaxHealth);

            if (HealthPercent > 25)
                _relicMessage25 = false;
            if (HealthPercent > 50)
                _relicMessage50 = false;
            if (HealthPercent > 75)
                _relicMessage75 = false;

            BroadcastDoorStatus();
            SaveIntoDatabase();
        }
        /// <summary>
        /// This Function is called when keep is taken to repair door
        /// </summary>
        /// <param name="realm">new realm of keep taken</param>
        public void Reset(eRealm realm)
        {
            Realm = realm;
            Health = MaxHealth;
            _oldHealthPercent = HealthPercent;
            _relicMessage25 = false;
            _relicMessage50 = false;
            _relicMessage75 = false;
            CloseDoor();
            SaveIntoDatabase();
        }

        /*
         * Note that 'enter' and 'exit' commands will also work at these doors.
         */

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            if (source is GamePlayer == false)
                return false;

            str = str.ToLower();

            if (str.Contains("enter") || str.Contains("exit"))
                Interact(source as GamePlayer);
            return true;
        }

        public override bool SayReceive(GameLiving source, string str)
        {
            if (!base.SayReceive(source, str))
                return false;

            if (source is GamePlayer == false)
                return false;

            str = str.ToLower();

            if (str.Contains("enter") || str.Contains("exit"))
                Interact(source as GamePlayer);
            return true;
        }
    }
}
