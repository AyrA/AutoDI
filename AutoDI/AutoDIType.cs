namespace AyrA.AutoDI
{
    /// <summary>
    /// Sets the type of automatic DI registration that is performed
    /// </summary>
    public enum AutoDIType
    {
        /// <summary>
        /// Registers the type as a singleton service
        /// </summary>
        Singleton,
        /// <summary>
        /// Registers the type as a transient service
        /// </summary>
        Transient,
        /// <summary>
        /// Registers the type as a scoped service
        /// </summary>
        Scoped,
        /// <summary>
        /// Uses the custom registration function to register the type
        /// </summary>
        Custom
    }
}