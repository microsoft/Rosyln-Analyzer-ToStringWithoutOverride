using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ImplicitStringConversionAnalyzer
{
    public class StringConcatenationWithImplicitConversionAnalyzer
    {
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        internal static void Run(SemanticModelAnalysisContext context)
        {
            new StringConcatenationWithImplicitConversionAnalyzer(context).Run();
        }

        public const string DiagnosticId = "ImplicitStringConversionAnalyzer";
        private const string Category = "Naming";
        private readonly SemanticModelAnalysisContext context;
        private readonly INamedTypeSymbol stringType;

        public StringConcatenationWithImplicitConversionAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            this.stringType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_String);
        }
        private void Run()
        {
            var binaryAddExpressions = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Where(node => node.Kind() == SyntaxKind.AddExpression);

            foreach (var binaryAddExpression in binaryAddExpressions)
            {
                var left = context.SemanticModel.GetTypeInfo(binaryAddExpression.Left);
                var right = context.SemanticModel.GetTypeInfo(binaryAddExpression.Right);

                if (left.Type == null || right.Type == null)
                {
                }
                else if (IsStringType(left) && NotStringType(right) && right.Type.IsReferenceType && TypeDidNotOverrideToString(right))
                {
                    var diagnostic = Diagnostic.Create(Rule, binaryAddExpression.Right.GetLocation(), binaryAddExpression.Right.ToString());

                    context.ReportDiagnostic(diagnostic);
                }
                else if (NotStringType(left) && IsStringType(right) && left.Type.IsReferenceType && TypeDidNotOverrideToString(left))
                {
                    var diagnostic = Diagnostic.Create(Rule, binaryAddExpression.Left.GetLocation(), binaryAddExpression.Left.ToString());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool NotStringType(TypeInfo typeInfo)
        {
            return !IsStringType(typeInfo);
        }

        private bool IsStringType(TypeInfo typeInfo)
        {
            return Equals(typeInfo.Type, stringType);
        }

        private static bool TypeDidNotOverrideToString(TypeInfo typeInfo)
        {
            return !TypeHasOverridenToString(typeInfo);
        }

        private static bool TypeHasOverridenToString(TypeInfo typeInfo)
        {
            for (ITypeSymbol type = typeInfo.Type; type?.BaseType != null; type = type.BaseType)
            {
                if (type.GetMembers("ToString").Any())
                {
                    return true;
                }
            }

            return false;
        }
    }
}