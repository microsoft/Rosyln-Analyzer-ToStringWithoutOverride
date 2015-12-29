using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ImplicitStringConversionAnalyzer
{
    public class StringFormatArgumentImplicitToStringAnalyzer
    {
        public const string DiagnosticId = "StringFormatArgumentImplicitToStringAnalyzer";
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

        public StringFormatArgumentImplicitToStringAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            stringType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_String);
        }

        internal static void Run(SemanticModelAnalysisContext context)
        {
            new StringFormatArgumentImplicitToStringAnalyzer(context).Run();
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

                if (memberAccess?.Name.ToString() != "Format")
                {
                    continue;
                }

                var memberAccessOnTypeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);

                if (memberAccessOnTypeInfo.Type.ToString() != "string")
                {
                    continue;
                }

                foreach (var argument in expression.ArgumentList.Arguments.Skip(1))
                {
                    var typeInfo = context.SemanticModel.GetTypeInfo(argument.Expression);

                    if (IsReferenceTypeWithoutOverridenToString(typeInfo))
                    {
                        ReportDiagnostic(argument.Expression, typeInfo);
                    }
                }
            }
        }

        private bool IsReferenceTypeWithoutOverridenToString(TypeInfo typeInfo)
        {
            return NotStringType(typeInfo) && typeInfo.Type?.IsReferenceType == true &&
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

        private static bool TypeDidNotOverrideToString(TypeInfo typeInfo)
        {
            return !TypeHasOverridenToString(typeInfo);
        }

        private static bool TypeHasOverridenToString(TypeInfo typeInfo)
        {
            for (var type = typeInfo.Type; type?.BaseType != null; type = type.BaseType)
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