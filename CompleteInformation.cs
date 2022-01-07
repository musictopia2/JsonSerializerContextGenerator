namespace JsonSerializerContextGenerator;
internal class CompleteInformation
{
    public BasicList<ResultsModel> Results { get; set; } = new();
    public BasicList<IPropertySymbol> PropertiesToIgnore { get; set; } = new();
}