﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBi.Core.Connection
{
    public interface IConnectionWaitCommand
    {
        string ConnectionString { get; }
        int TimeOut { get; }
    }
}
