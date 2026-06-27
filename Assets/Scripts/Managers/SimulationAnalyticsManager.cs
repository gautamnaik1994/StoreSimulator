using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;

public class PurchaseEventArgs : EventArgs
{
    public string ProductId { get; }
    public string ProductName { get; }
    public decimal Price { get; }
    public PurchaseType PurchaseType { get; }

    public PurchaseEventArgs(string productId, string productName, decimal price, PurchaseType purchaseType)
    {
        ProductId = productId;
        ProductName = productName;
        Price = price;
        PurchaseType = purchaseType;
    }
}


public class SimulationAnalyticsManager : MonoBehaviour
{
    private Dictionary<string, ProductSaleData> _salesRegistry = new Dictionary<string, ProductSaleData>();
    public UIDocument uiDocument;

    private TextElement totalSales;

    private VisualElement salesReportContainer;
    private ListView salesReportList;
    private Button closeButton;
    private float timer = 0f;
    private float interval = 10.0f; // Run code every 10 seconds

    private int liveTotalRevenue = 0;

    private void Start()
    {
        totalSales = uiDocument.rootVisualElement.Q<TextElement>("totalSales");
        salesReportList = uiDocument.rootVisualElement.Q<ListView>("SalesReportList");
        salesReportContainer = uiDocument.rootVisualElement.Q<VisualElement>("salesReport");
        closeButton = uiDocument.rootVisualElement.Q<Button>("CloseReport");
        closeButton.clicked += () => salesReportContainer.style.display = DisplayStyle.None;
    }


    private void OnEnable()
    {
        // Subscribe to the static event stream from ALL checkout counters
        CheckoutHandler.OnItemProcessed += HandleItemProcessed;
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        CheckoutHandler.OnItemProcessed -= HandleItemProcessed;
        closeButton.clicked -= () => salesReportContainer.style.display = DisplayStyle.None;
    }

    // The event handler matches the EventHandler signature
    private void HandleItemProcessed(object sender, PurchaseEventArgs e)
    {
        // Run your aggregation logic here safely using the event args payload
        string registryKey = $"{e.ProductId}_{e.PurchaseType}";

        if (_salesRegistry.TryGetValue(registryKey, out ProductSaleData existingSale))
        {
            existingSale.QuantitySold++;
        }
        else
        {
            _salesRegistry.Add(registryKey, new ProductSaleData
            {
                ProductId = e.ProductId,
                ProductName = e.ProductName,
                QuantitySold = 1,
                PricePerUnit = e.Price,
                Status = e.PurchaseType
            });
        }

        // Update live total revenue
        liveTotalRevenue += (int)e.Price;
        totalSales.text = $"SALES: ₹{liveTotalRevenue}";
    }

    /// <summary>
    /// Returns the final aggregated list sorted by highest revenue for your UI Table.
    /// </summary>
    public List<ProductSaleData> GetFinalReport()
    {
        // var finalReport = _salesRegistry.Values
        //     .OrderByDescending(item => item.TotalRevenue)
        //     .ToList();
        var finalReport = _salesRegistry.Values
            .ToList();
        return finalReport;
    }

    public void ToggleSalesReportInUI()
    {
        if (salesReportContainer.style.display == DisplayStyle.Flex)
        {
            salesReportContainer.style.display = DisplayStyle.None;
            return;
        }
        UpdateUI();

    }

    void LateUpdate()
    {
        timer += Time.deltaTime;
        if (timer >= interval && salesReportContainer.style.display == DisplayStyle.Flex)
        {
            timer = 0f;
            UpdateUI();
        }

    }

    void UpdateUI()
    {
        salesReportContainer.style.display = DisplayStyle.Flex;

        var finalReport = GetFinalReport();

        // Update the ListView's itemsSource
        salesReportList.bindItem = (element, i) =>
        {
            if (i < 0 || i >= finalReport.Count)
            {
                return;
            }
            var data = finalReport[i];
            element.Q<Label>("productName").text = data.ProductName;
            element.Q<Label>("quantitySold").text = data.QuantitySold.ToString();
            element.Q<Label>("pricePerUnit").text = $"₹{data.PricePerUnit}";
            element.Q<Label>("totalRevenue").text = $"₹{data.TotalRevenue}";
            element.Q<Label>("purchaseType").text = data.Status.ToString();
        };

        salesReportList.itemsSource = finalReport;
        salesReportList.Rebuild();
    }
}