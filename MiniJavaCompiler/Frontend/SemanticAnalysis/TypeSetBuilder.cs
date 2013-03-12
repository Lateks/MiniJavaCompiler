using System.Collections.Generic;
using MiniJavaCompiler.Support;
using MiniJavaCompiler.Support.AbstractSyntaxTree;
using System;

namespace MiniJavaCompiler.FrontEnd.SemanticAnalysis
{
    public partial class SymbolTableBuilder
    {
        private class TypeSetBuilder
        {
            private readonly Program _root;
            private readonly List<string> _types;
            private readonly IErrorReporter _errorReporter;

            public TypeSetBuilder(Program node, IErrorReporter errorReporter)
            {
                _root = node;
                _errorReporter = errorReporter;
                _types = new List<string>();
            }

            public bool BuildTypeSet(out IEnumerable<string> result)
            {
                bool typesOk = Handle(_root.MainClass);
                foreach (var classDecl in _root.Classes)
                {
                    typesOk &= Handle(classDecl);
                }

                result = _types;
                return typesOk;
            }

            public bool Handle(ClassDeclaration node)
            {
                if (NameAlreadyDefined(node.Name))
                {
                    _errorReporter.ReportError(
                        ErrorTypes.ConflictingDefinitions,
                        String.Format("Conflicting definitions for {0}.", node.Name),
                        node);
                    return false;
                }
                _types.Add(node.Name);
                return true;
            }

            private bool NameAlreadyDefined(string name)
            {
                return _types.Contains(name) || MiniJavaInfo.IsBuiltInType(name);
            }
        }
    }
}