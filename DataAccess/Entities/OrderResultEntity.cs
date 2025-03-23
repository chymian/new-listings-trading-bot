using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace new_listing_bot_cs.DataAccess.Entities;

public class ExitEntity
{
    public decimal StopLossPrice { get; set; }
    public decimal TakeProfitPrice { get; set; }
    public decimal? RealizedPnl { get; set; }
}

[Owned]
public class ExchangeOrderResultEntity
{
    public string OrderId { get; set; } = null!;
    public string MarketSymbol { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal AmountFilled { get; set; }
    public decimal Price { get; set; }
    public bool IsBuy { get; set; }
    public DateTime OrderDate { get; set; }
}

public class OrderResultEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public ExchangeOrderResultEntity ExchangeOrderResult { get; set; } = null!;
    public ExitEntity ExitStrategy { get; set; } = null!;
}

public class PortfolioEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(OrderResultEntity))]
    public int OrderResultId { get; set; }
    public OrderResultEntity OrderResult { get; set; } = null!;
}

