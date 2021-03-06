﻿namespace Symtab
{
    using Antlr4.Runtime;

    public class Literal : SymbolWithScope, ISymbol
    {
        public Literal(string n, IToken t, string fixed_value, int ind) : base(n, t)
        {
            index = ind;
            Cleaned = fixed_value;
        }

        public int TypeIndex { get; }

        public string Cleaned { get; protected set; }
    }
}
