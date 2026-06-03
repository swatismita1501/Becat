namespace EcatDesktop.Models
{
    public sealed class JobStepDefinition
    {
        public JobStepDefinition(string methodName, string description)
        {
            MethodName = methodName;
            Description = description;
        }

        public string MethodName { get; private set; }

        public string Description { get; private set; }
    }
}
