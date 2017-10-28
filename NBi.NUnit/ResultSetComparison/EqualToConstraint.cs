﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using NBi.Core;
using NBi.Core.ResultSet;
using NUnitCtr = NUnit.Framework.Constraints;
using NBi.Framework.FailureMessage;
using NBi.Framework;
using NBi.Core.Xml;
using NBi.Core.Transformation;
using NBi.Core.ResultSet.Analyzer;

namespace NBi.NUnit.ResultSetComparison
{
    public class EqualToConstraint : BaseResultSetComparisonConstraint
    {

        public EqualToConstraint(IResultSetService value)
            : base(value)
        { }
    }
}
