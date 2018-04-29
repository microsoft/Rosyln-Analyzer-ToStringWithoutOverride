using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ToStringWithoutOverrideAnalyzer
{
    public class ConsoleWriteAnalyzer
    {
        public const string DiagnosticId = "ConsoleWriteImplicitToStringAnalyzer";
        private const string Category = "Naming";

        static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.ConsoleWriteAnalyzerTitle),
            Resources.ResourceManager,
            typeof(Resources));

        static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(
                nameof(Resources.ConsoleWriteAnalyzerMessageFormat),
                Resources.ResourceManager,
                typeof(Resources));

        static readonly LocalizableString Description =
            new LocalizableResourceString(
                nameof(Resources.ConsoleWriteAnalyzerDescription),
                Resources.ResourceManager,
                typeof(Resources));

        public static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                ConsoleWriteAnalyzer.DiagnosticId,
                ConsoleWriteAnalyzer.Title,
                ConsoleWriteAnalyzer.MessageFormat,
                ConsoleWriteAnalyzer.Category,
                DiagnosticSeverity.Warning,
                true,
                ConsoleWriteAnalyzer.Description);

        private readonly SemanticModelAnalysisContext context;
        private readonly TypeInspection typeInspection;
        private readonly INamedTypeSymbol systemConsoleNamedType;
        private readonly INamedTypeSymbol systemIOTextWriterType;

        public ConsoleWriteAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            this.typeInspection = new TypeInspection(context.SemanticModel);
            this.systemConsoleNamedType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Console");
            this.systemIOTextWriterType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.IO.TextWriter");
        }

        internal static void Run(SemanticModelAnalysisContext context)
        {
            new ConsoleWriteAnalyzer(context).Run();
        }

        private void Run()
        {
            var expressions =
                this.context.SemanticModel.SyntaxTree.GetRoot()
                    .DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>();


            foreach (var expression in expressions)
            {
                var memberAccess = expression.Expression as MemberAccessExpressionSyntax;

                if (memberAccess == null)
                {
                    continue;
                }

                if (memberAccess.Name.ToString() != "Write" && memberAccess.Name.ToString() != "WriteLine")
                {
                    continue;
                }

                if (!IsTextWriterOrStaticSystemConsole(memberAccess.Expression)) {
                    continue;
                }

                foreach (var result in this.typeInspection.LackingOverridenToString(expression.ArgumentList))
                {
                    ReportDiagnostic(result.Expression, result.TypeInfo);
                }
            }
        }

        private bool IsTextWriterOrStaticSystemConsole(ExpressionSyntax expression)
        {
            var typeInfo = this.context.SemanticModel.GetTypeInfo(expression);
            return Equals(typeInfo.Type, this.systemIOTextWriterType) || Equals(typeInfo.Type, this.systemConsoleNamedType);
        }

        private void ReportDiagnostic(ExpressionSyntax expression, TypeInfo typeInfo)
        {
            var diagnostic = Diagnostic.Create(ConsoleWriteAnalyzer.Rule, expression.GetLocation(), typeInfo.Type.ToDisplayString());

            this.context.ReportDiagnostic(diagnostic);
        }
    }
}