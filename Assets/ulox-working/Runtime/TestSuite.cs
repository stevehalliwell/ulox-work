using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ULox
{
    public class TestSuite
    {
        private TestCaseResult currentTestCaseResult;
        private List<TestCaseResult> testCaseResults = new List<TestCaseResult>();

        public string Name { get; private set; }

        public TestSuite(string name) 
        {
            Name = name;
        }

        public IReadOnlyList<TestCaseResult> TestCaseResults => testCaseResults.AsReadOnly();

        public bool HasFailures => testCaseResults.Count(x => x.status == TestCaseResult.TestStatus.Failed) > 0;

        public void Start(Stmt.TestCase stmt, object valueExpr)
        {
            currentTestCaseResult = new TestCaseResult();
            testCaseResults.Add(currentTestCaseResult);
            currentTestCaseResult.name = valueExpr != null ? $"{stmt.name.Lexeme}_{valueExpr}" : stmt.name.Lexeme;
            currentTestCaseResult.status = TestCaseResult.TestStatus.Started;
        }

        public void LoxExceptionWasThrown(LoxException e)
        {
            currentTestCaseResult.status = TestCaseResult.TestStatus.Failed;
            currentTestCaseResult.msg = e.Message;
        }

        public void End()
        {
            if (currentTestCaseResult.status == TestCaseResult.TestStatus.Started)
                currentTestCaseResult.status = TestCaseResult.TestStatus.Succeed;
        }

        public string GenerateFailureReport()
        {
            var sb = new StringBuilder();

            var failedResults = testCaseResults.Where(x => x.status == TestCaseResult.TestStatus.Failed);

            sb.AppendLine($"{Name} FAILED {failedResults.Count()} of {testCaseResults.Count}");
            foreach (var result in failedResults)
            {
                sb.AppendLine(result.ToString());
            }

            return sb.ToString();
        }
    }

    public class TestCaseResult
    {
        public enum TestStatus { NotRun, Started, Failed, Succeed, Unknown };
        public string name, msg;
        public TestStatus status = TestStatus.NotRun;

        public override string ToString()
        {
            return $"{name}:{status} - {msg}";
        }
    }
}
