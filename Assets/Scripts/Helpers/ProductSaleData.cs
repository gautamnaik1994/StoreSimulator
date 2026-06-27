using System;

[Serializable]
public class ProductSaleData
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int QuantitySold { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalRevenue => QuantitySold * PricePerUnit;

    // Status tracking for how it was bought
    public PurchaseType Status { get; set; }
}

public enum PurchaseType
{
    Standard,          // Fixed item in sales
    MajorImpulse,      // Large speed bump
    MinorImpulse       // Smaller speed bump
}