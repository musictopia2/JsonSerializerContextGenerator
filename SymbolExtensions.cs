namespace JsonSerializerContextGenerator;
internal static class SymbolExtensions
{
    public static BasicList<IParameterSymbol> GetRecordParameters(this ITypeSymbol symbol)
    {
        var other = (INamedTypeSymbol)symbol;
        var cc = other.Constructors.First();
        return cc.Parameters.ToBasicList();
    }
    public static EnumRecordCategory RecordCategory(this ITypeSymbol symbol)
    {
        ITypeSymbol firsts;
        if (symbol.IsNullable())
        {
            firsts = symbol.GetSingleGenericTypeUsed()!;
        }
        else
        {
            firsts = symbol;
        }
        if (firsts.TypeKind == TypeKind.Struct)
        {
            return EnumRecordCategory.Struct; //not just record category anymore but did not know unfortunately.
        }
        if (firsts.IsRecord == false)
        {
            return EnumRecordCategory.None;
        }
        bool onlyParameters;
        INamedTypeSymbol other = (INamedTypeSymbol)firsts;
        var cc = other.Constructors.First();
        if (cc.Parameters.Count() > 1)
        {
            onlyParameters = true;
        }
        else if (cc.Parameters.Count() == 1 && cc.Parameters.Single().Name == "original")
        {
            onlyParameters = false;
        }
        else
        {
            onlyParameters = true;
        }
        var nexts = other.GetAllPublicProperties();
        bool wrongs = false;
        if (onlyParameters == true && nexts.Count != cc.Parameters.Count())
        {
            wrongs = true;
        }
        if (wrongs == true)
        {
            return EnumRecordCategory.Mixed;
        }
        return onlyParameters == true ? EnumRecordCategory.Record : EnumRecordCategory.Class;
    }
    public static string SerializerSimpleValue(this ITypeSymbol symbol)
    {
        if (symbol.GetSimpleCategory() != EnumTypeCategory.StandardSimple)
        {
            return "";
        }
        string name = symbol.Name;
        if (name == "Object")
        {
            return "";
        }
        if (name == "Boolean")
        {
            return "WriteBooleanValue";
        }
        if (name == "String" || name == "Char" ||
            name == "Guid" || name == "DateTime" ||
            name == "DateTimeOffset"
            )
        {
            return "WriteStringValue";
        }
        return "WriteNumberValue";
    }

    public static ITypeSymbol GetUnderlyingSymbol(this IPropertySymbol pp, EnumListCategory category, bool nullable)
    {
        if (category == EnumListCategory.None)
        {
            if (nullable == false)
            {
                return pp.Type;
            }
            return pp.Type.GetSingleGenericTypeUsed()!;
        }
        ITypeSymbol others;
        others = pp.GetSingleGenericTypeUsed()!;
        if (category == EnumListCategory.Single)
        {
            return others;
        }
        ITypeSymbol gg = others.GetSingleGenericTypeUsed()!;
        return gg;
    }
    public static ITypeSymbol GetUnderlyingSymbol(this IParameterSymbol pp, EnumListCategory category, bool nullable)
    {
        if (category == EnumListCategory.None)
        {
            if (nullable == false)
            {
                return pp.Type;
            }
            return pp.Type.GetSingleGenericTypeUsed()!;
        }
        ITypeSymbol others;
        others = pp.Type.GetSingleGenericTypeUsed()!;
        if (category == EnumListCategory.Single)
        {
            return others;
        }
        ITypeSymbol gg = others.GetSingleGenericTypeUsed()!;
        return gg;
    }
    public static EnumListCategory GetListCategory(this IParameterSymbol pp)
    {
        if (pp.Type.IsCollection() == false)
        {
            return EnumListCategory.None;
        }
        var others = pp.Type.GetSingleGenericTypeUsed();
        return others!.IsCollection() ? EnumListCategory.Double : EnumListCategory.Single;
    }
    public static EnumListCategory GetListCategory(this IPropertySymbol pp)
    {
        if (pp.IsCollection() == false)
        {
            return EnumListCategory.None;
        }
        var others = pp.GetSingleGenericTypeUsed()!;
        return others.IsCollection() ? EnumListCategory.Double : EnumListCategory.Single;
    }
    public static EnumTypeCategory GetSimpleCategory(this ITypeSymbol symbol)
    {
        if (symbol.TypeKind == TypeKind.Enum)
        {
            return EnumTypeCategory.StandardEnum;
        }
        if (symbol.Name.StartsWith("Enum"))
        {
            return EnumTypeCategory.CustomEnum;
        }
        if (symbol.IsKnownType() == false)
        {
            if (symbol.TypeKind == TypeKind.Struct)
            {
                return EnumTypeCategory.Struct;
            }
            //if (symbol.gene)
            return EnumTypeCategory.Complex;
        }
        if (symbol.Name == "DateOnly")
        {
            return EnumTypeCategory.DateOnly;
        }
        if (symbol.Name == "TimeOnly")
        {
            return EnumTypeCategory.TimeOnly;
        }
        return EnumTypeCategory.StandardSimple;
    }
    public static bool PropertyIgnored(this IPropertySymbol p, BasicList<IPropertySymbol> completeIgnores)
    {
        if (p.HasAttribute(aa.JsonIgnore.JsonIgnoreAttribute))
        {
            return true;
        }
        foreach (var aa in completeIgnores)
        {
            if (aa.Name == p.Name && aa.OriginalDefinition.ToDisplayString() == p.OriginalDefinition.ToDisplayString())
            {
                return true;
            }
        }
        return false;
    }
}