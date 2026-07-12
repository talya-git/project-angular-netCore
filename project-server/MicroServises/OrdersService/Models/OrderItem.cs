namespace OrdersService.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int GiftId { get; set; } // מזהה המתנה שמגיע משירות ה-Gifts
        public int Quantity { get; set; } // כמות כרטיסים
        public decimal UnitPrice { get; set; } // מחיר הכרטיס בזמן הקנייה
    }
}