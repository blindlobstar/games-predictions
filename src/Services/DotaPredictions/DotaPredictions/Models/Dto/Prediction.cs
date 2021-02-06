using Data.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotaPredictions.Models.Dto
{
    public class PredictionDto : PredictionBase<dynamic>, IEntity<string>
    {
        public bool IsFinished { get; set; }
        
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
    }
}