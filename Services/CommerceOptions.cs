namespace Webpcvaphukienkethopchatbot.Services;

public sealed class CommerceOptions
{
    public decimal FlatShippingFee { get; set; } = 30_000;

    public decimal FreeShippingThreshold { get; set; } = 20_000_000;

    public int PendingOrderExpirationHours { get; set; } = 24;
}
