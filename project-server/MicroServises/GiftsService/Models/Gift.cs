using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GiftsService.Models
{
    public class Gift
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [BsonElement("price")]
        public decimal Price { get; set; }
        public string DonorId { get; set; } = string.Empty;
        public Donor? Donor { get; set; }
        public decimal PriceCard { get => Price; set => Price = value; }
    }
}