using Data.MongoDB;
using Data.MongoDB.Repositories;
using DotaPredictions.Models.Dto;
using DotaPredictions.Repositories.Abstraction;

namespace DotaPredictions.Repositories.Implementation
{
    public class PredictionRepository : BaseRepository<PredictionDto>, IPredictionRepository
    {
        public PredictionRepository(IBaseContext<PredictionDto> context) : base(context)
        {
        }
    }
}