﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NBi.Core;
using NBi.Core.DataManipulation;
using NBi.Xml;
using NBi.Xml.Decoration;
using NUnit.Framework;
using NUnitCtr = NUnit.Framework.Constraints;
using NBi.NUnit.Runtime.Configuration;
using NBi.Framework;
using NBi.Core.Configuration;
using NBi.Core.Variable;
using NBi.Xml.Variables;
using NBi.NUnit.Builder.Helper;
using NBi.Core.Scalar.Resolver;
using System.Collections.ObjectModel;
using Ninject;
using NBi.Core.Injection;
using NBi.Core.Configuration.Extension;

namespace NBi.NUnit.Runtime
{
    /// <summary>
    /// This Class is the entry point for NUnit.Framework
    /// In reality the NUnit.Framework think this class is the class containing all the fixtures. But
    /// in reality this class will just call a method to build all the test-cases from the nbits file. 
    /// </summary>
    [TestFixture]
    public class TestSuite
    {
        public bool EnableAutoCategories { get; set; }
        public bool EnableGroupAsCategory { get; set; }
        public bool AllowDtdProcessing { get; set; }
        public string SettingsFilename { get; set; }
        public IConfiguration Configuration { get; set; }
        public static IDictionary<string, ITestVariable> Variables { get; set; }

        internal XmlManager TestSuiteManager { get; private set; }
        internal TestSuiteFinder TestSuiteFinder { get; set; }
        internal ConnectionStringsFinder ConnectionStringsFinder { get; set; }
        internal ConfigurationFinder ConfigurationFinder { get; set; }

        public TestSuite()
        {
            TestSuiteManager = new XmlManager();
            TestSuiteFinder = new TestSuiteFinder();
            ConnectionStringsFinder = new ConnectionStringsFinder();
            ConfigurationFinder = new ConfigurationFinder();
        }

        internal TestSuite(XmlManager testSuiteManager, TestSuiteFinder testSuiteFinder)
        {
            TestSuiteManager = testSuiteManager;
            TestSuiteFinder = testSuiteFinder;
        }

        [Test, TestCaseSource("GetTestCases")]
        public virtual void ExecuteTestCases(TestXml test)
        {
            if (ConfigurationFinder != null)
            {
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceError, string.Format("Loading configuration"));
                var config = ConfigurationFinder.Find();
                ApplyConfig(config);
            }
            else
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceError, $"No configuration-finder found.");

            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceVerbose, $"Test loaded by {GetOwnFilename()}");
            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"Test defined in {TestSuiteFinder.Find()}");
            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"{Variables.Count()} variables defined, {Variables.Count(x => x.Value.IsEvaluated())} already evaluated.");

            if (serviceLocator == null)
                Initialize();

            //check if ignore is set to true
            if (test.IsNotImplemented)
            {
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"Test not-implemented, will be ignored. Reason is '{test.NotImplemented.Reason}'");
                Assert.Ignore(test.IgnoreReason);
            }
            else if (test.Ignore)
            {
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"Test ignored. Reason is '{test.IgnoreReason}'");
                Assert.Ignore(test.IgnoreReason);
            }
            else
            {
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"Running test '{test.Name}' #{test.UniqueIdentifier}");
                ExecuteChecks(test.Condition);
                ExecuteSetup(test.Setup);
                foreach (var tc in test.Systems)
                {
                    foreach (var ctr in test.Constraints)
                    {
                        var factory = new TestCaseFactory(Configuration, Variables, serviceLocator);
                        var testCase = factory.Instantiate(tc, ctr);
                        try
                        {
                            AssertTestCase(testCase.SystemUnderTest, testCase.Constraint, test.Content);
                        }
                        catch
                        {
                            ExecuteCleanup(test.Cleanup);
                            throw;
                        }
                    }
                }
                ExecuteCleanup(test.Cleanup);
            }
        }

        private void ExecuteChecks(ConditionXml check)
        {
            foreach (var predicate in check.Predicates)
            {
                var impl = new DecorationFactory().Get(predicate);
                var isVerified = impl.Validate();
                if (!isVerified)
                {
                    Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"Test ignored. At least one condition was not validated: '{impl.Message}'");
                    Assert.Ignore($"This test has been ignored because following check wasn't successful: {impl.Message}");
                }

            }
        }

        private void ExecuteSetup(SetupXml setup)
        {
            try
            {
                foreach (var command in setup.Commands)
                {
                    var skip = false;
                    if (command is IGroupCommand)
                    {
                        var groupCommand = (command as IGroupCommand);
                        if (groupCommand.RunOnce)
                            skip = groupCommand.HasRun;
                    }

                    if (!skip)
                    {
                        var impl = new DecorationFactory().Get(command);
                        impl.Execute();
                        if (command is IGroupCommand)
                        {
                            var groupCommand = (command as IGroupCommand);
                            groupCommand.HasRun = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleExceptionDuringSetup(ex);
            }
        }

        protected virtual void HandleExceptionDuringSetup(Exception ex)
        {
            var message = string.Format("Exception during the setup of the test: {0}", ex.Message);
            message += "\r\n" + ex.StackTrace;
            if (ex.InnerException != null)
            {
                message += "\r\n" + ex.InnerException.Message;
                message += "\r\n" + ex.InnerException.StackTrace;
            }
            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceWarning, message);
            //If failure during setup then the test is failed!
            Assert.Fail(message);
        }

        private void ExecuteCleanup(CleanupXml cleanup)
        {
            try
            {
                foreach (var command in cleanup.Commands)
                {
                    var impl = new DecorationFactory().Get(command);
                    impl.Execute();
                }
            }
            catch (Exception ex)
            {
                HandleExceptionDuringCleanup(ex);
            }
        }

        protected virtual void HandleExceptionDuringCleanup(Exception ex)
        {
            var message = string.Format("Exception during the cleanup of the test: {0}", ex.Message);
            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceWarning, message);
            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceWarning, "Next cleanup functions are skipped.");
        }

        //public virtual void ExecuteTest(string testSuiteXml)
        //{
        //    Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, testSuiteXml);

        //    byte[] byteArray = Encoding.ASCII.GetBytes(testSuiteXml);
        //    var stream = new MemoryStream(byteArray);
        //    var sr = new StreamReader(stream);

        //    TestSuiteManager.Read(sr);
        //    foreach (var test in TestSuiteManager.TestSuite.Tests)
        //        ExecuteTestCases(test);
        //}

        /// <summary>
        /// Handles the standard assertion and if needed rethrow a new AssertionException with a modified stacktrace
        /// </summary>
        /// <param name="systemUnderTest"></param>
        /// <param name="constraint"></param>
        protected internal void AssertTestCase(Object systemUnderTest, NUnitCtr.Constraint constraint, string stackTrace)
        {
            try
            {
                Assert.That(systemUnderTest, constraint);
            }
            catch (AssertionException ex)
            {
                throw new CustomStackTraceAssertionException(ex, stackTrace);
            }
            catch (NBiException ex)
            {
                throw new CustomStackTraceErrorException(ex, stackTrace);
            }
        }

        public IEnumerable<TestCaseData> GetTestCases()
        {
            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"GetTestCases() has been called");
            //Find configuration of NBi
            if (ConfigurationFinder != null)
            {
                var config = ConfigurationFinder.Find();
                ApplyConfig(config);
            }
            else
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceError, string.Format("No configuration-finder found."));


            //Find connection strings referecned from an external file
            if (ConnectionStringsFinder != null)
                TestSuiteManager.ConnectionStrings = ConnectionStringsFinder.Find();

            //Service Locator
            if (serviceLocator == null)
                Initialize();

            //Build the Test suite
            var testSuiteFilename = TestSuiteFinder.Find();
            TestSuiteManager.Load(testSuiteFilename, SettingsFilename, AllowDtdProcessing);

            //Build the variables
            Variables = BuildVariables(TestSuiteManager.TestSuite.Variables);

            return BuildTestCases();
        }

        private IDictionary<string, ITestVariable> BuildVariables(IEnumerable<GlobalVariableXml> variables)
        {
            var instances = new Dictionary<string, ITestVariable>();
            var resolverFactory = serviceLocator.GetScalarResolverFactory();
            var factory = new TestVariableFactory();

            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"{variables.Count()} variable{(variables.Count() > 1 ? "s" : string.Empty)} defined in the test-suite.");
            foreach (var variable in variables)
            {
                var builder = new ScalarResolverArgsBuilder(serviceLocator);
                builder.Setup(instances); //Pass the catalog that we're building to itself
                if (variable.Script != null)
                    builder.Setup(variable.Script);
                else if (variable.QueryScalar != null)
                {
                    variable.QueryScalar.Settings = TestSuiteManager.TestSuite.Settings;
                    variable.QueryScalar.Default = TestSuiteManager.TestSuite.Settings.GetDefault(Xml.Settings.SettingsXml.DefaultScope.Variable);
                    builder.Setup(variable.QueryScalar);
                }
                builder.Build();
                var args = builder.GetArgs();

                var resolver = resolverFactory.Instantiate<object>(args);

                var instance = factory.Instantiate(resolver);
                instances.Add(variable.Name, instance);
            }

            return instances;
        }

        internal IEnumerable<TestCaseData> BuildTestCases()
        {
            List<TestCaseData> testCasesNUnit = new List<TestCaseData>();

            testCasesNUnit.AddRange(BuildTestCases(TestSuiteManager.TestSuite.Tests));
            testCasesNUnit.AddRange(BuildTestCases(TestSuiteManager.TestSuite.Groups));

            return testCasesNUnit;
        }

        private IEnumerable<TestCaseData> BuildTestCases(IEnumerable<TestXml> tests)
        {
            var testCases = new List<TestCaseData>(tests.Count());

            foreach (var test in tests)
            {
                TestCaseData testCaseDataNUnit = new TestCaseData(test);
                testCaseDataNUnit.SetName(test.GetName());
                testCaseDataNUnit.SetDescription(test.Description);
                foreach (var category in test.Categories)
                    testCaseDataNUnit.SetCategory(CategoryHelper.Format(category));
                foreach (var property in test.Traits)
                    testCaseDataNUnit.SetProperty(property.Name, property.Value);

                //Assign auto-categories
                if (EnableAutoCategories)
                {
                    foreach (var system in test.Systems)
                        foreach (var category in system.GetAutoCategories())
                            testCaseDataNUnit.SetCategory(CategoryHelper.Format(category));
                }
                //Assign auto-categories
                if (EnableGroupAsCategory)
                {
                    foreach (var groupName in test.GroupNames)
                        testCaseDataNUnit.SetCategory(CategoryHelper.Format(groupName));
                }

                testCases.Add(testCaseDataNUnit);
            }
            return testCases;
        }

        private IEnumerable<TestCaseData> BuildTestCases(IEnumerable<GroupXml> groups)
        {
            var testCases = new List<TestCaseData>();

            foreach (var group in groups)
            {
                testCases.AddRange(BuildTestCases(group.Tests));
                testCases.AddRange(BuildTestCases(group.Groups));
            }
            return testCases;
        }

        public void ApplyConfig(NBiSection config)
        {
            EnableAutoCategories = config.EnableAutoCategories;
            EnableGroupAsCategory = config.EnableGroupAsCategory;
            AllowDtdProcessing = config.AllowDtdProcessing;
            SettingsFilename = config.SettingsFilename;

            var notableTypes = new List<Type>();
            var analyzer = new ExtensionAnalyzer();
            var filenames = new List<string>();
            foreach (ExtensionElement extension in config.Extensions)
                filenames.Add(extension.Assembly);
            foreach (var filename in filenames)
                notableTypes.AddRange(analyzer.Execute(filename));

            if (serviceLocator == null)
                Initialize();

            var setupConfiguration = serviceLocator.GetConfiguration();
            setupConfiguration.LoadExtensions(notableTypes);
            setupConfiguration.LoadFailureReportProfile(config.FailureReportProfile);
            Configuration = setupConfiguration;
        }

        private static ServiceLocator serviceLocator;
        public void Initialize()
        {
            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"Initializing service locator ...");
            var stopWatch = new Stopwatch();
            serviceLocator = new ServiceLocator();
            Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"Service locator initialized in {stopWatch.Elapsed:d'.'hh':'mm':'ss'.'fff'ms'}");


            if (ConfigurationFinder != null)
            {
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceError, string.Format("Loading configuration ..."));
                stopWatch.Reset();
                var config = ConfigurationFinder.Find();
                ApplyConfig(config);
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceInfo, $"Configuration loaded in {stopWatch.Elapsed:d'.'hh':'mm':'ss'.'fff'ms'}");
            }
            else
                Trace.WriteLineIf(Extensibility.NBiTraceSwitch.TraceError, $"No configuration-finder found.");
        }


        protected internal string GetOwnFilename()
        {
            //get the full location of the assembly with DaoTests in it
            var fullPath = System.Reflection.Assembly.GetAssembly(typeof(TestSuite)).Location;

            //get the filename that's in
            var fileName = Path.GetFileName(fullPath);

            return fileName;
        }

        protected internal string GetManifestName()
        {
            //get the full location of the assembly with DaoTests in it
            var fullName = System.Reflection.Assembly.GetAssembly(typeof(TestSuite)).ManifestModule.Name;

            return fullName;
        }
    }
}
