namespace GiftsService.Models
{
    public class Gift
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DonorId { get; set; }
        public Donor? Donor { get; set; }
    }
}