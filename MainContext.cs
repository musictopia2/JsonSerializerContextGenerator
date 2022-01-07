namespace JsonSerializerContextGenerator;
[IncludeCode]
internal interface IFinalConfig<T>
{
    IFinalConfig<T> Ignore<P>(Func<T, P> propertySelector);
}
internal interface IMakeConfig<T>
{
    //if there is no action for cloneable, then means nothing to ignore
    IMakeConfig<T> SourceGeneratedSerializable(Action<IFinalConfig<T>>? config = null);
    IMakeConfig<T> IgnoreProperties(Action<IFinalConfig<T>> config); //this cannot be null because don't bother using this unless you will ignore specific properties.
}
internal interface ICustomConfig
{
    ICustomConfig Make<T>(Action<IMakeConfig<T>> config);
}
internal abstract class MainContext
{
    internal const string ConfigureName = nameof(Configure);
    protected abstract void Configure(ICustomConfig config);
}