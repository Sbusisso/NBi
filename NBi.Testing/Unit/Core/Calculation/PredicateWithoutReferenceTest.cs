﻿using Moq;
using NBi.Core.Calculation;
using NBi.Core.Calculation.Predicate;
using NBi.Core.ResultSet;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBi.Testing.Unit.Core.Calculation
{
    public class PredicateWithoutReferenceTest
    {
        [Test]
        [TestCase(ComparerType.Null, null)]
        [TestCase(ComparerType.Null, "(null)")]
        [TestCase(ComparerType.Empty, "")]
        [TestCase(ComparerType.Empty, "(empty)")]
        [TestCase(ComparerType.NullOrEmpty, null)]
        [TestCase(ComparerType.NullOrEmpty, "(null)")]
        [TestCase(ComparerType.NullOrEmpty, "")]
        [TestCase(ComparerType.NullOrEmpty, "(empty)")]
        [TestCase(ComparerType.LowerCase, "")]
        [TestCase(ComparerType.LowerCase, "(empty)")]
        [TestCase(ComparerType.LowerCase, "(null)")]
        [TestCase(ComparerType.LowerCase, "abcd1235")]
        [TestCase(ComparerType.UpperCase, "")]
        [TestCase(ComparerType.UpperCase, "(empty)")]
        [TestCase(ComparerType.UpperCase, "(null)")]
        [TestCase(ComparerType.UpperCase, "ABD1235")]
        public void Apply_Text_Success(ComparerType comparerType, object x)
        {
            var info = Mock.Of<IPredicateInfo>(
                    i => i.ColumnType== ColumnType.Text
                    && i.ComparerType == comparerType
                );

            var factory = new PredicateFactory();
            var comparer = factory.Get(info);
            Assert.That(comparer.Apply(x), Is.True);
        }

        [Test]
        [TestCase(ComparerType.LowerCase, "abCD1235")]
        [TestCase(ComparerType.UpperCase, "Abc1235")]
        public void Apply_Text_Failure(ComparerType comparerType, object x)
        {
            var info = Mock.Of<IPredicateInfo>(
                    i => i.ColumnType == ColumnType.Text
                    && i.ComparerType == comparerType
                );

            var factory = new PredicateFactory();
            var comparer = factory.Get(info);
            Assert.That(comparer.Apply(x), Is.False);
        }

        [Test]
        [TestCase(ComparerType.Null, null, true)]
        [TestCase(ComparerType.Null, 1, false)]
        public void Compare_Numeric_Result(ComparerType comparerType, object x, bool result)
        {
            var info = Mock.Of<IPredicateInfo>(
                    i => i.ColumnType == ColumnType.Numeric
                    && i.ComparerType == comparerType
                );

            var factory = new PredicateFactory();
            var comparer = factory.Get(info);
            Assert.That(comparer.Apply(x), Is.EqualTo(result));
        }

        [Test]
        public void Apply_NullDateTime_Success()
        {
            var info = Mock.Of<IPredicateInfo>(
                    i => i.ColumnType == ColumnType.DateTime
                    && i.ComparerType == ComparerType.Null
                );

            var factory = new PredicateFactory();
            var comparer = factory.Get(info);
            Assert.That(comparer.Apply(null), Is.True);
        }

        [Test]
        public void Compare_NotNullDateTime_Failure()
        {
            var info = Mock.Of<IPredicateInfo>(
                    i => i.ColumnType == ColumnType.DateTime
                    && i.ComparerType == ComparerType.Null
                );

            var factory = new PredicateFactory();
            var comparer = factory.Get(info);
            Assert.That(comparer.Apply(new DateTime(2015, 10, 1)), Is.False);
        }

        [Test]
        [TestCase(ComparerType.Null, null, true)]
        [TestCase(ComparerType.Null, "true", false)]
        [TestCase(ComparerType.Null, "(null)", true)]
        public void Compare_Boolean_Success(ComparerType comparerType, object x, bool result)
        {
            var info = Mock.Of<IPredicateInfo>(
                    i => i.ColumnType == ColumnType.Boolean
                    && i.ComparerType == comparerType
                );

            var factory = new PredicateFactory();
            var comparer = factory.Get(info);
            Assert.That(comparer.Apply(x), Is.EqualTo(result));
        }
        
    }
}
