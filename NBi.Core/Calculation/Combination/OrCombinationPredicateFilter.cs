﻿using NBi.Core.Calculation.Predicate;
using NBi.Core.Evaluate;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBi.Core.Calculation.Predicate.Combination
{
    class OrCombinationPredicateFilter : BaseCombinationPredicateFilter
    {
        public override string Description { get => "or"; }

        public OrCombinationPredicateFilter(IEnumerable<IColumnAlias> aliases, IEnumerable<IColumnExpression> expressions, IEnumerable<Predication> predications)
            : base(aliases, expressions, predications)
        { }

        protected override bool RowApply(DataRow row)
        {
            var result = false;
            var enumerator = predications.GetEnumerator();
            while (enumerator.MoveNext() && !result)
            {
                var value = GetValueFromRow(row, enumerator.Current.Operand);
                result = enumerator.Current.Predicate.Execute(value);
            }
            return result;
        }
        
    }
}
