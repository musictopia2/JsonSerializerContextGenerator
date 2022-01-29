namespace JsonSerializerContextGenerator;
internal enum EnumTypeCategory
{
    StandardSimple,
    DateOnly,
    TimeOnly,
    CustomEnum,
    StandardEnum,
    Complex,
    Struct //has to now account for structs since they require special treatment.
}