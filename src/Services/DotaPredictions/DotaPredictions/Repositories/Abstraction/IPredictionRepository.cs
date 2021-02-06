using Data.Core;
using Data.MongoDB.Repositories;
using DotaPredictions.Models.Dto;
using MongoDB.Bson;

namespace DotaPredictions.Repositories.Abstraction
{
    public interface IPredictionRepository : IMongoRepository<PredictionDto>
    {
        
    }
}