namespace JsonSerializerContextGenerator;
internal static class WriterExtensions
{
    public static ICodeBlock WritePropertyValue(this ICodeBlock w, IPropertySymbol p)
    {
        w.WriteLine(w =>
        {
            w.Write("writer.WritePropertyName(PropName_")
                .Write(p.Name)
                .Write(");");
        });
        return w;
    }
    public static IWriter PopulateSubName(this IWriter w, TypeModel info)
    {
        if (info.ListCategory == EnumListCategory.None)
        {
            w.Write("//unable to populate sub name because list category was none");
            return w;
        }
        if (info.ListCategory == EnumListCategory.Single)
        {
            if (info.Nullable)
            {
                w.Write("Nullable");
            }
            w.Write(info.TypeName);
            return w;
        }
        if (info.ListCategory is not EnumListCategory.Double)
        {
            w.Write("//Not Supported");
            return w;
        }
        w.Write(info.CollectionStringName);
        if (info.Nullable)
        {
            w.Write("Nullable");
        }
        w.Write(info.TypeName);
        return w;
    }
    public static IWriter CustomGenericWrite(this IWriter w, TypeModel info)
    {
        return w.CustomGenericWrite(w =>
        {
            w.PopulateGenericInfo(info);
        });
    }
    public static IWriter PopulateSubGeneric(this IWriter w, TypeModel info)
    {
        if (info.ListCategory == EnumListCategory.Double)
        {
            return w.PopulateGenericInfo(info, EnumListCategory.Single);
        }
        if (info.ListCategory == EnumListCategory.Single)
        {
            return w.PopulateGenericInfo(info, EnumListCategory.None);
        }
        return w;
    }
    public static IWriter PopulateGenericInfo(this IWriter w, TypeModel info)
    {
        return w.PopulateGenericInfo(info, info.ListCategory);
    }
    public static IWriter PopulateNonNullGeneric(this IWriter w, TypeModel info)
    {
        w.Write("<")
        .Write(info.GetGlobalNameSpace)
               .Write(".")
               .Write(info.TypeName)
               .Write(">");
        return w;
    }
    private static IWriter FinishPopulateGenericInfo(this IWriter w, TypeModel info, EnumListCategory category)
    {
        //nullable is always at the end.
        if (info.Nullable)
        {
            w.Write("global::System.Nullable<"); //i think
        }
        w.Write(info.GetGlobalNameSpace)
               .Write(".")
               .Write(info.TypeName);
        if (info.GenericsUsed.Count == 1)
        {
            var used = info.GenericsUsed.Single();
            //will do the generic stuff.
            w.Write("<")
            .GlobalWrite()
                .Write(used.ContainingNamespace.ToDisplayString())
                .Write(".")
                .Write(used.Name)
                .Write(">");
        }
        if (info.Nullable)
        {
            w.Write(">");
        }
        if (category == EnumListCategory.Single)
        {
            w.Write(">");
        }
        if (category == EnumListCategory.Double)
        {
            w.Write(">>");
        }
        return w;
    }
    public static IWriter PopulateGenericInfo(this IWriter w, TypeModel info, EnumListCategory category)
    {
        //this does have to account for null now because it can be used in many places.
        if (category == EnumListCategory.None)
        {
            return w.FinishPopulateGenericInfo(info, category);
        }
        if (category == EnumListCategory.Single)
        {
            w.Write("global::")
                .Write(info.CollectionNameSpace)
                .Write("<");
            return w.FinishPopulateGenericInfo(info, category);
        }
        if (category == EnumListCategory.Double)
        {
            w.Write("global::")
                .Write(info.CollectionNameSpace)
                .Write("<")
                .Write(info.CollectionNameSpace)
                .Write("<");
            return w.FinishPopulateGenericInfo(info, category);
        }
        return w;
    }
}