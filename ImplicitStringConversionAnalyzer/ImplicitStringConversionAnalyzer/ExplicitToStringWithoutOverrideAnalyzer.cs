using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ImplicitStringConversionAnalyzer
{
    public class ExplicitToStringWithoutOverrideAnalyzer
    {
        public const string DiagnosticId = "ExplicitToStringWithoutOverrideAnalyzer";
        private const string Category = "Naming";

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.ExplicitToStringWithoutOverrideTitle), Resources.ResourceManager, typeof (Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.ExplicitToStringWithoutOverrideMessageFormat), Resources.ResourceManager,
                typeof (Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.ExplicitToStringWithoutOverrideDescription), Resources.ResourceManager,
                typeof (Resources));

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description);

        private readonly SemanticModelAnalysisContext context;
        private readonly INamedTypeSymbol stringType;
        private readonly INamedTypeSymbol objectType;

        public ExplicitToStringWithoutOverrideAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            stringType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            objectType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
        }

        internal static void Run(SemanticModelAnalysisContext context)
        {
            new ExplicitToStringWithoutOverrideAnalyzer(context).Run();
        }

        private void Run()
        {
            var expressions =
                context.SemanticModel.SyntaxTree.GetRoot()
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

                var invocationReturnTypeInfo = context.SemanticModel.GetTypeInfo(expression);

                if (!IsStringType(invocationReturnTypeInfo))
                {
                    continue;
                }

                var typeInfo2 = context.SemanticModel.GetTypeInfo(memberAccess.Expression);

                if (!IsReferenceTypeWithoutOverridenToString(typeInfo2))
                {
                    continue;
                }

                ReportDiagnostic(expression, typeInfo2);
            }
        }

        private bool IsReferenceTypeWithoutOverridenToString(TypeInfo typeInfo)
        {
            return NotStringType(typeInfo) && typeInfo.Type?.IsReferenceType == true && !Equals(typeInfo.Type, objectType) &&
                   TypeDidNotOverrideToString(typeInfo);
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

        private bool TypeDidNotOverrideToString(TypeInfo typeInfo)
        {
            return !TypeHasOverridenToString(typeInfo);
        }

        private bool TypeHasOverridenToString(TypeInfo typeInfo)
        {
            for (var type = typeInfo.Type; type != null && !Equals(type, objectType); type = type.BaseType)
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