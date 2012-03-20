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
                public class EndlessCommentError : Exception
                {
                    public int Row
                    {
                        get;
                        private set;
                    }
                    public int Col
                    {
                        get;
                        private set;
                    }

                    public EndlessCommentError(string message, int row, int col)
                        : base(message) { }
                }
            }
        }
    }
}
