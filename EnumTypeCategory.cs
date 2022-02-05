namespace JsonSerializerContextGenerator;
internal enum EnumTypeCategory
{
    StandardSimple,
    DateOnly,
    TimeOnly,
    CustomEnum,
    StandardEnum,
    Complex,
    Struct, //has to now account for structs since they require special treatment.
    Generic //for now, will only be single generic.  if somehow requires multiple, then will require rethinking.
}