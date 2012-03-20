using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler
{
    namespace Support
    {
        namespace Errors
        {
            namespace Compilation
            {
                public class LexicalError : Exception
                {
                    public LexicalError(string message)
                        : base(message) { }
                }
            }
        }
    }
}
