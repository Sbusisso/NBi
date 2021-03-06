﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBi.Core;
using NBi.Core.Calculation;
using Moq;
using NBi.Core.Evaluate;
using NBi.Core.ResultSet;
using NBi.Core.ResultSet.Resolver;

namespace NBi.Testing.Unit.Core.Calculation
{
    public class SinglePredicateFilterTest
    {

        [Test]
        public void Apply_Variable_CorrectResult()
        {
            var service = new ObjectsResultSetResolver(
                new ObjectsResultSetResolverArgs(
                    new object[]
                    {
                        new List<object>() { "(null)", 10, 100 },
                        new List<object>() { "(empty)", 2, 75 },
                        new List<object>() { "C", 5, 50 }
                    }));

            var rs = service.Execute();

            var aliases = new[] { Mock.Of<IColumnAlias>(v => v.Column == 0 && v.Name == "a") };

            var predicate = new Mock<IPredicateInfo>();
            predicate.SetupGet(p => p.ColumnType).Returns(ColumnType.Text);
            predicate.SetupGet(p => p.ComparerType).Returns(ComparerType.NullOrEmpty);
            predicate.SetupGet(p => p.Operand).Returns("a");

            var factory = new PredicateFilterFactory();
            var filter = factory.Instantiate(aliases, new IColumnExpression[0], predicate.Object);
            var result = filter.Apply(rs);

            Assert.That(result.Rows, Has.Count.EqualTo(2));
        }

        [Test]
        public void Apply_ColumnIndex_CorrectResult()
        {
            var service = new ObjectsResultSetResolver(
                new ObjectsResultSetResolverArgs(
                    new object[]
                    {
                        new List<object>() { "(null)", 10, 100 },
                        new List<object>() { "(empty)", 2, 75 },
                        new List<object>() { "C", 5, 50 }
                    }));
            var rs = service.Execute();

            var predicate = new Mock<IPredicateInfo>();
            predicate.SetupGet(p => p.ColumnType).Returns(ColumnType.Text);
            predicate.SetupGet(p => p.ComparerType).Returns(ComparerType.NullOrEmpty);
            predicate.SetupGet(p => p.Operand).Returns("#0");

            var factory = new PredicateFilterFactory();
            var filter = factory.Instantiate(new IColumnAlias[0], new IColumnExpression[0], predicate.Object);
            var result = filter.Apply(rs);

            Assert.That(result.Rows, Has.Count.EqualTo(2));
            Assert.That(filter.Describe(), Is.StringContaining("null").And.StringContaining("or empty"));
        }

        [Test]
        public void Apply_ColumnName_CorrectResult()
        {
            var service = new ObjectsResultSetResolver(
                new ObjectsResultSetResolverArgs(
                    new object[]
                    {
                        new List<object>() { "(null)", 10, 100 },
                        new List<object>() { "(empty)", 2, 75 },
                        new List<object>() { "C", 5, 50 }
                    }));
            var rs = service.Execute();
            rs.Table.Columns[0].ColumnName = "first";

            var predicate = new Mock<IPredicateInfo>();
            predicate.SetupGet(p => p.ColumnType).Returns(ColumnType.Text);
            predicate.SetupGet(p => p.ComparerType).Returns(ComparerType.NullOrEmpty);
            predicate.SetupGet(p => p.Operand).Returns("first");

            var factory = new PredicateFilterFactory();
            var filter = factory.Instantiate(new IColumnAlias[0], new IColumnExpression[0], predicate.Object);
            var result = filter.Apply(rs);

            Assert.That(result.Rows, Has.Count.EqualTo(2));
        }

        [Test]
        public void Apply_NestedExpression_CorrectResult()
        {
            var service = new ObjectsResultSetResolver(
                new ObjectsResultSetResolverArgs(
                    new object[]
                        {
                            new List<object>() { 1, 10, 100 },
                            new List<object>() { 2, 2, 75 },
                            new List<object>() { 3, 5, 50 }
                        }));
            var rs = service.Execute();

            var aliases = new List<IColumnAlias>()
            {
                Mock.Of<IColumnAlias>(v => v.Column == 0 && v.Name == "a"),
                Mock.Of<IColumnAlias>(v => v.Column == 1 && v.Name == "b"),
                Mock.Of<IColumnAlias>(v => v.Column == 2 && v.Name == "c")
            };

            var expressions = new List<IColumnExpression>()
            {
                Mock.Of<IColumnExpression>(e => e.Value == "Abs([a])+[e]" && e.Name == "d"),
                Mock.Of<IColumnExpression>(e => e.Value == "[b]*[c]" && e.Name == "e")
            };

            var predicate = new Mock<IPredicateInfo>();
            predicate.SetupGet(p => p.ColumnType).Returns(ColumnType.Numeric);
            predicate.SetupGet(p => p.ComparerType).Returns(ComparerType.MoreThanOrEqual);
            predicate.SetupGet(p => p.Operand).Returns("d");
            predicate.As<IReferencePredicateInfo>().SetupGet(p => p.Reference).Returns((object)200);

            var factory = new PredicateFilterFactory();
            var filter = factory.Instantiate(aliases, expressions, predicate.Object);
            var result = filter.Apply(rs);

            Assert.That(result.Rows, Has.Count.EqualTo(2));
        }

        [Test]
        public void Apply_MixedExpression_CorrectResult()
        {
            var service = new ObjectsResultSetResolver(
                new ObjectsResultSetResolverArgs(
                    new object[]
                    {
                        new List<object>() { 1, 10, 100 },
                        new List<object>() { 2, 2, 75 },
                        new List<object>() { 3, 5, 50 }
                    }));
            var rs = service.Execute();
            rs.Table.Columns[2].ColumnName = "c1";

            var aliases = new List<IColumnAlias>()
            {
                Mock.Of<IColumnAlias>(v => v.Column == 0 && v.Name == "a"),
            };

            var expressions = new List<IColumnExpression>()
            {
                Mock.Of<IColumnExpression>(e => e.Value == "Abs([a])+[e]" && e.Name == "d"),
                Mock.Of<IColumnExpression>(e => e.Value == "[#1]*[c1]" && e.Name == "e")
            };
            
            var predicate = new Mock<IPredicateInfo>();
            predicate.SetupGet(p => p.ColumnType).Returns(ColumnType.Numeric);
            predicate.SetupGet(p => p.ComparerType).Returns(ComparerType.MoreThanOrEqual);
            predicate.SetupGet(p => p.Operand).Returns("d");
            predicate.As<IReferencePredicateInfo>().SetupGet(p => p.Reference).Returns((object)200);

            var factory = new PredicateFilterFactory();
            var filter = factory.Instantiate(aliases, expressions, predicate.Object);
            var result = filter.Apply(rs);

            Assert.That(result.Rows, Has.Count.EqualTo(2));
        }

    }
}
