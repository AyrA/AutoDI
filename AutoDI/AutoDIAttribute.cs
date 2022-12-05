namespace AutoDI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoDIAttribute : Attribute
    {

        public AutoDIAttribute(AutoDIType registrationType, Type interfaceType = null)
        {
            if (!Enum.IsDefined(registrationType))
            {
                throw new ArgumentException($"Enum not defined: {registrationType}");
            }
            RegistrationType = registrationType;
            InterfaceType = interfaceType;
        }


        public AutoDIType RegistrationType { get; }
        public Type InterfaceType { get; }
    }
}