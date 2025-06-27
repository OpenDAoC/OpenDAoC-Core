using System;
using DOL.Database;
using DOL.Events;

namespace DOL.GS.Interfaces
{
    /// <summary>
    /// Core interface for all game objects - basic identity and state
    /// Following Interface Segregation Principle (ISP) - maximum 5 methods per interface
    /// </summary>
    public interface IGameObject : IPositionable, IIdentifiable, IEventNotifier
    {
        /// <summary>
        /// Current state of the object (Active, Inactive, Deleted)
        /// </summary>
        GameObject.eObjectState ObjectState { get; set; }
        
        /// <summary>
        /// Type of game object (NPC, Player, Item, etc.)
        /// </summary>
        eGameObjectType GameObjectType { get; }
    }

    /// <summary>
    /// Interface for positional data and spatial operations
    /// DAoC Rule: All objects exist in 3D space with heading
    /// </summary>
    public interface IPositionable : IPoint3D
    {
        /// <summary>
        /// Object's facing direction (0-4095, where 0 = North)
        /// </summary>
        ushort Heading { get; set; }
        
        /// <summary>
        /// Current region the object resides in
        /// </summary>
        Region CurrentRegion { get; set; }
        
        /// <summary>
        /// Current region ID for the object
        /// </summary>
        ushort CurrentRegionID { get; set; }
        
        /// <summary>
        /// Current zone within the region
        /// </summary>
        Zone CurrentZone { get; }
        
        /// <summary>
        /// Object's realm affiliation
        /// </summary>
        eRealm Realm { get; set; }
    }

    /// <summary>
    /// Interface for object identity and display properties
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// Unique object identifier within the region
        /// </summary>
        int ObjectID { get; set; }
        
        /// <summary>
        /// Internal unique identifier for database persistence
        /// </summary>
        string InternalID { get; set; }
        
        /// <summary>
        /// Display name of the object
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// Visual model ID for client rendering
        /// </summary>
        ushort Model { get; set; }
        
        /// <summary>
        /// Object level for calculations and display
        /// </summary>
        byte Level { get; set; }
    }

    /// <summary>
    /// Interface for event notification capabilities
    /// </summary>
    public interface IEventNotifier
    {
        /// <summary>
        /// Notify observers of an event with full parameters
        /// </summary>
        void Notify(DOLEvent e, object sender, EventArgs args);
        
        /// <summary>
        /// Notify observers of an event with sender only
        /// </summary>
        void Notify(DOLEvent e, object sender);
        
        /// <summary>
        /// Notify observers of an event without parameters
        /// </summary>
        void Notify(DOLEvent e);
    }

    /// <summary>
    /// Interface for objects that can interact with players
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Handle interaction with a player
        /// </summary>
        bool Interact(GamePlayer player);
        
        /// <summary>
        /// Get the interaction distance for this object
        /// </summary>
        int InteractDistance { get; }
    }

    /// <summary>
    /// Interface for database persistence operations
    /// </summary>
    public interface IPersistable
    {
        /// <summary>
        /// Whether this object should be saved to database
        /// </summary>
        bool SaveInDB { get; set; }
        
        /// <summary>
        /// Save the object to database
        /// </summary>
        void SaveIntoDatabase();
        
        /// <summary>
        /// Load object data from database
        /// </summary>
        void LoadFromDatabase(DataObject obj);
        
        /// <summary>
        /// Delete the object from database
        /// </summary>
        void DeleteFromDatabase();
    }

    /// <summary>
    /// Interface for objects that can receive items and money
    /// DAoC Rule: Item transfer requires validation and event notification
    /// </summary>
    public interface IReceiver
    {
        /// <summary>
        /// Receive an item from a living entity
        /// </summary>
        bool ReceiveItem(GameLiving source, DbInventoryItem item);
        
        /// <summary>
        /// Receive money from a living entity
        /// </summary>
        bool ReceiveMoney(GameLiving source, long money);
    }

    /// <summary>
    /// Interface for objects with ownership
    /// </summary>
    public interface IOwnable
    {
        /// <summary>
        /// Owner identifier for this object
        /// </summary>
        string OwnerID { get; set; }
    }

    /// <summary>
    /// Interface for objects that can be added/removed from world
    /// </summary>
    public interface IWorldObject
    {
        /// <summary>
        /// Add object to the world
        /// </summary>
        bool AddToWorld();
        
        /// <summary>
        /// Remove object from the world
        /// </summary>
        bool RemoveFromWorld();
        
        /// <summary>
        /// Move object to a new position
        /// </summary>
        void MoveTo(int regionID, int x, int y, int z, ushort heading);
        
        /// <summary>
        /// Delete the object permanently
        /// </summary>
        void Delete();
    }
} 