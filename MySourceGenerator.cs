namespace JsonSerializerContextGenerator;
[Generator]
public partial class MySourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(c =>
        {
            c.CreateCustomSource().AddAttributesToSourceOnly();
            c.CreateCustomSource().BuildSourceCode();
        });
        IncrementalValuesProvider<NodeInformation> declares = context.SyntaxProvider.CreateSyntaxProvider(
            (s, _) => IsSyntaxTarget(s),
            (t, _) => GetTarget(t))
            .Where(m => m != null)!;
        IncrementalValueProvider<(Compilation, ImmutableArray<NodeInformation>)> compilation
            = context.CompilationProvider.Combine(declares.Collect());
        context.RegisterSourceOutput(compilation, (spc, source) =>
        {
            Execute(source.Item1, source.Item2, spc);
        });
    }
    private bool IsSyntaxTarget(SyntaxNode syntax)
    {
        bool rets = syntax is ClassDeclarationSyntax ctx &&
            ctx.BaseList is not null &&
            ctx.ToString().Contains(nameof(MainContext));
        if (rets)
        {
            return true;
        }
        bool output = syntax switch
        {
            ClassDeclarationSyntax c when c.AttributeLists.Count > 0 => true,
            RecordDeclarationSyntax r when r.AttributeLists.Count > 0 => true,
            StructDeclarationSyntax s when s.AttributeLists.Count > 0 => true,
            _ => false
        };
        return output;
    }
    private NodeInformation? GetTarget(GeneratorSyntaxContext context)
    {
        ClassDeclarationSyntax? cc = (ClassDeclarationSyntax)context.Node;
        NodeInformation output;
        if (cc is not null)
        {
            if (cc.BaseList is not null && cc.ToString().Contains(nameof(MainContext)))
            {
                output = new();
                output.Node = context.Node;
                output.Source = EnumSourceCategory.Fluent;
                return output;
            }
        }
        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
        bool rets = symbol!.HasAttribute(aa.SourceGeneratedSerialization.SourceGeneratedSerializationAttribute); //change to what attribute i use.
        if (rets == false)
        {
            return null;
        }
        output = new();
        output.Node = context.Node;
        output.Source = EnumSourceCategory.Attribute;
        return output;
    }
    private void Execute(Compilation compilation, ImmutableArray<NodeInformation> list, SourceProductionContext context)
    {
        try
        {
            if (list.IsDefaultOrEmpty)
            {
                return;
            }
            Parser parses = new(compilation);
            var info = parses.GetResults(list);
            Emitter emitter = new(context, info, compilation);
            emitter.Emit();
        }
        catch
        {

        }
    }
    private static DiagnosticDescriptor MixedRecordProblem(ResultsModel result, TypeModel info) => new("FourthID",
        "Could not serialize",
        $"The class {result.ClassName} cannot be serialized because type {info.TypeName} was a mixed record.  Either make them all parameters alone or take out all parameters",
        "MixedRecordID",
        DiagnosticSeverity.Error,
        true
        );
}