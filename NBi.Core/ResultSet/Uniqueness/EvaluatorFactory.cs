﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBi.Core.ResultSet.Uniqueness
{
    public class EvaluatorFactory
    {
        public Evaluator Instantiate(ISettingsResultSet settings)
        {
            if (settings is SettingsIndexResultSet)
                return new IndexEvaluator(settings as SettingsIndexResultSet);
            else if (settings is SettingsNameResultSet)
                return new NameEvaluator(settings as SettingsNameResultSet);
            throw new ArgumentOutOfRangeException();
        }
    }
}
