namespace InventoryService.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public int GiftId { get; set; } // הקישור למתנה משירות ה-Gifts (מנוהל לוגית בלבד, לכן אין פה Navigation Property)
        public int AvailableStock { get; set; }
    }
}