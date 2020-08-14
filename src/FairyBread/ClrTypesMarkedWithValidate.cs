using System;
using System.Collections.Concurrent;

namespace FairyBread
{
    internal static class ClrTypesMarkedWithValidate
    {
        internal static readonly ConcurrentDictionary<Type, bool> Cache = new ConcurrentDictionary<Type, bool>();
    }
}
