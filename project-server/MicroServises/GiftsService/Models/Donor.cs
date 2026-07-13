using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GiftsService.Models
{
    public class Donor
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Name => $"{FirstName} {LastName}".Trim();
    }
}