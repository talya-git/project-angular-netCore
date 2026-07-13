namespace NotificationService.Messaging
{
    public record InventoryReservedEvent(int OrderId, string CorrelationId);
    public record InventoryRejectedEvent(int OrderId, string CorrelationId, string Reason);
}
