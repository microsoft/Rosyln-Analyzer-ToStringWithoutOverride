using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ToStringWithoutOverrideAnalyzer
{
    public class ExplicitToStringWithoutOverrideAnalyzer
    {
        public const string DiagnosticId = "ExplicitToStringWithoutOverrideAnalyzer";
        private const string Category = "Naming";

        static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.ExplicitToStringWithoutOverrideTitle),
            Resources.ResourceManager,
            typeof(Resources));

        static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(
                nameof(Resources.ExplicitToStringWithoutOverrideMessageFormat),
                Resources.ResourceManager,
                typeof(Resources));

        static readonly LocalizableString Description =
            new LocalizableResourceString(
                nameof(Resources.ExplicitToStringWithoutOverrideDescription),
                Resources.ResourceManager,
                typeof(Resources));

        public static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                ExplicitToStringWithoutOverrideAnalyzer.DiagnosticId,
                ExplicitToStringWithoutOverrideAnalyzer.Title,
                ExplicitToStringWithoutOverrideAnalyzer.MessageFormat,
                ExplicitToStringWithoutOverrideAnalyzer.Category,
                DiagnosticSeverity.Warning,
                true,
                ExplicitToStringWithoutOverrideAnalyzer.Description);

        private readonly SemanticModelAnalysisContext context;
        private readonly TypeInspection typeInspection;

        public ExplicitToStringWithoutOverrideAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            this.typeInspection = new TypeInspection(context.SemanticModel);
        }

        internal static void Run(SemanticModelAnalysisContext context)
        {
            new ExplicitToStringWithoutOverrideAnalyzer(context).Run();
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

                if (memberAccess?.Name.ToString() != "ToString")
                {
                    continue;
                }
                
                if (expression.ArgumentList.Arguments.Any())
                {
                    continue;
                }

                var invocationReturnTypeInfo = this.context.SemanticModel.GetTypeInfo(expression);

                if (!this.typeInspection.IsStringType(invocationReturnTypeInfo))
                {
                    continue;
                }

                var typeInfo2 = this.context.SemanticModel.GetTypeInfo(memberAccess.Expression);

                if (!this.typeInspection.IsTypeWithoutOverridenToString(typeInfo2))
                {
                    continue;
                }

                ReportDiagnostic(expression, typeInfo2);
            }
        }

        private void ReportDiagnostic(ExpressionSyntax expression, TypeInfo typeInfo)
        {
            var diagnostic = Diagnostic.Create(ExplicitToStringWithoutOverrideAnalyzer.Rule, expression.GetLocation(), typeInfo.Type.ToDisplayString());

            this.context.ReportDiagnostic(diagnostic);
        }
    }
}