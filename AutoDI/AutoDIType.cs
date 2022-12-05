namespace AutoDI
{
    /// <summary>
    /// Sets the type of automatic DI registration that is performed
    /// </summary>
    public enum AutoDIType
    {
        /// <summary>
        /// No automatic registration.
        /// Same behavior as if <see cref="AutoDIAttribute"/> was not specified at all.
        /// </summary>
        None,
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
        Scoped
    }
}