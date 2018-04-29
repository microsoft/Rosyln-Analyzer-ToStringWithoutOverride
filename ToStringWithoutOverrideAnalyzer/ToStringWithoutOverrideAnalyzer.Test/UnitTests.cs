using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToStringWithoutOverrideAnalyzer.Test.Verifiers;

namespace ToStringWithoutOverrideAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [TestMethod]
        public void Verify_No_Code_Shows_No_Diagnostics()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void AllowImplicitRightHandObjectToStringConversionForConcatenation()
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
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void AllowImplicitLeftHandObjectToStringConversionForConcatenation()
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
            VerifyCSharpDiagnostic(test);
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

        [TestMethod]
        public void DisallowImplicitCustomObjectToStringConversionForConcatenation()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = """" + new NotConvertableToString();
        }

        class NotConvertableToString
        {
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ImplicitStringConversionAnalyzer",
                Message =
                    "Expression of type 'ConsoleApplication1.Program.NotConvertableToString' will be implicitly converted to a string, but does not override ToString()",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 31)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void AllowImplicitCustomObjectWithOverridenToStringToStringConversionForConcatenation()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = """" + new ConvertableToString();
        }

        class ConvertableToString
        {
            public override string ToString()
            {
                return ""value"";
            }
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void AllowImplicitCustomObjectWithOverridenToStringOnBaseToStringConversionForConcatenation()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = """" + new ConvertableToStringSubclass();
        }

        class ConvertableToString
        {
            public override string ToString()
            {
                return ""value"";
            }
        }

        class ConvertableToStringSubclass : ConvertableToString
        {
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DisallowExplicitCustomClassToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = new NotConvertableToString().ToString();
        }

        class NotConvertableToString
        {
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ExplicitToStringWithoutOverrideAnalyzer",
                Message = "Calling ToString() on object of type 'ConsoleApplication1.Program.NotConvertableToString' but it does not override ToString()",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 26)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void DisallowExplicitCustomStructToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = new NotConvertableToString().ToString();
        }

        struct NotConvertableToString
        {
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ExplicitToStringWithoutOverrideAnalyzer",
                Message = "Calling ToString() on object of type 'ConsoleApplication1.Program.NotConvertableToString' but it does not override ToString()",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 26)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void AllowExplicitToString_For_CustomObjectWithOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = new ConvertableToString().ToString();
        }

        class ConvertableToString
        {
            public override string ToString()
            {
                return ""value"";
            }
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void AllowExplicitToString_For_Interface_With_ToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            IConvertableToString obj = new ConvertableToString();
            string str = obj.ToString();
        }

        interface IConvertableToString
        {
            public string ToString();
        }

        class ConvertableToString
        {
            public override string ToString()
            {
                return ""value"";
            }
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void AllowExplicitToString_On_Object_Type()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = new object().ToString();
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DisallowStringInterpolationArgument_For_CustomObjectWithoutOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var notConvertable = new NotConvertableToString()
            string str = $""{notConvertable}"";
        }

        class NotConvertableToString
        {
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "InterpolatedStringImplicitToStringAnalyzer",
                Message =
                    "Expression of type 'ConsoleApplication1.Program.NotConvertableToString' will be converted to a string, but does not override ToString()",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 9, 29)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void AllowStringInterpolationArgument_For_CustomObjectWithOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var convertable = new ConvertableToString()
            string str = $""{convertable}"";
        }

        class ConvertableToString
        {
            public override string ToString()
            {
                return ""value"";
            }
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DisallowStringFormatArgument_For_CustomObjectWithoutOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = string.Format(""{0}"", new NotConvertableToString());
        }

        class NotConvertableToString
        {
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "StringFormatArgumentImplicitToStringAnalyzer",
                Message = "Calling ToString() on object of type 'ConsoleApplication1.Program.NotConvertableToString' but it does not override ToString()",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 47)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void AllowStringFormatArgument_For_CustomObjectWithOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = string.Format(""{0}"", new ConvertableToString());
        }

        class ConvertableToString
        {
            public override string ToString()
            {
                return ""value"";
            }
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void AllowStringFormatArgument_For_ArrayContainingCustomObjectWithOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = string.Format(""{0}"", new [] { new ConvertableToString() });
        }

        class ConvertableToString
        {
            public override string ToString()
            {
                return ""value"";
            }
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DisallowStringFormatArgument_For_ArrayContainingCustomObjectWithoutOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = string.Format(""{0}"", new [] { new NotConvertableToString() });
        }

        class NotConvertableToString
        {
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "StringFormatArgumentImplicitToStringAnalyzer",
                Message =
                    "Calling ToString() on object of type 'ConsoleApplication1.Program.NotConvertableToString' but it does not override ToString()",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 56)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void AllowStringFormatArgument_For_ObjectTypeArray()
        {
            var test = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            object[] array = new object[] { new object() };
            string str = string.Format(""{0}"", array);
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DisallowConsoleWriteWithoutOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    struct Money {
        public decimal amount;
        public string currency;
    }
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.Out.Write(""I need about {0}"", new Money { amount = 3.50m, currency = ""$"" });
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "ConsoleWriteImplicitToStringAnalyzer",
                Message =
                    "Expression of type 'ConsoleApplication1.Money' will be converted to a string, but does not override ToString()",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 58)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        // NB: This case is used as an example in the README
        [TestMethod]
        public void DisallowConsoleWriteLineWithoutOverridenToString()
        {
            var test = @"
namespace ConsoleApplication1
{
    struct Money {
        public decimal amount;
        public string currency;
    }
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine(""I need about {0}"", new Money { amount = 3.50m, currency = ""$"" });
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "ConsoleWriteImplicitToStringAnalyzer",
                Message =
                    "Expression of type 'ConsoleApplication1.Money' will be converted to a string, but does not override ToString()",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 58)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ToStringWithoutOverrideAnalyzer();
        }
    }
}