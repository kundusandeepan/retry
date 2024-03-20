using System.Runtime.ConstrainedExecution;
using Autofac.Core;
using Autofac.Extras.DynamicProxy;

namespace rnd001.Customization
{
    // [AttributeUsage( AttributeTargets.Interface |AttributeTargets.Class, AllowMultiple = false)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RetryableAttribute :Attribute//: InterceptAttribute //Attribute
    {
        // public static readonly string RETRYABLE = "retryable";
       
        // public RetryableAttribute() : base(RETRYABLE)
        // {
        // }
    }
}