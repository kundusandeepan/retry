using System.Diagnostics.CodeAnalysis;

namespace rnd001.Customization
{
    public class LoggerService
    {
        public void Log(string? value){
            Console.WriteLine(value);
        }
        public void Log([StringSyntax("CompositeFormat")] string format, object? arg0){
            Console.WriteLine(format, arg0);
        }
        public void Log([StringSyntax("CompositeFormat")] string format, params object?[]? arg){
            Console.WriteLine(format, arg);
        }
        // public void Log([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2){
        //     Console.Write(format, arg0, arg1, arg2);
        // }

    }
}