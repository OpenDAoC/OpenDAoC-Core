namespace DOL.GS
{
    // Interface to be implemented by classes that are to be managed by the entity manager.
    public interface IServiceObject
    {
        public ServiceObjectId ServiceObjectId { get; set; }
    }
}
