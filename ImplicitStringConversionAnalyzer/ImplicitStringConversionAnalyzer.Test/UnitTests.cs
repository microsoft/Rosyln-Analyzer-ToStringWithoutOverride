using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using ImplicitStringConversionAnalyzer;

namespace ImplicitStringConversionAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }
        
        [TestMethod]
        public void DisallowImplicitRightHandObjectToStringConversionForConcatenation()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = """" + new object();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ImplicitStringConversionAnalyzer",
                Message = String.Format("Expression '{0}' will be implicitly converted to a string", "new object()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 31)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void DisallowImplicitLeftHandObjectToStringConversionForConcatenation()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = new object() + """";
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ImplicitStringConversionAnalyzer",
                Message = String.Format("Expression '{0}' will be implicitly converted to a string", "new object()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 26)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void AllowImplicitIntegerToStringConversionForConcatenation()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = """" + 0;
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ImplicitStringConversionAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ImplicitStringConversionAnalyzerAnalyzer();
        }
    }
}