using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ImplicitStringConversionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ImplicitStringConversionAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ImplicitStringConversionAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.AddExpression);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is BinaryExpressionSyntax && context.Node.Kind() == SyntaxKind.AddExpression)
            {
                var node = (BinaryExpressionSyntax)context.Node;

                if (node.Left.Kind() == SyntaxKind.StringLiteralExpression &&
                    node.Right.Kind() == SyntaxKind.ObjectCreationExpression)
                {
                    var diagnostic = Diagnostic.Create(Rule, node.Right.GetLocation(), node.Right.ToString());

                    context.ReportDiagnostic(diagnostic);
                } 
            }
        }
    }
}
