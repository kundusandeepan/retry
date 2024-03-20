namespace rnd001.Customization
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RetryAttribute : Attribute
    {
        public int RetryCount { get; }

        public RetryAttribute(int retryCount)
        {
            RetryCount = retryCount;
        }
    }
}