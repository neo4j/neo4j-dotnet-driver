using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class StatementRunnerTests
    {
        public class MyStatementRunner : StatementRunner
        {
            public string Statement { private set; get; }
            public IDictionary<string, object> Parameters { private set; get; } 

            public override IStatementResult Run(string statement, IDictionary<string,object> parameters = null)
            {
                Statement = statement;
                Parameters = parameters;
                return null; // nah, I do not care
            }

            public MyStatementRunner() : base(null)
            {
            }
        }

        public class RunStatementMethod
        {
            [Fact]
            public void ShouldInvokeRunStringMethod()
            {
                var stRunner = new MyStatementRunner();
                stRunner.Run(new Statement("lalalala"));
                stRunner.Statement.Should().Be("lalalala");
                stRunner.Parameters.Should().BeEmpty();
            }

            [Fact]
            public void ShouldInvokeRunStringMethodWithParameters()
            {
                var stRunner = new MyStatementRunner();
                stRunner.Run("buibuibui", new {Age = 20, Name = "buibuibui", Hobby = new []{"painting", "skiing"}});
                stRunner.Statement.Should().Be("buibuibui");

                var parameters = stRunner.Parameters;

                parameters.Count.Should().Be(3);
                parameters["Age"].Should().Be(20);
                parameters["Name"].Should().Be("buibuibui");

                var hobby = parameters["Hobby"].As<List<string>>();
                hobby.Count.Should().Be(2);
                hobby.Contains("painting").Should().BeTrue();
                hobby.Contains("skiing").Should().BeTrue();
            }
        }
    }
}