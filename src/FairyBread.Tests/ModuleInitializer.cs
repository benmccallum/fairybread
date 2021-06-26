using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VerifyTests;

[SuppressMessage("Design", "CA1050:Declare types in namespaces")]
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.ModifySerialization(settings =>
        {
            settings.IgnoreMember<Exception>(_ => _.StackTrace);
        });
    }
}
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif
