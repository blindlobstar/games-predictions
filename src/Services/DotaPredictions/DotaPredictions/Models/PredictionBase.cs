namespace DotaPredictions.Models
{
    public abstract class PredictionBase<T>
    {
        public string UserId { get; set; }
        public ulong SteamId { get; set; }
        public string PredictionType { get; set; }
        public T Parameters { get; set; }
    }
}