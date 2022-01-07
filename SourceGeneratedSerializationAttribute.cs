global using JsonSerializerContextGenerator;
namespace JsonSerializerContextGenerator;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class SourceGeneratedSerializationAttribute : Attribute
{
}