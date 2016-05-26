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
        private TypeInspection typeInspection;
        private IArrayTypeSymbol objectArrayType;

        public StringFormatArgumentImplicitToStringAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            typeInspection = new TypeInspection(context.SemanticModel);
            objectArrayType = context.SemanticModel.Compilation.CreateArrayTypeSymbol(context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Object));
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

                var arguments = expression.ArgumentList.Arguments;

                if (arguments.Count == 2 && Equals(context.SemanticModel.GetTypeInfo(arguments[1].Expression).Type, objectArrayType))
                {
                }
                else if (arguments.Count == 2 && arguments[1].Expression is ImplicitArrayCreationExpressionSyntax)
                {
                    var paramsArraryArgumentExpression = (ImplicitArrayCreationExpressionSyntax)arguments[1].Expression;

                    foreach (var argument in paramsArraryArgumentExpression.Initializer.Expressions)
                    {
                        var typeInfo = context.SemanticModel.GetTypeInfo(argument);

                        if (typeInspection.IsReferenceTypeWithoutOverridenToString(typeInfo))
                        {
                            ReportDiagnostic(argument, typeInfo);
                        }
                    }
                }
                else
                {
                    foreach (var argument in arguments.Skip(1))
                    {
                        var typeInfo = context.SemanticModel.GetTypeInfo(argument.Expression);

                        if (typeInspection.IsReferenceTypeWithoutOverridenToString(typeInfo))
                        {
                            ReportDiagnostic(argument.Expression, typeInfo);
                        }
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