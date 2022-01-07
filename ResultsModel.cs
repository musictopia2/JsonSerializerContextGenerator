namespace JsonSerializerContextGenerator;
internal class ResultsModel
{
    public HashSet<string> PropertyNames { get; set; } = new();
    public HashSet<TypeModel> Types = new();
    public string NamespaceName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string GetGlobalName => $"global::{NamespaceName}.{ClassName}";
    public string GetGlobalMain => $"global::{NamespaceName}";
}