namespace DotaPredictions.Models.Dto
{
    public class Prediction : PredictionBase<dynamic>
    {
        public bool IsFinished { get; set; }
    }
}