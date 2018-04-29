using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ToStringWithoutOverrideAnalyzer
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

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(StringFormatArgumentImplicitToStringAnalyzer.DiagnosticId, StringFormatArgumentImplicitToStringAnalyzer.Title, StringFormatArgumentImplicitToStringAnalyzer.MessageFormat,
            StringFormatArgumentImplicitToStringAnalyzer.Category, DiagnosticSeverity.Warning, true, StringFormatArgumentImplicitToStringAnalyzer.Description);

        private readonly SemanticModelAnalysisContext context;
        private TypeInspection typeInspection;
        private IArrayTypeSymbol objectArrayType;

        public StringFormatArgumentImplicitToStringAnalyzer(SemanticModelAnalysisContext context)
        {
            this.context = context;
            this.typeInspection = new TypeInspection(context.SemanticModel);
            this.objectArrayType = context.SemanticModel.Compilation.CreateArrayTypeSymbol(context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Object));
        }

        internal static void Run(SemanticModelAnalysisContext context)
        {
            new StringFormatArgumentImplicitToStringAnalyzer(context).Run();
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

                if (memberAccess?.Name.ToString() != "Format")
                {
                    continue;
                }

                var memberAccessOnTypeInfo = this.context.SemanticModel.GetTypeInfo(memberAccess.Expression);

                if (memberAccessOnTypeInfo.Type.ToString() != "string")
                {
                    continue;
                }

                var arguments = expression.ArgumentList.Arguments;

                if (arguments.Count == 2 && Equals(this.context.SemanticModel.GetTypeInfo(arguments[1].Expression).Type, this.objectArrayType))
                {
                }
                else if (arguments.Count == 2 && arguments[1].Expression is ImplicitArrayCreationExpressionSyntax)
                {
                    var paramsArraryArgumentExpression = (ImplicitArrayCreationExpressionSyntax)arguments[1].Expression;

                    foreach (var argument in paramsArraryArgumentExpression.Initializer.Expressions)
                    {
                        var typeInfo = this.context.SemanticModel.GetTypeInfo(argument);

                        if (this.typeInspection.IsTypeWithoutOverridenToString(typeInfo))
                        {
                            ReportDiagnostic(argument, typeInfo);
                        }
                    }
                }
                else
                {
                    foreach (var argument in arguments.Skip(1))
                    {
                        var typeInfo = this.context.SemanticModel.GetTypeInfo(argument.Expression);

                        if (this.typeInspection.IsTypeWithoutOverridenToString(typeInfo))
                        {
                            ReportDiagnostic(argument.Expression, typeInfo);
                        }
                    }
                }
            }
        }
        
        private void ReportDiagnostic(ExpressionSyntax expression, TypeInfo typeInfo)
        {
            var diagnostic = Diagnostic.Create(StringFormatArgumentImplicitToStringAnalyzer.Rule, expression.GetLocation(), typeInfo.Type.ToDisplayString());

            this.context.ReportDiagnostic(diagnostic);
        }
    }
}