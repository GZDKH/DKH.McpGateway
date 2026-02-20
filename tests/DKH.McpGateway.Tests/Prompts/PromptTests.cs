using DKH.McpGateway.Application.Prompts;

namespace DKH.McpGateway.Tests.Prompts;

public class PromptTests
{
    [Fact]
    public void AnalyzeCatalog_ContainsCatalogName()
    {
        var result = AnalyzeCatalogPrompt.Execute(catalogSeoName: "electronics");

        result.Should().Contain("electronics");
    }

    [Fact]
    public void AnalyzeCatalog_DefaultParams_ContainsMainCatalog()
    {
        var result = AnalyzeCatalogPrompt.Execute();

        result.Should().Contain("main-catalog");
        result.Should().Contain("ru");
    }

    [Fact]
    public void AnalyzeCatalog_ContainsToolReferences()
    {
        var result = AnalyzeCatalogPrompt.Execute();

        result.Should().Contain("list_catalogs");
        result.Should().Contain("list_categories");
        result.Should().Contain("search_products");
    }

    [Fact]
    public void SalesReport_ContainsPeriod()
    {
        var result = SalesReportPrompt.Execute(
            periodStart: "2025-01-01", periodEnd: "2025-01-31");

        result.Should().Contain("2025-01-01");
        result.Should().Contain("2025-01-31");
    }

    [Fact]
    public void SalesReport_WithStorefront_ContainsFilter()
    {
        var result = SalesReportPrompt.Execute(
            periodStart: "2025-01-01", periodEnd: "2025-01-31",
            storefrontCode: "main");

        result.Should().Contain("main");
        result.Should().Contain("storefront");
    }

    [Fact]
    public void SalesReport_WithoutStorefront_ContainsAllStorefronts()
    {
        var result = SalesReportPrompt.Execute(
            periodStart: "2025-01-01", periodEnd: "2025-01-31");

        result.Should().Contain("all storefronts");
    }

    [Fact]
    public void StorefrontAudit_ContainsCode()
    {
        var result = StorefrontAuditPrompt.Execute(storefrontCode: "main");

        result.Should().Contain("main");
        result.Should().Contain("get_storefront");
    }

    [Fact]
    public void ReviewAnalysis_WithProduct_ContainsProductName()
    {
        var result = ReviewAnalysisPrompt.Execute(productSeoName: "widget-pro");

        result.Should().Contain("widget-pro");
        result.Should().Contain("product");
    }

    [Fact]
    public void ReviewAnalysis_Global_ContainsAllProducts()
    {
        var result = ReviewAnalysisPrompt.Execute();

        result.Should().Contain("all products");
    }

    [Fact]
    public void DataQualityCheck_ScopeAll_ContainsBothSections()
    {
        var result = DataQualityCheckPrompt.Execute(scope: "all");

        result.Should().Contain("Product data checks");
        result.Should().Contain("Reference data checks");
    }

    [Fact]
    public void DataQualityCheck_ScopeProducts_ContainsOnlyProductSection()
    {
        var result = DataQualityCheckPrompt.Execute(scope: "products");

        result.Should().Contain("Product data checks");
        result.Should().NotContain("Reference data checks");
    }

    [Fact]
    public void DataQualityCheck_ScopeReferences_ContainsOnlyReferenceSection()
    {
        var result = DataQualityCheckPrompt.Execute(scope: "references");

        result.Should().Contain("Reference data checks");
        result.Should().NotContain("Product data checks");
    }

    [Fact]
    public void DataQualityCheck_ContainsLanguageCode()
    {
        var result = DataQualityCheckPrompt.Execute(languageCode: "en");

        result.Should().Contain("en");
    }
}
