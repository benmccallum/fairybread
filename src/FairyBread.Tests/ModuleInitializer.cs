﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VerifyTests;

[SuppressMessage("Design", "CA1050:Declare types in namespaces")]
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.IgnoreStackTrace();
    }
}
