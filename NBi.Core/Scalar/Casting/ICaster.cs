﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBi.Core.Scalar.Caster
{
    public interface ICaster<T>: ICaster
    {
        new T Execute(object obj);
        bool IsValid(object obj);
    }

    public interface ICaster
    {
        object Execute(object obj);
    }
}
