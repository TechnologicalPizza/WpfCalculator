﻿using System;

namespace Miniräknare
{
    [Flags]
    public enum FieldState
    {
        Indeterminate = 0,
        Ok = 1 << 0,
        NestedError = 1 << 1,

        CyclicReference = 1 << 2,
        SyntaxError = 1 << 3,
        UnknownWord = 1 << 4,
        UnknownFunction = 1 << 5,
        
        UnknownWordNested = UnknownWord | NestedError,
        UnknownFunctionNested = UnknownFunction | NestedError,
        SyntaxErrorNested = SyntaxError | NestedError
    }
}
