namespace JsonSerializerContextGenerator;
internal static class SourceBuilderExtensions
{
    public static void WriteContext(this SourceCodeStringBuilder builder, Action<ICodeBlock> action, ResultsModel result)
    {
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
                    .Write("Context");
                })
                .WriteCodeBlock(w =>
                {
                    action.Invoke(w);
                });
    }
}