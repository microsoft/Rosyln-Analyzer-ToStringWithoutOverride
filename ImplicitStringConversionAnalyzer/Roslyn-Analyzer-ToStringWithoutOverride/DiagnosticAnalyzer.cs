using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rosyln.Analyzer.ToStringWithOverride
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ImplicitStringConversionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            =>
                ImmutableArray.Create(StringConcatenationWithImplicitConversionAnalyzer.Rule,
                    ExplicitToStringWithoutOverrideAnalyzer.Rule, StringFormatArgumentImplicitToStringAnalyzer.Rule, InterpolatedStringImplicitToStringAnalyzer.Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            StringConcatenationWithImplicitConversionAnalyzer.Run(context);
            ExplicitToStringWithoutOverrideAnalyzer.Run(context);
            StringFormatArgumentImplicitToStringAnalyzer.Run(context);
            InterpolatedStringImplicitToStringAnalyzer.Run(context);
        }
    }
}