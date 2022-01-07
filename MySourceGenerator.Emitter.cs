namespace JsonSerializerContextGenerator;
public partial class MySourceGenerator
{
    private sealed partial class Emitter
    {
        private readonly SourceProductionContext _context;
        private readonly BasicList<ResultsModel> _results;
        private readonly Compilation _compilation;
        private readonly BasicList<IPropertySymbol> _ignoreProperties;
        public Emitter(SourceProductionContext context, CompleteInformation info, Compilation compilation)
        {
            _context = context;
            _results = info.Results;
            _ignoreProperties = info.PropertiesToIgnore;
            _compilation = compilation;
        }
        private void RaiseException(ResultsModel result, TypeModel info)
        {
            _context.ReportDiagnostic(Diagnostic.Create(MixedRecordProblem(result, info), Location.None));
        }
        private bool HasExceptions()
        {
            foreach (var r in _results)
            {
                foreach (var t in r.Types)
                {
                    if (t.RecordCategory == EnumRecordCategory.Mixed)
                    {
                        RaiseException(r, t);
                        return true;
                    }
                }
            }
            return false;
        }
        public void Emit()
        {
            bool exceptions = HasExceptions();
            if (exceptions)
            {
                return;
            }
            ProcessBasicResults();
            AddGlobal();
        }
        private void ProcessBasicResults()
        {
            foreach (var item in _results)
            {
                if (item.PropertyNames.Count == 0)
                {
                    continue;
                }
                ProcessSingleResult(item);
            }
        }
        private void AddGlobal()
        {
            SourceCodeStringBuilder builder = new();
            string ns = _compilation.AssemblyName!;
            builder.WriteLine("#nullable enable")
            .WriteLine(w =>
            {
                w.Write("namespace ")
                .Write(ns)
                .Write(".JsonContextProcesses;");
            })
            .WriteLine("public static class GlobalJsonContextClass")
            .WriteCodeBlock(w =>
            {
                w.WriteLine("public static void AddJsonContexts()")
                .WriteCodeBlock(w =>
                {
                    if (Parser.HadCustomEnum)
                    {
                        w.WriteLine(w =>
                        {
                            w.Write("global::")
                            .Write(ns)
                            .Write(".JsonConverterProcesses.GlobalJsonConverterClass.AddEnumConverters();");
                        });
                    }
                    foreach (var r in _results)
                    {
                        w.WriteLine(w =>
                        {
                            w.Write("global::CommonBasicLibraries.AdvancedGeneralFunctionsAndProcesses.JsonSerializers.MultipleContextHelpers.AddContext<")
                            .Write(r.GetGlobalName)
                            .Write(">(options => options.AddContext<")
                            .Write(r.GetGlobalName)
                            .Write("Context>());");
                        });
                    }
                });
            });
            _context.AddSource("generatedglobal.g", builder.ToString());
        }
        private void AddSource(string fileName, Action<ICodeBlock, ResultsModel> action, ResultsModel result)
        {
            SourceCodeStringBuilder builder = new();
            builder.WriteContext(w =>
            {
                action.Invoke(w, result);
            }, result);
            if (fileName != "")
            {
                _context.AddSource($"{result.ClassName}Context.{fileName}.g", builder.ToString());
            }
            else
            {
                _context.AddSource($"{result.ClassName}Context.g", builder.ToString());
            }
        }
        private void ProcessSingleResult(ResultsModel result)
        {
            AddSource("PropertyNames", ProcessPropertyNameFile, result);
            ProcessBasicMainType(result);
            AddSource("", ProcessEmptyOne, result);
            PrivateProcessTypes(result);
        }
        private void ProcessPropertyNameFile(ICodeBlock w, ResultsModel result)
        {
            foreach (var p in result.PropertyNames)
            {
                w.WriteLine(w =>
                {
                    w.Write("private static readonly global::System.Text.Json.JsonEncodedText PropName_")
                    .Write(p)
                    .Write(" = global::System.Text.Json.JsonEncodedText.Encode(")
                    .AppendDoubleQuote(p)
                    .Write(");");
                });
            }
        }
        private void ProcessEmptyOne(ICodeBlock w, ResultsModel result)
        {
            void WriteContext(IWriter w)
            {
                w.Write(result.GetGlobalName)
                    .Write("Context");
            }
            void WriteUsed(IWriter w)
            {
                w.Write(result.ClassName)
                    .Write("Context");
            }
            w.WriteLine("private static global::System.Text.Json.JsonSerializerOptions s_defaultOptions { get; }  = new global::System.Text.Json.JsonSerializerOptions()")
                .WriteCodeBlock(w =>
                {
                    w.WriteLine("DefaultIgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.Never,")
                    .WriteLine("IgnoreReadOnlyFields = true,")
                    .WriteLine("IgnoreReadOnlyProperties = true,")
                    .WriteLine("IncludeFields = false,")
                    .WriteLine("WriteIndented = true");
                }, endSemi: true)
                .WriteLine(w =>
                {
                    w.Write("private static ");
                    WriteContext(w);
                    w.Write("? s_defaultContext;");
                })
                .WriteLine(w =>
                {
                    w.Write("public static ");
                    WriteContext(w);
                    w.Write(" Default => s_defaultContext ??= new ");
                    WriteContext(w);
                    w.Write("(new global::System.Text.Json.JsonSerializerOptions(s_defaultOptions));");
                })
                .WriteLine("protected override global::System.Text.Json.JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;")
                .WriteLine(w =>
                {
                    w.Write("public ");
                    WriteUsed(w);
                    w.Write("() : base(null) {}");
                })
                .WriteLine(w =>
                {
                    w.Write("public ");
                    WriteUsed(w);
                    w.Write("(global::System.Text.Json.JsonSerializerOptions options) : base(options){}");
                })
                .WriteLine("private global::System.Text.Json.Serialization.JsonConverter? GetRuntimeProvidedCustomConverter(global::System.Type type)")
                .WriteCodeBlock(w =>
                {
                    w.WriteLine("global::System.Collections.Generic.IList<global::System.Text.Json.Serialization.JsonConverter> converters = Options.Converters;")
                    .WriteLine("for (int i = 0; i < converters.Count; i++)")
                    .WriteCodeBlock(w =>
                    {
                        w.WriteLine("global::System.Text.Json.Serialization.JsonConverter? converter = converters[i];")
                        .WriteLine("if (converter.CanConvert(type))")
                        .WriteCodeBlock(w =>
                        {
                            w.WriteLine("if (converter is global::System.Text.Json.Serialization.JsonConverterFactory factory)")
                            .WriteCodeBlock(w =>
                            {
                                w.WriteLine("converter = factory.CreateConverter(type, Options);")
                                .WriteLine("if (converter == null || converter is global::System.Text.Json.Serialization.JsonConverterFactory)")
                                .WriteCodeBlock(w =>
                                {
                                    w.WriteLine(w =>
                                    {
                                        w.Write("throw new global::System.InvalidOperationException(string.Format(")
                                        .AppendDoubleQuote("The converter '{0}' cannot return null or a JsonConverterFactory instance.")
                                        .Write(", factory.GetType()));");
                                    });
                                });
                            }).WriteLine("return converter;");
                        });
                    })
                    .WriteLine("return null;");
                });
        }
        private void ProcessBasicMainType(ResultsModel result)
        {
            SourceCodeStringBuilder builder = new();
            builder.WriteLine("#nullable enable")
                .WriteLine(w =>
                {
                    w.Write("namespace ")
                    .Write(result.NamespaceName)
                    .Write(";");
                })
                .WriteLine(w =>
                {
                    w.Write("internal partial class ")
                    .Write(result.ClassName)
                    .Write("Context :  global::System.Text.Json.Serialization.JsonSerializerContext");
                })
                .WriteCodeBlock(w =>
                {
                    w.WriteLine("public override global::System.Text.Json.Serialization.Metadata.JsonTypeInfo GetTypeInfo(global::System.Type type)")
                    .WriteCodeBlock(w =>
                    {
                        w.WriteLine(w =>
                        {
                            w.Write("if (type == typeof(")
                            .Write(result.ClassName)
                            .Write("))");
                        })
                        .WriteCodeBlock(w =>
                        {
                            w.WriteLine(w =>
                            {
                                w.Write("return ")
                                .Write(result.ClassName)
                                .Write(";");
                            });
                        })
                        .WriteLine("return null!;");
                    });
                });
            _context.AddSource($"{result.ClassName}Context.GetJsonTypeInfo.g", builder.ToString());
        }
        private void PrivateProcessTypes(ResultsModel result)
        {
            foreach (var item in result.Types)
            {
                ProcessTypesClass procs = new(result, item, _context, _ignoreProperties);
                procs.ProcessTypes();
            }
        }
    }
}