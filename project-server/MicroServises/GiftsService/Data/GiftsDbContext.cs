using GiftsService.Models;
using MongoDB.Driver;

namespace GiftsService.Data
{
    public class GiftsDbContext
    {
        private readonly IMongoDatabase _database;

        public GiftsDbContext(IMongoDatabase database)
        {
            _database = database;
        }

        public IMongoCollection<Gift> Gifts => _database.GetCollection<Gift>("gifts");
        public IMongoCollection<Donor> Donors => _database.GetCollection<Donor>("donors");
    }
}
