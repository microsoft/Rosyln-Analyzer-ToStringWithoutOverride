using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ImplicitStringConversionAnalyzer
{
    public class InterpolatedStringImplicitToStringAnalyzer
    {
        public const string DiagnosticId = "InterpolatedStringImplicitToStringAnalyzer";
        private const string Category = "Naming";

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.InterpolatedStringImplicitToStringTitle), Resources.ResourceManager, typeof (Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.InterpolatedStringImplicitToStringMessageFormat), Resources.ResourceManager,
                typeof (Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.InterpolatedStringImplicitToStringDescription), Resources.ResourceManager,
                typeof (Resources));

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description);

        private readonly SemanticModelAnalysisContext context;
        private readonly TypeInspection typeInspection;

        public InterpolatedStringImplicitToStringAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            typeInspection = new TypeInspection(context.SemanticModel);
        }

        internal static void Run(SemanticModelAnalysisContext context)
        {
            new InterpolatedStringImplicitToStringAnalyzer(context).Run();
        }

        private void Run()
        {
            var expressions =
                context.SemanticModel.SyntaxTree.GetRoot()
                    .DescendantNodesAndSelf()
                    .OfType<InterpolatedStringExpressionSyntax>();


            foreach (var expression in expressions)
            {
                foreach (var part in expression.Contents.OfType<InterpolationSyntax>())
                {
                    var typeInfo = context.SemanticModel.GetTypeInfo(part.Expression);

                    if (typeInspection.IsReferenceTypeWithoutOverridenToString(typeInfo))
                    {
                        ReportDiagnostic(part.Expression, typeInfo);
                    }
                }
            }
        }
        
        private void ReportDiagnostic(ExpressionSyntax expression, TypeInfo typeInfo)
        {
            var diagnostic = Diagnostic.Create(Rule, expression.GetLocation(), typeInfo.Type.ToDisplayString());

            context.ReportDiagnostic(diagnostic);
        }
    }
}