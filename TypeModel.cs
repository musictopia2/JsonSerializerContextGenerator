namespace JsonSerializerContextGenerator;
internal record struct TypeModel()
{
    public readonly string GetGlobalNameSpace => $"global::{SymbolUsed!.ContainingNamespace.ToDisplayString()}";
    public string CollectionNameSpace { get; set; } = "";
    public string CollectionStringName { get; set; } = "";
    public string FileName { get; set; } = ""; //try to search by filename now.
    public EnumListCategory ListCategory { get; set; }
    public EnumTypeCategory TypeCategory { get; set; }
    public EnumRecordCategory RecordCategory { get; set; } = EnumRecordCategory.None;
    public ITypeSymbol? SymbolUsed { get; set; }
    /// <summary>
    /// this will be when there are generics but is not the custom lists though.
    /// </summary>
    public BasicList<ITypeSymbol> GenericsUsed { get; set; } = new();
    public readonly string TypeName => SymbolUsed!.Name;
    public bool Nullable { get; set; }
    
    public readonly string SerializerSimpleValue()
    {
        if (TypeCategory == EnumTypeCategory.StandardSimple)
        {
            string name = TypeName;
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
        return "";
    }
    public readonly VariableInformation GetVariableInformation()
    {
        string camel;
        //try just with file name all the time (?)
        //if (Nullable)
        //{
        //    string nullName;
        //    if (ListCategory == EnumListCategory.None)
        //    {
        //        nullName = $"Nullable{TypeName}";
        //        camel = nullName.ChangeCasingForVariable(EnumVariableCategory.PrivateFieldParameter);
        //        return new(nullName, camel);
        //    }
        //    nullName = $"Nullable{FileName}";
        //    camel = nullName.ChangeCasingForVariable(EnumVariableCategory.PrivateFieldParameter);
        //    return new(nullName, camel);
        //}
        //if (ListCategory == EnumListCategory.None)
        //{
        //    camel = TypeName.ChangeCasingForVariable(EnumVariableCategory.PrivateFieldParameter);
        //    return new(TypeName, camel);
        //}
        camel = FileName.ChangeCasingForVariable(EnumVariableCategory.PrivateFieldParameter);
        return new(FileName, camel);
    }
    public bool NeedsSerialization()
    {
        if (TypeCategory == EnumTypeCategory.Struct)
        {
            return true;
        }
        if (TypeCategory == EnumTypeCategory.StandardSimple && ListCategory == EnumListCategory.Single)
        {
            return TypeName is not "Object";
        }
        if (ListCategory == EnumListCategory.Double || ListCategory == EnumListCategory.Single)
        {
            return true;
        }
        if (TypeCategory == EnumTypeCategory.Complex && Nullable)
        {
            return false;
        }
        if (ListCategory == EnumListCategory.None && HasStandardObject())
        {
            return false;
        }
        return TypeCategory == EnumTypeCategory.Complex;
    }
    public readonly bool HasStandardObject()
    {
        var list = SymbolUsed!.GetAllPublicProperties();
        foreach (var p in list)
        {
            if (p.HasAttribute(aa.JsonIgnore.JsonIgnoreAttribute) == false)
            {
                if (p.Type.Name == "Object")
                {
                    return true;
                }
            }
        }
        return false;
    }

}