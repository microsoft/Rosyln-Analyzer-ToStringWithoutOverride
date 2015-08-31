using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ImplicitStringConversionAnalyzer
{
    /// <summary>
    /// Warns about objects being used in <a href="https://msdn.microsoft.com/en-us/library/aa691375(v=vs.71).aspx">string concatenation</a> that do not have an overriden ToString() method
    /// </summary>
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
            var binaryAddExpressions = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Where(IsAddExpression);

            foreach (var binaryAddExpression in binaryAddExpressions)
            {
                var left = context.SemanticModel.GetTypeInfo(binaryAddExpression.Left);
                var right = context.SemanticModel.GetTypeInfo(binaryAddExpression.Right);

                if (IsStringType(left) && IsReferenceTypeWithoutOverridenToString(right))
                {
                    ReportDiagnostic(binaryAddExpression.Right, right);
                }
                else if (IsReferenceTypeWithoutOverridenToString(left) && IsStringType(right))
                {
                    ReportDiagnostic(binaryAddExpression.Left, left);
                }
            }
        }

        private static bool IsAddExpression(BinaryExpressionSyntax node)
        {
            return node.Kind() == SyntaxKind.AddExpression;
        }

        private bool IsReferenceTypeWithoutOverridenToString(TypeInfo typeInfo)
        {
            return NotStringType(typeInfo) && typeInfo.Type?.IsReferenceType == true && TypeDidNotOverrideToString(typeInfo);
        }

        private void ReportDiagnostic(ExpressionSyntax expression, TypeInfo typeInfo)
        {
            var diagnostic = Diagnostic.Create(Rule, expression.GetLocation(), typeInfo.Type.ToDisplayString());

            context.ReportDiagnostic(diagnostic);
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