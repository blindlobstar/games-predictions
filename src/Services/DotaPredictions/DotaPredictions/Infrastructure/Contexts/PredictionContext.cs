using Data.MongoDB;
using Data.MongoDB.Models;
using DotaPredictions.Models.Dto;

namespace DotaPredictions.Infrastructure.Contexts
{
    public class PredictionContext : BaseContext<PredictionDto>, IBaseContext<PredictionDto>
    {
        public PredictionContext(IDatabaseSettings databaseSettings) : base(databaseSettings)
        {
        }
    }
}