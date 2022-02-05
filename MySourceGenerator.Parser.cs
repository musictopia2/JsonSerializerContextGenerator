namespace JsonSerializerContextGenerator;
public partial class MySourceGenerator
{
    private sealed class Parser
    {
        public static bool HadCustomEnum { get; private set; }
        private HashSet<string> _properties = new();
        private HashSet<TypeModel> _types = new();
        private HashSet<string> _lookedAt = new();
        private readonly Compilation _compilation;
        public Parser(Compilation compilation)
        {
            _compilation = compilation;
        }
        public CompleteInformation GetResults(ImmutableArray<NodeInformation> firsts)
        {
            CompleteInformation output = new();
            foreach (var first in firsts)
            {
                if (first.Source == EnumSourceCategory.Fluent)
                {
                    PopulateFluentResults(first.Node!, output);
                }
                else
                {
                    PopulateOldResults(first.Node!, output);
                }
            }
            foreach (var f in output.Results)
            {
                foreach (var item in f.Types)
                {
                    if (item.TypeCategory == EnumTypeCategory.CustomEnum && item.ListCategory == EnumListCategory.None)
                    {
                        ISymbol symbol;
                        symbol = item.SymbolUsed!;
                        if (symbol.ContainingAssembly.MetadataName == _compilation.AssemblyName)
                        {
                            HadCustomEnum = true;
                            break;
                        }
                    }
                }
            }
            return output;
        }
        private void PopulateOldResults(SyntaxNode syntax, CompleteInformation fluent)
        {
            Clear();
            SemanticModel compilationSemanticModel = syntax.GetSemanticModel(_compilation);
            INamedTypeSymbol symbol = (INamedTypeSymbol)compilationSemanticModel.GetDeclaredSymbol(syntax)!;
            ResultsModel model = new();
            model.ClassName = syntax switch
            {
                ClassDeclarationSyntax c => c.Identifier.ValueText,
                RecordDeclarationSyntax r => r.Identifier.ValueText,
                StructDeclarationSyntax s => s.Identifier.ValueText,
                _ => ""
            };
            model.NamespaceName = symbol!.ContainingNamespace.ToDisplayString();
            PopulateNames(symbol, model);
            model.PropertyNames = _properties;
            TypeModel fins = new();
            fins.CollectionNameSpace = "";
            fins.CollectionStringName = "";
            fins.SymbolUsed = symbol;
            fins.TypeCategory = EnumTypeCategory.Complex;
            fins.ListCategory = EnumListCategory.None;
            fins.FileName = symbol.Name;
            fins.Nullable = false;
            fins.RecordCategory = symbol.RecordCategory();
            _types.Add(fins);
            model.Types = _types;
            AddResults(model, fluent);
        }
        private void AddResults(ResultsModel result, CompleteInformation fluent)
        {
            foreach (var item in fluent.Results)
            {
                if (item.ClassName == result.ClassName && item.NamespaceName == result.NamespaceName)
                {
                    return;
                }
            }
            fluent.Results.Add(result);
        }
        private void PopulateFluentResults(SyntaxNode node, CompleteInformation fluent)
        {
            ParseContext context = new(_compilation, node);
            if (node is not ClassDeclarationSyntax classDeclaration)
            {
                return;
            }
            var members = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var m in members)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(m) as IMethodSymbol;
                if (symbol is not null && symbol.Name == MainContext.ConfigureName)
                {
                    ParseContext(context, m, symbol, fluent);
                    return;
                }
            }
            return;
        }
        private void Clear()
        {
            _properties = new();
            _types = new();
            _lookedAt = new();
        }
        private void ParseContext(ParseContext context, MethodDeclarationSyntax syntax, IMethodSymbol symbol, CompleteInformation fluent)
        {
            var makeCalls = ParseUtils.FindCallsOfMethodWithName(context, syntax, nameof(ICustomConfig.Make));
            foreach (var make in makeCalls)
            {
                INamedTypeSymbol makeType = (INamedTypeSymbol)make.MethodSymbol.TypeArguments[0]!;
                string name = nameof(IMakeConfig<object>.SourceGeneratedSerializable);
                var firsts = ParseUtils.FindCallsOfMethodInConfigLambda(context, make, name);
                if (firsts.Count == 1)
                {
                    Clear();
                    ResultsModel result = new();
                    result.ClassName = makeType.Name;
                    result.NamespaceName = makeType.ContainingNamespace.ToDisplayString();
                    PopulateNames(makeType, result);
                    result.PropertyNames = _properties;
                    TypeModel fins = new();
                    fins.CollectionNameSpace = "";
                    fins.CollectionStringName = "";
                    fins.SymbolUsed = makeType;
                    fins.TypeCategory = EnumTypeCategory.Complex;
                    fins.ListCategory = EnumListCategory.None;
                    fins.FileName = makeType.Name;
                    fins.Nullable = false;
                    fins.RecordCategory = makeType.RecordCategory();
                    _types.Add(fins);
                    result.Types = _types;
                    AddResults(result, fluent);
                    var seconds = ParseUtils.FindCallsOfMethodInConfigLambda(context, firsts.Single(), nameof(IFinalConfig<object>.Ignore), optional: true);
                    foreach (var zz in seconds)
                    {
                        var ignoreIdentifier = zz.Invocation.DescendantNodes()
                           .OfType<IdentifierNameSyntax>()
                           .Last();
                        var ignoreProp = makeType.GetMembers(ignoreIdentifier.Identifier.ValueText)
                    .OfType<IPropertySymbol>()
                    .SingleOrDefault();
                        fluent.PropertiesToIgnore.Add(ignoreProp);
                    }
                }
                name = nameof(IMakeConfig<object>.IgnoreProperties);
                firsts = ParseUtils.FindCallsOfMethodInConfigLambda(context, make, name);
                if (firsts.Count == 1)
                {
                    var seconds = ParseUtils.FindCallsOfMethodInConfigLambda(context, firsts.Single(), nameof(IFinalConfig<object>.Ignore), optional: true);
                    foreach (var zz in seconds)
                    {
                        var ignoreIdentifier = zz.Invocation.DescendantNodes()
                           .OfType<IdentifierNameSyntax>()
                           .Last();
                        var ignoreProp = makeType.GetMembers(ignoreIdentifier.Identifier.ValueText)
                    .OfType<IPropertySymbol>()
                    .SingleOrDefault();
                        fluent.PropertiesToIgnore.Add(ignoreProp);
                    }
                }
            }
        }
        private void PopulateNames(INamedTypeSymbol symbol, ResultsModel results)
        {
            if (_lookedAt.Contains(symbol.Name) == true)
            {
                return;
            }
            _lookedAt.Add(symbol.Name);
            var properties = symbol.GetAllPublicProperties();
            properties.RemoveAllOnly(xx =>
            {
                return xx.IsReadOnly ||
                xx.CanBeReferencedByName == false ||
                xx.SetMethod is null;
            });
            foreach (var pp in properties)
            {
                ITypeSymbol others;
                ITypeSymbol mm;
                if (pp.HasAttribute(aa.JsonIgnore.JsonIgnoreAttribute) == false)
                {
                    _properties.Add(pp.Name);
                }
                if (pp.IsCollection())
                {
                    others = pp.GetSingleGenericTypeUsed()!;
                    if (others is not null && others.IsCollection())
                    {
                        TypeModel fins = new();
                        fins.ListCategory = EnumListCategory.Double;
                        fins.CollectionNameSpace = $"{others.ContainingSymbol.ToDisplayString()}.{others.Name}";
                        fins.CollectionStringName = others.Name;
                        mm = others.GetSingleGenericTypeUsed()!;
                        fins.Nullable = mm.IsNullable();
                        if (fins.Nullable == false)
                        {
                            fins.FileName = $"{others.Name}{others.Name}{mm!.Name}";
                            fins.SymbolUsed = mm;
                            fins.TypeCategory = fins.SymbolUsed.GetSimpleCategory();
                            _types.Add(fins);
                            AddListNames(mm, others, results);
                            continue;
                        }
                        ITypeSymbol gg = mm.GetSingleGenericTypeUsed()!;
                        fins.SymbolUsed = gg;
                        fins.TypeCategory = fins.SymbolUsed.GetSimpleCategory();
                        if (fins.TypeCategory == EnumTypeCategory.Complex)
                        {
                            fins.FileName = $"{others.Name}{others.Name}{gg.Name}";
                        }
                        else
                        {
                            fins.FileName = $"{others.Name}{others.Name}Nullable{gg.Name}";
                        }
                        _types.Add(fins);
                        AddListNames(mm, others, results);
                        continue;
                    }
                    AddListNames(others!, pp.Type, results);
                    continue;
                }
                AddSimpleName(pp, results);
            }
        }
        private void AddListNames(ITypeSymbol symbol, ISymbol collection, ResultsModel results)
        {
            TypeModel fins = new();
            fins.Nullable = symbol.IsNullable();
            fins.ListCategory = EnumListCategory.Single;
            string name = collection.Name;
            fins.CollectionNameSpace = $"{collection.ContainingSymbol.ToDisplayString()}.{name}";
            if (fins.Nullable == false)
            {
                fins.SymbolUsed = symbol;
                fins.TypeCategory = fins.SymbolUsed.GetSimpleCategory();
                fins.FileName = $"{name}{symbol.Name}";
                _types.Add(fins);
                AddSimpleName(symbol, results);
                return;
            }
            var mm = symbol.GetSingleGenericTypeUsed()!;
            fins.SymbolUsed = mm;
            fins.TypeCategory = fins.SymbolUsed.GetSimpleCategory();
            fins.FileName = $"{name}Nullable{mm.Name}";
            _types.Add(fins);
            AddSimpleName(symbol, results);
        }
        private void AddSimpleName(IPropertySymbol symbol, ResultsModel results)
        {
            AddSimpleName(symbol.Type, results);
        }
        private void AddSimpleName(ITypeSymbol symbol, ResultsModel results)
        {
            TypeModel fins = new();
            fins.Nullable = symbol.IsNullable();
            fins.ListCategory = EnumListCategory.None;
            if (fins.Nullable == false)
            {
                fins.SymbolUsed = symbol;
                fins.FileName = symbol.Name;
                fins.TypeCategory = fins.SymbolUsed.GetSimpleCategory();
                var others = symbol.GetSingleGenericTypeUsed();
                if (others is not null)
                {
                    fins.GenericsUsed = new()
                    {
                        others
                    };
                }
                if (fins.GenericsUsed.Count == 1)
                {
                    fins.FileName = $"{symbol.Name}{others!.Name}";
                }
                if (fins.TypeCategory == EnumTypeCategory.Complex || fins.TypeCategory == EnumTypeCategory.Struct)
                {
                    fins.RecordCategory = symbol.RecordCategory();
                    _types.Add(fins);
                    PopulateNames((INamedTypeSymbol)symbol, results);
                    return;
                }
                _types.Add(fins);
                return;
            }
            else
            {
                var others = symbol.GetSingleGenericTypeUsed();
                fins.SymbolUsed = others;
                fins.TypeCategory = fins.SymbolUsed!.GetSimpleCategory();
                if (fins.TypeCategory == EnumTypeCategory.Complex || fins.TypeCategory == EnumTypeCategory.Struct)
                {
                    if (others!.TypeKind == TypeKind.Struct)
                    {
                        fins.FileName = $"Nullable{others!.Name}";
                        fins.Nullable = true;
                        _types.Add(fins);
                        fins = new();
                        fins.Nullable = false;
                        fins.ListCategory = EnumListCategory.None;
                        fins.SymbolUsed = others;
                        fins.TypeCategory = EnumTypeCategory.Complex;
                    }
                    fins.FileName = others!.Name;
                    fins.RecordCategory = symbol.RecordCategory();
                    _types.Add(fins);
                    PopulateNames((INamedTypeSymbol)others!, results);
                    return;
                }
                fins.FileName = $"Nullable{others!.Name}";
                _types.Add(fins);
                fins = new();
                fins.Nullable = false;
                fins.ListCategory = EnumListCategory.None;
                fins.FileName = others!.Name;
                fins.SymbolUsed = others;
                fins.TypeCategory = fins.SymbolUsed.GetSimpleCategory();
                _types.Add(fins);
            }
        }
    }
}