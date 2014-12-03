using System;
using System.Diagnostics;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Bugsnag.Core.Test
{
    public class ExceptionParserTests
    {
        #region Test Classes and Methods

        public class TestClass
        {

        }

        public int Calculate(string string1, int integer2)
        {
            return 0;
        }

        public void Addition(long long1, long long2)
        {
            return;
        }

        public TestClass Subtraction(params uint[] unsigned)
        {
            return null;
        }

        public void Multiplication<T>(TestClass testClass)
        {
            return;
        }

        public void Nothing()
        {
            return;
        }

        public StackTrace CreateTrace()
        {
            return new StackTrace(true);
        }

        #endregion

        [Theory]
        [InlineData("Calculate", "Bugsnag.Core.Test.ExceptionParserTests.Calculate(String string1, Int32 integer2)")]
        [InlineData("Addition", "Bugsnag.Core.Test.ExceptionParserTests.Addition(Int64 long1, Int64 long2)")]
        [InlineData("Subtraction", "Bugsnag.Core.Test.ExceptionParserTests.Subtraction(UInt32[] unsigned)")]
        [InlineData("Nothing", "Bugsnag.Core.Test.ExceptionParserTests.Nothing()")]
        [InlineData("Multiplication", "Bugsnag.Core.Test.ExceptionParserTests.Multiplication(TestClass testClass)")]
        public void GenerateMethodSignature_GeneratesSignaturesCorrectly(string methodName, string expSignature)
        {
            // Arrange
            var method = typeof(ExceptionParserTests).GetMethod(methodName);

            // Act
            var actSignature = ExceptionParser.GenerateMethodSignature(method);

            // Assert
            Assert.Equal(expSignature, actSignature);
        }

        [Fact]
        public void GenerateMethodSignature_NullMethodReturnsNullSignature()
        {
            // Act
            var actSignature = ExceptionParser.GenerateMethodSignature(null);

            // Assert
            Assert.Null(actSignature);
        }

        [Fact]
        public void GenerateExceptionInfo_NullIfNoStackTraceOnExceptionAndNoCallStack()
        {
            // Arrange
            var testConfig = new Mock<IConfiguration>();
            var exp = new SystemException("System Error");

            // Act
            var actInfo = ExceptionParser.GenerateExceptionInfo(exp, null, testConfig.Object);

            // Assert
            Assert.Null(actInfo);
        }

        [Fact]
        public void GenerateExceptionInfo_NullIfExceptionIsNull()
        {
            // Arrange
            var testConfig = new Mock<IConfiguration>();

            // Act
            var actInfoWithCall = ExceptionParser.GenerateExceptionInfo(null, new StackTrace(), testConfig.Object);
            var actInfoWithoutCall = ExceptionParser.GenerateExceptionInfo(null, null, testConfig.Object);

            // Assert
            Assert.Null(actInfoWithCall);
            Assert.Null(actInfoWithoutCall);
        }

        [Theory]
        [InlineData(true, false, false, false)]
        [InlineData(true, true, false, true)]
        [InlineData(true, false, true, true)]
        [InlineData(true, true, true, true)]
        [InlineData(false, false, false, false)]
        [InlineData(false, true, false, true)]
        [InlineData(false, false, true, true)]
        [InlineData(false, true, true, true)]
        public void GenerateExceptionInfo_GeneratesInfoWithExceptionStackTrace(
            bool useCallStack,
            bool autoInProject,
            bool projectNamespace,
            bool expInProject)
        {
            // Arrange
            var testConfig = new Mock<IConfiguration>();
            testConfig.Setup(x => x.AutoDetectInProject).Returns(autoInProject);
            testConfig.Setup(x => x.IsInProjectNamespace("Bugsnag.Core.Test.ExceptionParserTests"))
                .Returns(projectNamespace);
            testConfig.Setup(x => x.RemoveFileNamePrefix(It.IsAny<string>())).Returns((string x) => x);

            RankException testExp;
            try
            {
                throw new RankException("Test Rank Exp");
            }
            catch (Exception exp)
            {
                testExp = exp as RankException;
            }

            // Act
            var actInfo = ExceptionParser.GenerateExceptionInfo(testExp, useCallStack ? new StackTrace() : null, testConfig.Object);

            // Assert
            Assert.NotNull(actInfo);
            Assert.Equal("RankException", actInfo.ExceptionClass);
            Assert.True(actInfo.Description.Contains(testExp.Message));
            Assert.True(actInfo.StackTrace[0].File.EndsWith("ExceptionParserTests.cs"));
            Assert.True(actInfo.StackTrace[0].Method.Contains("GenerateExceptionInfo_GeneratesInfoWithExceptionStackTrace"));
            Assert.Equal(expInProject, actInfo.StackTrace[0].InProject);
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, true, true)]
        public void GenerateExceptionInfo_GeneratesInfoWithNoExceptionStackTraceButHasCallStack(
            bool autoInProject,
            bool projectNamespace,
            bool expInProject)
        {
            // Arrange
            var testConfig = new Mock<IConfiguration>();
            testConfig.Setup(x => x.AutoDetectInProject).Returns(autoInProject);
            testConfig.Setup(x => x.IsInProjectNamespace("Bugsnag.Core.Test.ExceptionParserTests"))
                .Returns(projectNamespace);
            testConfig.Setup(x => x.RemoveFileNamePrefix(It.IsAny<string>())).Returns((string x) => x);

            var testExp = new RankException("Test Rank Exp");

            // Act
            var actInfo = ExceptionParser.GenerateExceptionInfo(testExp, CreateTrace(), testConfig.Object);

            // Assert
            Assert.NotNull(actInfo);
            Assert.Equal("RankException", actInfo.ExceptionClass);
            Assert.True(actInfo.Description.Contains(testExp.Message));
            Assert.True(actInfo.Description.Contains("[CALL STACK]"));
            Assert.True(actInfo.StackTrace[0].File.EndsWith("ExceptionParserTests.cs"));
            Assert.True(actInfo.StackTrace[0].Method.Contains("CreateTrace"));
            Assert.Equal(expInProject, actInfo.StackTrace[0].InProject);
            Assert.True(actInfo.StackTrace[1].File.EndsWith("ExceptionParserTests.cs"));
            Assert.True(actInfo.StackTrace[1].Method.Contains("GenerateExceptionInfo_GeneratesInfoWithNoExceptionStackTraceButHasCallStack"));
            Assert.Equal(expInProject, actInfo.StackTrace[0].InProject);
        }
    }
}
