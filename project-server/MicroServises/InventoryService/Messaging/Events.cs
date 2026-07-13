namespace InventoryService.Messaging
{
    public record OrderPlacedEvent(int OrderId, string CorrelationId, List<OrderItemDto> Items);
    public record OrderItemDto(int GiftId, int Quantity);
    public record InventoryReservedEvent(int OrderId, string CorrelationId);
    public record InventoryRejectedEvent(int OrderId, string CorrelationId, string Reason);
}
