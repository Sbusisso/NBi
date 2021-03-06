﻿using NBi.Core.ResultSet.Lookup;
using NBi.Framework.Sampling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBi.Framework.FailureMessage.Json
{
    class ReferenceViolationsMessageJson : IReferenceViolationsMessageFormatter
    {
        private readonly IDictionary<string, ISampler<DataRow>> samplers;
        private string actual;
        private string expected;
        private string analysis;

        public ReferenceViolationsMessageJson(IDictionary<string, ISampler<DataRow>> samplers)
        {
            this.samplers = samplers;
        }

        public void Generate(IEnumerable<DataRow> parentRows, IEnumerable<DataRow> childRows, ReferenceViolations violations)
        {
            expected = BuildTable(parentRows, samplers["expected"]);
            actual = BuildTable(childRows, samplers["actual"]);

            var rows = new List<DataRow>();
            foreach (var violation in violations)
                rows = rows.Union(violation.Value).ToList();

            analysis = BuildMultipleTables(
                new[]
                {
                    new Tuple<string, IEnumerable<DataRow>, TableHelperJson>("missing", rows, new CompareTableHelperJson()),
                }, samplers["analysis"]
             );
        }

        private string BuildTable(IEnumerable<DataRow> rows, ISampler<DataRow> sampler)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var writer = new JsonTextWriter(sw);

            var tableHelper = new TableHelperJson();
            tableHelper.Execute(rows, sampler, writer);

            writer.Close();
            return sb.ToString();
        }

        public string RenderActual() => actual;
        public string RenderExpected() => expected;
        public string RenderAnalysis() => analysis;
        public virtual string RenderPredicate() => "Some references are missing and violate referential integrity";
        private string BuildMultipleTables(IEnumerable<Tuple<string, IEnumerable<DataRow>, TableHelperJson>> tableInfos, ISampler<DataRow> sampler)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var writer = new JsonTextWriter(sw);

            writer.WriteStartObject();
            foreach (var item in tableInfos)
            {
                writer.WritePropertyName(item.Item1);
                writer.WriteRawValue(BuildTable(item.Item2, sampler));
            }
            writer.WriteEndObject();

            writer.Close();
            return sb.ToString();
        }

        public string RenderMessage()
        {
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("timestamp");
                writer.WriteValue(DateTime.Now);
                if (!string.IsNullOrEmpty(actual))
                {
                    writer.WritePropertyName("actual");
                    writer.WriteRawValue(actual);
                }
                if (!string.IsNullOrEmpty(expected))
                {
                    writer.WritePropertyName("expected");
                    writer.WriteRawValue(expected);
                }
                if (!string.IsNullOrEmpty(analysis))
                {
                    writer.WritePropertyName("analysis");
                    writer.WriteRawValue(analysis);
                }
                writer.WriteEndObject();
                return sb.ToString();
            }
        }
    }
}
