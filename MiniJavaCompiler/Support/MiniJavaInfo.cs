using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler.Support
{
    internal static class MiniJavaInfo
    {
        // TODO: define semantics of operators here?

        internal static readonly string[] BuiltIns = new [] { "int", "boolean" };
        internal static readonly string[] UnaryOperators = new [] { "!" };

        internal static bool IsBuiltinType(string typeName)
        {
            return BuiltIns.Contains(typeName);
        }
    }
}
