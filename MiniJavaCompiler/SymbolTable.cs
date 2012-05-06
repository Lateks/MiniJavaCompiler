using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJavaCompiler
{
    namespace Support
    {
        public class SymbolTable
        {
            private Dictionary<string, Symbol> symbolTable;
            private SymbolTable outerScope;

            public SymbolTable(SymbolTable outerScope)
            {
                symbolTable = new Dictionary<string, Symbol>();
                this.outerScope = outerScope;
            }

            public void define(Symbol sym)
            {
                symbolTable.Add(sym.Name, sym);
            }

            public Symbol resolve(string name)
            {
                try
                {
                    return symbolTable[name];
                }
                catch
                {
                    if (outerScope == null)
                        return null;
                    else
                        return outerScope.resolve(name);
                }
            }
        }

        public class Symbol
        {
            public string Name
            {
                get;
                private set;
            }
            public string Type
            {
                get;
                private set;
            }

            public Symbol(string name, string type)
            {
                Name = name;
                Type = type;
            }
        }
    }
}
