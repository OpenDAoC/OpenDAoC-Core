namespace DOL.GS
{
    // Interface to be implemented by classes that are to be handled by `ServiceObjectStore`.
    public interface IServiceObject
    {
        public ServiceObjectId ServiceObjectId { get; }
    }
}
