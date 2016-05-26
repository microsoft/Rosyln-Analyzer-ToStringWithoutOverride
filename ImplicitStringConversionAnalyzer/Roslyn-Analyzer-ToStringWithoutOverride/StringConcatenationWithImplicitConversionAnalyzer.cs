using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ImplicitStringConversionAnalyzer
{
    /// <summary>
    ///     Warns about objects being used in
    ///     <a href="https://msdn.microsoft.com/en-us/library/aa691375(v=vs.71).aspx">string concatenation</a> that do not have
    ///     an overriden ToString() method
    /// </summary>
    public class StringConcatenationWithImplicitConversionAnalyzer
    {
        public const string DiagnosticId = "ImplicitStringConversionAnalyzer";
        private const string Category = "Naming";

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.StringConcatenationWithImplicitConversionTitle), Resources.ResourceManager, typeof (Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.StringConcatenationWithImplicitConversionMessageFormat), Resources.ResourceManager,
                typeof (Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.StringConcatenationWithImplicitConversionDescription), Resources.ResourceManager,
                typeof (Resources));

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description);

        private readonly SemanticModelAnalysisContext context;
        private readonly TypeInspection typeInspection;

        public StringConcatenationWithImplicitConversionAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            typeInspection = new TypeInspection(context.SemanticModel);
        }

        internal static void Run(SemanticModelAnalysisContext context)
        {
            new StringConcatenationWithImplicitConversionAnalyzer(context).Run();
        }

        private void Run()
        {
            var binaryAddExpressions =
                context.SemanticModel.SyntaxTree.GetRoot()
                    .DescendantNodesAndSelf()
                    .OfType<BinaryExpressionSyntax>()
                    .Where(IsAddExpression);

            foreach (var binaryAddExpression in binaryAddExpressions)
            {
                var left = context.SemanticModel.GetTypeInfo(binaryAddExpression.Left);
                var right = context.SemanticModel.GetTypeInfo(binaryAddExpression.Right);

                if (typeInspection.IsStringType(left) && typeInspection.IsReferenceTypeWithoutOverridenToString(right))
                {
                    ReportDiagnostic(binaryAddExpression.Right, right);
                }
                else if (typeInspection.IsReferenceTypeWithoutOverridenToString(left) && typeInspection.IsStringType(right))
                {
                    ReportDiagnostic(binaryAddExpression.Left, left);
                }
            }
        }

        private static bool IsAddExpression(BinaryExpressionSyntax node)
        {
            return node.Kind() == SyntaxKind.AddExpression;
        }

        private void ReportDiagnostic(ExpressionSyntax expression, TypeInfo typeInfo)
        {
            var diagnostic = Diagnostic.Create(Rule, expression.GetLocation(), typeInfo.Type.ToDisplayString());

            context.ReportDiagnostic(diagnostic);
        }
    }
}