using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProductDatabase", menuName = "Inventory/Product Database")]
public class ProductDatabase : ScriptableObject
{
    // Prepopulated with average Indian market prices as integer values (in INR ₹)
    public List<ProductData> products = new List<ProductData>()
    {
        // --- Set 1 ---
        new ProductData("FrozenFoods", 150),
        new ProductData("Juice", 110),
        new ProductData("SoftDrink", 40),
        new ProductData("Milk", 33),
        new ProductData("FrozenChicken", 280),
        new ProductData("FrozenMeat", 450),
        new ProductData("Gifts", 350),
        new ProductData("Toys", 499),
        new ProductData("PartySupplies", 199),
        new ProductData("PersonalCare", 120),
        new ProductData("Cosmetics", 250),
        new ProductData("Rice", 65),
        new ProductData("Wheat", 45),
        new ProductData("OralCare", 90),
        new ProductData("IceCream", 50),
        new ProductData("Chocolates", 40),
        new ProductData("LargeIceCream", 250),
        new ProductData("GreetingCards", 70),
        new ProductData("FreshCutMeat", 400),
        new ProductData("Fruits", 120),
        new ProductData("OrganicGreen", 80),
        new ProductData("Bakery", 60),
        new ProductData("Seafood", 550),
        new ProductData("Vegetables", 50),
        new ProductData("Grains", 90),
        new ProductData("Detergent", 140),
        new ProductData("Toiletries", 85),
        new ProductData("BabyCare", 180),
        new ProductData("MensClothing", 799),
        new ProductData("KidsClothing", 450),
        new ProductData("Shoes", 1200),
        new ProductData("DogFood", 320),
        new ProductData("CatFood", 290),
        new ProductData("Diapers", 650),
        new ProductData("WomensClothing", 899),
        new ProductData("Books", 299),
        new ProductData("Magazines", 100),
        new ProductData("Stationery", 50),
        new ProductData("BoardGames", 599),
        new ProductData("CookingOil", 160),
        new ProductData("CannedFood", 130),
        new ProductData("Cereals", 180),
        new ProductData("Tea", 140),
        new ProductData("Coffee", 220),
        new ProductData("InstantNoodles", 90),
        new ProductData("Butter", 105),
        new ProductData("Paneer", 120),


    };
    private Dictionary<string, ProductData> productDictionary = new Dictionary<string, ProductData>();
    private bool isDictionaryBuilt = false;
    // --- The O(1) Indexer ---
    public ProductData this[string name]
    {
        get
        {
            // BULLETPROOF: If the dictionary isn't ready or was cleared, rebuild it on demand
            if (!isDictionaryBuilt || productDictionary.Count == 0)
            {
                BuildDictionary();
            }

            string cleanName = name.Trim(); // Protect against "Cereals " with trailing spaces

            if (productDictionary.TryGetValue(cleanName, out ProductData product))
            {
                return product;
            }

            Debug.LogWarning($"Product '{cleanName}' not found! Database has {productDictionary.Count} entries.");
            return default;
        }
    }

    private void BuildDictionary()
    {
        productDictionary.Clear();
        foreach (var product in products)
        {
            if (product.productName == null) continue;

            string cleanKey = product.productName.Trim();
            if (!productDictionary.ContainsKey(cleanKey))
            {
                productDictionary.Add(cleanKey, product);
            }
        }
        isDictionaryBuilt = true;
    }

    public void OnAfterDeserialize()
    {
        BuildDictionary();
    }
}

[System.Serializable]
public struct ProductData
{
    public string productName;
    public int price; // Changed to int to meet your integer requirement

    public ProductData(string name, int itemPrice)
    {
        productName = name;
        price = itemPrice;
    }
}