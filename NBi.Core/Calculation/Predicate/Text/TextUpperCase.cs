﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBi.Core.Calculation.Predicate.Text
{
    class TextUpperCase : IPredicate
    {
        public bool Apply(object x)
        {
            return (x as string).ToUpperInvariant() == (x as string) || (x as string) == "(empty)" || (x as string) == "(null)";
        }
    }
}
