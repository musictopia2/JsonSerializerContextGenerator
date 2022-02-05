namespace JsonSerializerContextGenerator;
public partial class MySourceGenerator
{
    private partial class Emitter
    {
        private partial class ProcessTypesClass
        {
            private readonly ResultsModel _result;
            private readonly TypeModel _info;
            private readonly SourceProductionContext _context;
            private readonly BasicList<IPropertySymbol> _ignoreList;
            public ProcessTypesClass(ResultsModel result, TypeModel info, SourceProductionContext context, BasicList<IPropertySymbol> ignoreList)
            {
                _result = result;
                _info = info;
                _context = context;
                _ignoreList = ignoreList;
            }
            public void ProcessTypes()
            {
                AddSource(w =>
                {
                    JsonTypeProcess(w);
                    PropertyInitProcesses(w);
                    SerializeProcess(w);
                });
            }
            private void AddSource(Action<ICodeBlock> action)
            {
                SourceCodeStringBuilder builder = new();
                builder.WriteContext(w =>
                {
                    action.Invoke(w);
                }, _result);
                _context.AddSource($"{_result.ClassName}Context.{_info.FileName}.g", builder.ToString());
            }
            #region Serialize Portion
            private void SerializeProcess(ICodeBlock w)
            {
                if (_info.NeedsSerialization() == false)
                {
                    return;
                }
                w.WriteLine(w =>
                {
                    w.Write("private static void ")
                    .Write(_info.FileName)
                    .Write("SerializeHandler(global::System.Text.Json.Utf8JsonWriter writer, ")
                    .PopulateGenericInfo(_info);
                    if (_info.SymbolUsed!.TypeKind != TypeKind.Struct)
                    {
                        w.Write("?");
                    }
                    w.Write(" value)");
                }).WriteCodeBlock(w =>
                {
                    if (_info.TypeCategory == EnumTypeCategory.Complex && _info.ListCategory == EnumListCategory.None)
                    {
                        SerializeComplex(w);
                    }
                    else if (_info.ListCategory is not EnumListCategory.None)
                    {
                        SerializeList(w);
                    }
                    else if (_info.TypeCategory == EnumTypeCategory.Struct)
                    {
                        SerializeStruct(w);
                    }
                    else
                    {
                        w.WriteLine("//Unable to figure out the serialization for now");
                    }
                });
            }
            private void SerializeList(ICodeBlock w)
            {
                w.WriteLine("if (value == null)")
                .WriteCodeBlock(w =>
                {
                    w.WriteLine("writer.WriteNullValue();")
                    .WriteLine("return;");
                })
                .WriteLine("writer.WriteStartArray();")
                .WriteLine("for (int i = 0; i < value.Count; i++)")
                .WriteCodeBlock(w =>
                {
                    SerializeLoop(w);
                })
                .WriteLine("writer.WriteEndArray();");
            }
            private void SerializeLoop(ICodeBlock w)
            {
                if (_info.ListCategory == EnumListCategory.Double)
                {
                    w.WriteLine(w =>
                    {
                        if (_info.SymbolUsed!.Name is not "Object")
                        {
                            w.PopulateSubName(_info)
                            .Write("SerializeHandler(writer, value[i]!);");
                        }
                        else
                        {
                            w.Write("global::System.Text.Json.JsonSerializer.Serialize(writer, value[i]!, ")
                            .Write(_result.GetGlobalName)
                            .Write("Context.Default.")
                            .Write(_info.CollectionStringName)
                            .Write("Object!);");
                        }
                    });
                    return;
                }
                if (_info.TypeCategory == EnumTypeCategory.StandardEnum || _info.TypeCategory == EnumTypeCategory.DateOnly || _info.TypeCategory == EnumTypeCategory.TimeOnly || _info.Nullable)
                {
                    w.WriteLine(w =>
                    {
                        w.Write("global::System.Text.Json.JsonSerializer.Serialize(writer, value[i], ")
                        .Write(_result.GetGlobalName)
                        .Write("Context")
                        .Write(".Default.");
                        if (_info.Nullable)
                        {
                            w.Write("Nullable");
                        }
                        w.Write(_info.TypeName)
                        .Write("!);");
                    });
                    return;
                }
                if (_info.TypeCategory == EnumTypeCategory.CustomEnum)
                {
                    w.WriteLine("if (value[i].IsNull)")
                        .WriteCodeBlock(w =>
                        {
                            w.WriteLine(w =>
                            {
                                w.Write("writer.WriteStringValue(")
                                .AppendDoubleQuote()
                                .Write(");");
                            });
                        })
                        .WriteLine("else")
                        .WriteCodeBlock(w =>
                        {
                            w.WriteLine("writer.WriteStringValue(value[i].Name);"); //this is fine this time.
                        });
                    return;
                }
                if (_info.TypeCategory == EnumTypeCategory.Complex)
                {
                    w.WriteLine(w =>
                    {
                        w.PopulateSubName(_info)
                        .Write("SerializeHandler(writer, value[i]!);");
                    });
                    return;
                }
                w.WriteLine(w =>
                {
                    w.Write("writer.")
                    .Write(_info.SerializerSimpleValue())
                    .Write("(value[i]);");
                });
            }
            private void SerializeStruct(ICodeBlock w)
            {
                w.WriteLine("writer.WriteStartObject();");
                var list = _info.SymbolUsed!.GetAllPublicProperties();
                foreach (var p in list)
                {
                    if (p.PropertyIgnored(_ignoreList))
                    {
                        continue;
                    }
                    FinishWriteProperty(w, p);
                }
                w.WriteLine("writer.WriteEndObject();");

            }
            private void FinishWriteProperty(ICodeBlock w, IPropertySymbol p)
            {
                EnumTypeCategory typeCategory = p.Type.GetSimpleCategory();
                EnumListCategory listCategory = p.GetListCategory();
                if (listCategory != EnumListCategory.None)
                {
                    WriteListSerializer(w, p, p.Type, listCategory);
                    return;
                }
                if (typeCategory == EnumTypeCategory.StandardSimple && p.Type.IsNullable() == false)
                {
                    string value = p.Type.SerializerSimpleValue();
                    value = value.Replace("Value", "");
                    w.WriteLine(w =>
                    {
                        w.Write("writer.")
                        .Write(value)
                        .Write("(PropName_")
                        .Write(p.Name)
                        .Write(", value!.")
                        .Write(p.Name)
                        .Write(");");
                    });
                    return;
                }
                if (p.Type.IsNullable())
                {
                    ITypeSymbol nullUnder = p.Type.GetSingleGenericTypeUsed()!;
                    WriteNullableValue(w, p, nullUnder);
                    return;
                }
                if (typeCategory == EnumTypeCategory.CustomEnum)
                {
                    w.WriteLine(w =>
                    {
                        w.Write("writer.WriteStringValue(value!.")
                        .Write(p.Name)
                          .Write(".Name);");
                    });
                    return;
                }
                if (typeCategory == EnumTypeCategory.Complex)
                {
                    WriteSimpleSerializer(w, p, p.Type);
                    return;
                }
                WriteSymbolValue(w, p, p.Type);
            }
            private void SerializeComplex(ICodeBlock w)
            {
                w.WriteLine("writer.WriteStartObject();");
                var list = _info.SymbolUsed!.GetAllPublicProperties();
                foreach (var p in list)
                {
                    if (p.PropertyIgnored(_ignoreList))
                    {
                        continue;
                    }
                    w.WritePropertyValue(p);
                    FinishWriteProperty(w, p);
                }
                w.WriteLine("writer.WriteEndObject();");
            }
            private void WriteSimpleSerializer(ICodeBlock w, IPropertySymbol p, ITypeSymbol symbol)
            {
                w.WriteLine(w =>
                {
                    w.Write(symbol.Name);
                    ITypeSymbol? fins = p.GetSingleGenericTypeUsed();
                    if (fins is not null)
                    {
                        w.Write(fins.Name);
                    }
                    w.Write("SerializeHandler(writer, value!.")
                    .Write(p.Name)
                    .Write("!);");
                });
            }
            private void WriteSymbolValue(ICodeBlock w, IPropertySymbol p, ITypeSymbol symbol)
            {
                w.WriteLine(w =>
                {
                    w.Write("global::System.Text.Json.JsonSerializer.Serialize(writer, value!.")
                    .Write(p.Name)
                    .Write(", ")
                    .Write(_result.GetGlobalMain)
                    .Write(".")
                    .Write(_result.ClassName)
                    .Write("Context")
                    .Write(".Default.")
                    .Write(symbol.Name)
                    .Write("!);");
                });
            }
            private void WriteNullableValue(ICodeBlock w, IPropertySymbol p, ITypeSymbol nullUnder)
            {
                w.WriteLine(w =>
                {
                    w.Write("global::System.Text.Json.JsonSerializer.Serialize(writer, value!.")
                    .Write(p.Name)
                    .Write(", ")
                    .Write(_result.GetGlobalMain)
                    .Write(".")
                    .Write(_result.ClassName)
                    .Write("Context")
                    .Write(".Default.Nullable")
                    .Write(nullUnder.Name)
                    .Write("!);");
                });
            }
            private void WriteListSerializer(ICodeBlock w, IPropertySymbol p, ITypeSymbol symbol, EnumListCategory list)
            {
                string name = GetListValue(symbol, list);
                w.WriteLine(w =>
                {
                    w.Write(name)
                    .Write("SerializeHandler(writer, value!.")
                    .Write(p.Name)
                    .Write("!);");
                });
            }
            #endregion
            private string GetListValue(ITypeSymbol symbol, EnumListCategory list)
            {
                string collectionName = symbol.Name;
                ITypeSymbol other = symbol.GetSingleGenericTypeUsed()!;
                ITypeSymbol? gg;
                if (list == EnumListCategory.Single)
                {
                    if (other.IsNullable())
                    {
                        gg = other.GetSingleGenericTypeUsed();
                        return $"{collectionName}Nullable{gg!.Name}";
                    }
                    return $"{collectionName}{other.Name}";
                }
                if (list == EnumListCategory.None)
                {
                    return "";
                }
                gg = other.GetSingleGenericTypeUsed();
                if (gg!.IsNullable())
                {
                    ITypeSymbol ff = gg!.GetSingleGenericTypeUsed()!;
                    return $"{collectionName}{collectionName}Nullable{ff.Name}";
                }
                return $"{collectionName}{collectionName}{gg!.Name}";
            }
            #region Property Processes
            private void PropertyInitProcesses(ICodeBlock w)
            {
                if (_info.ListCategory is not EnumListCategory.None)
                {
                    return;
                }
                if (_info.Nullable)
                {
                    return;
                }
                if (_info.TypeCategory is not EnumTypeCategory.Complex && _info.TypeCategory is not EnumTypeCategory.Struct)
                {
                    return;
                }
                w.WriteLine(w =>
                {
                    w.Write("private static global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo[] ")
                    .Write(_info.TypeName)
                    .Write("PropInit(global::System.Text.Json.Serialization.JsonSerializerContext context)");
                })
                .WriteCodeBlock(w =>
                {
                    w.WriteLine(w =>
                    {
                        w.Write(_result.GetGlobalName)
                        .Write("Context jsonContext = (")
                        .Write(_result.GetGlobalName)
                        .Write("Context)context;");
                    })
                    .WriteLine("global::System.Text.Json.JsonSerializerOptions options = context.Options;");
                    var list = _info.SymbolUsed!.GetAllPublicProperties();
                    int count = list.Count;
                    w.WriteLine(w =>
                    {
                        w.Write("global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo[] properties = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo[")
                        .Write(count)
                        .Write("];");
                    });
                    int upTo = 0;
                    string contains = w.GetSingleText(w =>
                    {
                        w.Write(_info.GetGlobalNameSpace)
                        .Write(".")
                        .Write(_info.TypeName);
                        if (_info.GenericsUsed.Count == 1 && _info.TypeCategory == EnumTypeCategory.Complex)
                        {
                            ITypeSymbol fins = _info.GenericsUsed.Single();
                            w.Write("<")
                            .GlobalWrite()
                            .Write(fins.ContainingNamespace.ToDisplayString())
                            .Write(".")
                            .Write(fins.Name)
                            .Write(">");
                        }
                    });
                    foreach (var p in list)
                    {
                        bool ignore = false;
                        if (p.HasAttribute(aa.JsonIgnore.JsonIgnoreAttribute))
                        {
                            ignore = true;
                        }
                        PopulateSinglePropertyInfo(w, _info.RecordCategory, p, contains, upTo, ignore);
                        upTo++;
                    }
                    w.WriteLine("return properties;");
                });
            }
            private void PopulateSinglePropertyInfo(ICodeBlock w, EnumRecordCategory category, IPropertySymbol p, string containingInfo, int upTo, bool ignore)
            {
                string nameUsed;
                EnumListCategory cat = p.GetListCategory();
                bool nullable = p.IsNullable();
                if (nullable)
                {
                    nameUsed = $"Nullable{p.GetSingleGenericTypeUsed()!.Name}";
                }
                else if (cat == EnumListCategory.None)
                {
                    ITypeSymbol? fins = p.Type.GetSingleGenericTypeUsed();
                    if (fins is not null)
                    {
                        nameUsed = $"{p.Type.Name}{fins.Name}";
                    }
                    else
                    {
                        nameUsed = p.Type.Name;
                    }
                }
                else
                {
                    nameUsed = GetListValue(p.Type, cat);
                }
                w.WriteLine(w =>
                {
                    w.Write("global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<");
                    PopulateGenericForProperty(w, p, cat, nullable);
                    w.Write("> info")
                    .Write(upTo)
                    .Write(" = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<");
                    PopulateGenericForProperty(w, p, cat, nullable);
                    w.Write(">()");
                }).WriteCodeBlock(w =>
                {
                    w.WriteLine("IsProperty = true,")
                    .WriteLine("IsPublic = true,")
                    .WriteLine("IsVirtual = false,")
                    .WriteLine(w =>
                    {
                        w.Write("DeclaringType = typeof(")
                        .Write(containingInfo)
                        .Write("),");
                    })
                    .WriteLine(w =>
                    {
                        w.Write("PropertyTypeInfo = jsonContext.")
                        .Write(nameUsed)
                        .Write(",");
                    })
                    .WriteLine("Converter = null,");
                    if (ignore == true)
                    {
                        w.WriteLine("Getter = null,")
                       .WriteLine("Setter = null,")
                       .WriteLine("IgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.Always,");
                    }
                    else if (category == EnumRecordCategory.Record || category == EnumRecordCategory.Struct)
                    {
                        w.WriteLine(w =>
                        {
                            w.Write("Getter = static (obj) => ((")
                            .Write(containingInfo)
                            .Write(")obj).")
                            .Write(p.Name)
                            .Write("!,");
                        })
                        .WriteLine(w =>
                        {
                            w.Write("Setter = static (obj, value) => global::System.Runtime.CompilerServices.Unsafe.Unbox<")
                            .Write(containingInfo)
                            .Write(">(obj).")
                            .Write(p.Name)
                            .Write(" = value!,");
                        })
                        .WriteLine("IgnoreCondition = null,");
                    }
                    else
                    {
                        w.WriteLine(w =>
                        {
                            w.Write("Getter = static (obj) => ((")
                            .Write(containingInfo)
                            .Write(")obj).")
                            .Write(p.Name)
                            .Write("!,");
                        })
                      .WriteLine(w =>
                      {
                          w.Write("Setter = static (obj, value) => ((")
                          .Write(containingInfo)
                          .Write(")obj).")
                          .Write(p.Name)
                          .Write(" = value!,");
                      })
                      .WriteLine("IgnoreCondition = null,");
                    }
                    w.WriteLine("HasJsonInclude = false,")
                    .WriteLine("IsExtensionData = false,")
                    .WriteLine("NumberHandling = default,")
                    .WriteLine(w =>
                    {
                        w.Write("PropertyName = ")
                        .AppendDoubleQuote(w =>
                        {
                            w.Write(p.Name);
                        }).Write(",");
                    }).WriteLine("JsonPropertyName = null");
                }, endSemi: true)
                .WriteLine(w =>
                {
                    w.Write("properties[")
                    .Write(upTo)
                    .Write("] = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreatePropertyInfo<");
                    PopulateGenericForProperty(w, p, cat, nullable);
                    w.Write(">(options, info")
                    .Write(upTo)
                    .Write(");");
                });
            }
            private void PopulateGenericForProperty(IWriter w, IPropertySymbol p, EnumListCategory category, bool nullable)
            {
                string nameSpace = p.Type.ContainingNamespace.ToDisplayString();
                string className = p.Type.Name;
                ITypeSymbol other = p.GetUnderlyingSymbol(category, nullable);
                if (nullable)
                {
                    w.Write("global::System.Nullable<global::")
                        .Write(other.ContainingNamespace.ToDisplayString())
                        .Write(".")
                        .Write(other.Name)
                        .Write(">");
                    return;
                }
                w.Write("global::")
                .Write(nameSpace).Write(".")
                .Write(className);
                if (category == EnumListCategory.None)
                {
                    ITypeSymbol? fins = p.Type.GetSingleGenericTypeUsed();
                    if (fins is not null)
                    {
                        w.Write("<")
                        .GlobalWrite()
                            .Write(fins.ContainingNamespace.ToDisplayString())
                            .Write(".")
                            .Write(fins.Name)
                            .Write(">");
                    }
                    return;
                }
                w.Write("<global::");
                if (category == EnumListCategory.Single)
                {
                    if (other.IsNullable() == false)
                    {
                        w.Write(other.ContainingNamespace.ToDisplayString())
                        .Write(".")
                        .Write(other.Name)
                        .Write(">");
                        return;
                    }
                    ITypeSymbol singlefins = other.GetSingleGenericTypeUsed()!;
                    w.Write("System.Nullable<global::")
                    .Write(singlefins.ContainingNamespace.ToDisplayString())
                    .Write(".")
                    .Write(singlefins.Name)
                    .Write(">>");
                }
                if (category == EnumListCategory.Double)
                {
                    w.Write(nameSpace).Write(".")
                    .Write(className)
                    .Write("<global::");
                    if (other.IsNullable() == false)
                    {
                        w.Write(other.ContainingNamespace.ToDisplayString())
                        .Write(".")
                        .Write(other.Name)
                        .Write(">>");
                        return;
                    }
                    ITypeSymbol doublefins = other.GetSingleGenericTypeUsed()!;
                    w.Write("System.Nullable<global::")
                   .Write(doublefins.ContainingNamespace.ToDisplayString())
                   .Write(".")
                   .Write(doublefins.Name)
                   .Write(">>>");
                }
            }
            #endregion
            #region JsonInfo
            private void JsonTypeProcess(ICodeBlock w)
            {
                VariableInformation variable = _info.GetVariableInformation();
                w.WriteLine(w =>
                {
                    w.Write("private global::System.Text.Json.Serialization.Metadata.JsonTypeInfo")
                    .CustomGenericWrite(_info)
                    .Write("? ")
                    .Write(variable.Camel)
                    .Write(";");
                })
                .WriteLine(w =>
                {
                    w.Write("public global::System.Text.Json.Serialization.Metadata.JsonTypeInfo")
                    .CustomGenericWrite(_info)
                    .Write(" ")
                    .Write(variable.Name);
                })
                .WriteCodeBlock(w =>
                {
                    w.WriteLine("get")
                    .WriteCodeBlock(w =>
                    {
                        w.WriteLine(w =>
                        {
                            w.Write("if (")
                            .Write(variable.Camel)
                            .Write(" is null)");
                        })
                        .WriteCodeBlock(w =>
                        {
                            JsonCustom(w, variable);
                        });
                        if (_info.TypeCategory == EnumTypeCategory.CustomEnum && _info.Nullable == false && _info.ListCategory == EnumListCategory.None)
                        {
                            w.WriteLine(w =>
                            {
                                w.Write("if (")
                                .Write(variable.Camel)
                                .Write(" is null)");
                            })
                            .WriteCodeBlock(w =>
                            {
                                w.WriteLine(w =>
                                {
                                    w.Write("throw new global::CommonBasicLibraries.BasicDataSettingsAndProcesses.CustomBasicException(")
                                    .AppendDoubleQuote(w =>
                                    {
                                        w.Write("Did not create converter for ")
                                        .Write(_info.TypeName);
                                    })
                                    .Write(");");
                                });
                            });
                        }
                        w.WriteLine(w =>
                        {
                            w.Write("return ")
                            .Write(variable.Camel)
                            .Write(";");
                        });
                    });
                });
            }
            private void JsonCustom(ICodeBlock w, VariableInformation variable)
            {
                if (NeedsCustomConverter())
                {
                    JsonConverterFirst(w, variable);
                    return;
                }
                if (_info.ListCategory is not EnumListCategory.None)
                {
                    CollectionInit(w, variable);
                    return;
                }
                InitVariableValue(w, variable);
            }
            private bool NeedsCustomConverter()
            {
                if (_info.TypeCategory == EnumTypeCategory.Complex || _info.TypeCategory == EnumTypeCategory.Struct)
                {
                    return true;
                }
                if (_info.TypeCategory == EnumTypeCategory.StandardEnum)
                {
                    return false;
                }
                if (_info.TypeCategory == EnumTypeCategory.CustomEnum)
                {
                    return _info.ListCategory == EnumListCategory.None;
                }
                if (_info.TypeCategory == EnumTypeCategory.DateOnly || _info.TypeCategory == EnumTypeCategory.TimeOnly || _info.TypeCategory == EnumTypeCategory.StandardSimple)
                {
                    return _info.ListCategory is not EnumListCategory.None;
                }
                return false;
            }
            private void JsonConverterFirst(ICodeBlock w, VariableInformation variable)
            {
                w.WriteLine("global::System.Text.Json.Serialization.JsonConverter? customConverter;")
                .WriteLine(w =>
                {
                    w.Write("if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(")
                    .PopulateGenericInfo(_info)
                    .Write("))) is not null)");
                })
                .WriteCodeBlock(w =>
                {
                    w.WriteLine(w =>
                    {
                        w.Write(variable.Camel)
                        .Write(" = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreateValueInfo")
                        .CustomGenericWrite(_info)
                        .Write("(Options, customConverter);");
                    });
                });
                if (_info.Nullable == false && _info.TypeCategory == EnumTypeCategory.CustomEnum && _info.ListCategory == EnumListCategory.None)
                {
                    InitVariableValue(w, variable);
                    return; //try this now (?)
                }
                w.WriteLine("else")
                .WriteCodeBlock(w =>
                {
                    JsonConverterSecond(w, variable);
                });
            }
            private void JsonConverterSecond(ICodeBlock w, VariableInformation variable)
            {
                if (_info.Nullable && _info.ListCategory == EnumListCategory.None && _info.TypeCategory == EnumTypeCategory.Complex)
                {
                    InitVariableValue(w, variable);
                    return; //try this.
                }
                if (_info.ListCategory == EnumListCategory.None)
                {
                    ObjectInit(w, variable);
                }
                else
                {
                    CollectionInit(w, variable);
                }
            }
            private void CollectionInit(ICodeBlock w, VariableInformation variable)
            {
                w.WriteLine(w =>
                {
                    w.Write("global::System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues")
                    .CustomGenericWrite(_info)
                    .Write(" info = new global::System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues")
                    .CustomGenericWrite(_info)
                    .Write("()");
                })
                .WriteCodeBlock(w =>
                {
                    w.WriteLine(w =>
                    {
                        w.Write("ObjectCreator = () => new ")
                        .PopulateGenericInfo(_info)
                        .Write("(),");
                    })
                    .WriteLine("KeyInfo = null,")
                    .WriteLine(w =>
                    {
                        w.Write("ElementInfo = this.")
                        .PopulateSubName(_info) //i think
                        .Write(",");
                    })
                    .WriteLine("NumberHandling = default,")
                    .WriteLine(w =>
                    {
                        w.Write("SerializeHandler = ");
                        if (_info.ListCategory == EnumListCategory.Single && _info.SerializerSimpleValue() == "")
                        {
                            w.Write("null");
                        }
                        else
                        {
                            w.Write(_info.FileName)
                            .Write("SerializeHandler");
                        }
                    });
                }, endSemi: true);
                RestCollection(w, variable);
            }
            private void RestCollection(ICodeBlock w, VariableInformation variable)
            {
                w.WriteLine(w =>
                {
                    w.Write(variable.Camel)
                    .Write(" = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreateIListInfo<")
                    .PopulateGenericInfo(_info)
                    .Write(", ")
                    .PopulateSubGeneric(_info)
                    .Write(">(Options, info);");
                });
            }
            private void RestObject(ICodeBlock w, VariableInformation variable)
            {
                if (_info.TypeCategory == EnumTypeCategory.CustomEnum)
                {
                    return;
                }
                w.WriteLine(w =>
                {
                    w.Write(variable.Camel)
                    .Write(" = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreateObjectInfo")
                    .CustomGenericWrite(_info)
                    .Write("(Options ,objectInfo);");
                });
            }
            private void ObjectInit(ICodeBlock w, VariableInformation variable)
            {
                if (_info.TypeCategory == EnumTypeCategory.CustomEnum)
                {
                    InitVariableValue(w, variable);
                    return;
                }
                w.WriteLine(w =>
                {
                    w.Write("global::System.Text.Json.Serialization.Metadata.JsonObjectInfoValues")
                    .CustomGenericWrite(_info)
                    .Write(" objectInfo = new global::System.Text.Json.Serialization.Metadata.JsonObjectInfoValues")
                    .CustomGenericWrite(_info)
                    .Write("()");
                })
                .WriteCodeBlock(w =>
                {
                    w.WriteLine(w =>
                    {
                        w.Write("ObjectCreator = () => new ")
                        .PopulateGenericInfo(_info)
                        .Write("(),");
                    })
                    .WriteLine("ObjectWithParameterizedConstructorCreator = null,")
                    .WriteLine(w =>
                    {
                        w.Write("PropertyMetadataInitializer = ")
                        .Write(_info.TypeName)
                        .Write("PropInit,");
                    })
                    .WriteLine("ConstructorParameterMetadataInitializer = null,")
                    .WriteLine("NumberHandling = default,")
                    .WriteLine(w =>
                    {
                        w.Write("SerializeHandler = ");
                        if (_info.HasStandardObject())
                        {
                            w.Write("null");
                        }
                        else
                        {
                            w.Write(_info.FileName)
                            .Write("SerializeHandler");
                        }
                    });
                }, endSemi: true);
                RestObject(w, variable);
            }
            private void InitVariableValue(ICodeBlock w, VariableInformation variable)
            {
                if (_info.TypeCategory == EnumTypeCategory.CustomEnum && _info.Nullable == false)
                {
                    return;
                }
                w.WriteLine(w =>
                {
                    w.Write(variable.Camel)
                    .Write(" = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreateValueInfo");
                    if (_info.Nullable == false)
                    {
                        if (_info.TypeCategory == EnumTypeCategory.DateOnly)
                        {
                            w.Write("<global::System.DateOnly>(Options, new global::CommonBasicLibraries.AdvancedGeneralFunctionsAndProcesses.JsonSerializers.JsonDateOnlyConverter());");
                            return;
                        }
                        else if (_info.TypeCategory == EnumTypeCategory.TimeOnly)
                        {
                            w.Write("<global::System.TimeOnly>(Options, new global::CommonBasicLibraries.AdvancedGeneralFunctionsAndProcesses.JsonSerializers.JsonTimeOnlyConverter());");
                            return;
                        }
                        else if (_info.TypeCategory == EnumTypeCategory.StandardEnum)
                        {
                            w.CustomGenericWrite(_info)
                            .Write("(Options, global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.GetEnumConverter")
                            .CustomGenericWrite(_info)
                            .Write("(Options));");
                            return;
                        }
                        else
                        {
                            w.CustomGenericWrite(_info)
                            .Write("(Options, global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.")
                            .Write(_info.TypeName)
                            .Write("Converter);");
                            return;
                        }
                    }
                    else
                    {
                        w.CustomGenericWrite(_info)
                         .Write("(");
                    }
                });
                if (_info.Nullable == false)
                {
                    return;
                }
                w.WriteLine("Options,")
                .WriteLine(w =>
                {
                    w.Write("global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.GetNullableConverter")
                    .PopulateNonNullGeneric(_info)
                    .Write("(underlyingTypeInfo: ")
                    .Write(_info.TypeName)
                    .Write("));");
                });
            }
            #endregion
        }
    }
}