using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ULox
{
    public class TestSuiteManager
    {
        private Stack<TestSuite> _testSuiteStack = new Stack<TestSuite>();
        private Dictionary<string, TestSuite> _testSuiteLookup = new Dictionary<string, TestSuite>();

        public bool IsInUserSuite => _testSuiteStack.Count > 1;

        public bool HasNoResults => _testSuiteLookup.Count == 1 && _testSuiteStack.Peek().TestCaseResults.Count == 0;

        public bool HasFailures => _testSuiteLookup.Count(x => x.Value.HasFailures) > 0;

        public bool AutoThrowOnFailingSuite { get; set; } = true;

        public TestSuiteManager()
        {
            SetSuite("anon");
        }

        public void SetSuite(string lexeme)
        {
            TestSuite testSuite;
            if (!_testSuiteLookup.TryGetValue(lexeme, out testSuite))
            {
                testSuite = new TestSuite(lexeme);
                _testSuiteLookup[lexeme] = testSuite;
            }
            _testSuiteStack.Push(testSuite);
        }

        public void EndCurrentSuite()
        {
            var endingSuite = _testSuiteStack.Pop();
            if (AutoThrowOnFailingSuite && endingSuite.HasFailures)
            {
                throw new TestException(endingSuite.GenerateFailureReport());
            }
        }

        public void StartCase(Stmt.TestCase stmt, object valueExpr)
        {
            _testSuiteStack.Peek().Start(stmt, valueExpr);
        }

        public void LoxExceptionWasThrownByCase(LoxException e)
        {
            _testSuiteStack.Peek().LoxExceptionWasThrown(e);
        }

        public void EndingCase()
        {
            _testSuiteStack.Peek().End();
        }


        public string GenerateReport()
        {
            var sb = new StringBuilder();

            if (HasNoResults)
                return "No Testing Report Available.";

            foreach (var testSuite in _testSuiteLookup)
            {
                sb.AppendLine(testSuite.Key);

                foreach (var testCaseRes in testSuite.Value.TestCaseResults)
                {
                    sb.AppendLine(testCaseRes.ToString());
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
