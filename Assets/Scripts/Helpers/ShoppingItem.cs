public class ShoppingItem
{
    public string ItemName { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }

    public Department ProductCategory { get; set; }

    public PurchaseType PurchaseType { get; set; }

    public ShoppingItem(string itemName, int price, int quantity, Department productCategory, PurchaseType purchaseType)
    {
        this.ItemName = itemName;
        this.Price = price;
        this.Quantity = quantity;
        this.ProductCategory = productCategory;
        this.PurchaseType = purchaseType;
    }

}
