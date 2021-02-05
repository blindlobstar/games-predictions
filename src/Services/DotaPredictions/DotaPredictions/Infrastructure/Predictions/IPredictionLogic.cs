namespace DotaPredictions.Infrastructure.Predictions
{
    public interface IPredictionLogic<in TData, in TParameters>
    {
        CheckResult Check(TData data, TParameters parameters);
    }
}