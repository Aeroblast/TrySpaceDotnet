using System;
using System.Linq;
using System.Reflection;

if (args.Length < 1)
{
    Console.WriteLine("Usage: <ClassName> [MethodName] [parameters...]");
    return;
}

string className = args[0];
string methodName = "Proc";
string[] methodArgs = args.Skip(1).ToArray();

try
{
    Type type = Type.GetType($"{className}");
    if (type == null)
    {
        Console.WriteLine($"Class '{className}' not found.");
        return;
    }

    MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
    if (method == null)
    {
        Console.WriteLine($"Method '{methodName}' not found in class '{className}'.");
        return;
    }

    ParameterInfo[] parameters = method.GetParameters();
    if (parameters.Length != methodArgs.Length)
    {
        Console.WriteLine("Parameter count mismatch.");
        return;
    }

    object[] parsedArgs = new object[methodArgs.Length];
    for (int i = 0; i < methodArgs.Length; i++)
    {
        parsedArgs[i] = Convert.ChangeType(methodArgs[i], parameters[i].ParameterType);
    }

    method.Invoke(null, parsedArgs);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}