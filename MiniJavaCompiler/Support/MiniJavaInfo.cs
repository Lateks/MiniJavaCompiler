using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.Support
{
    internal static class MiniJavaInfo
    {
        internal static readonly HashSet<string> BuiltIns = new HashSet<string>(new[] { "int", "boolean" });

        internal static bool IsBuiltinType(string typeName)
        {
            return BuiltIns.Contains(typeName);
        }
    }
}
