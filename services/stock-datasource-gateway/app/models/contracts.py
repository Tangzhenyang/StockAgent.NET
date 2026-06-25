from datetime import datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class HealthResponse(BaseModel):
    """Health payload returned by the public health endpoint. 健康检查端点返回的载荷。"""

    status: Literal["ok"] = "ok"
    service: Literal["stock-datasource-gateway"] = "stock-datasource-gateway"


class MarketSnapshotResponse(BaseModel):
    """Market and financial snapshot aligned with StockAgent.NET. 与 StockAgent.NET 对齐的行情和财务快照。"""

    model_config = ConfigDict(populate_by_name=True)

    ticker: str = Field(description="Normalized ticker, such as 00700.HK or 600519.")
    market: Literal["AShare", "HongKong"] = Field(description="Stock market enum expected by .NET.")
    company_name: str = Field(alias="companyName", description="Company display name.")
    last_price: float = Field(alias="lastPrice", description="Latest stock price.")
    market_cap: float = Field(alias="marketCap", description="Latest market capitalization.")
    pe_ratio: float = Field(alias="peRatio", description="Latest price-to-earnings ratio.")
    revenue_growth_percent: float = Field(alias="revenueGrowthPercent", description="Recent revenue growth.")
    net_margin_percent: float = Field(alias="netMarginPercent", description="Recent net margin.")
    quote_source: str | None = Field(default=None, alias="quoteSource", description="Provider endpoint used for price.")
    retrieved_at: datetime | None = Field(default=None, alias="retrievedAt", description="UTC retrieval timestamp.")
    cache_ttl_seconds: int | None = Field(default=None, alias="cacheTtlSeconds", description="Service-side market cache TTL.")
    price_freshness: Literal["intraday-delayed", "daily-close-fallback"] | None = Field(
        default=None,
        alias="priceFreshness",
        description="Whether price came from intraday delayed quote or daily close fallback.",
    )


class EvidenceDocumentResponse(BaseModel):
    """Evidence document contract aligned with StockAgent.NET WebEvidenceDocument. 与 .NET 证据文档对齐的响应契约。"""

    model_config = ConfigDict(populate_by_name=True)

    url: str = Field(description="Original source URL.")
    title: str = Field(description="Source title.")
    source_type: Literal[
        "annual-report",
        "interim-report",
        "quarterly-report",
        "announcement",
        "regulatory",
        "news",
    ] = Field(alias="sourceType", description="Evidence source category.")
    published_at: datetime | None = Field(alias="publishedAt", description="Publication timestamp.")
    text: str = Field(description="Extracted plain text used by the research pipeline.")


class IndustryNewsItemResponse(BaseModel):
    """Industry news item used by StockAgent.NET. StockAgent.NET 使用的行业新闻项。"""

    model_config = ConfigDict(populate_by_name=True)

    title: str = Field(description="News title.")
    url: str = Field(description="Source URL.")
    source: str = Field(description="Publisher or upstream provider.")
    published_at: datetime | None = Field(alias="publishedAt", description="Publication timestamp.")
    summary: str = Field(description="Short summary.")


class IndustryProfileResponse(BaseModel):
    """Industry profile and recent news for a ticker. 股票所属行业画像和近期行业新闻。"""

    model_config = ConfigDict(populate_by_name=True)

    ticker: str = Field(description="Normalized ticker.")
    company_name: str = Field(alias="companyName", description="Company display name.")
    industry_name: str = Field(alias="industryName", description="Primary industry name.")
    sectors: list[str] = Field(description="Related sectors and sub-industries.")
    keywords: list[str] = Field(description="Search keywords for industry research.")
    provider: str = Field(description="Provider used for industry classification.")
    retrieved_at: datetime = Field(alias="retrievedAt", description="UTC retrieval timestamp.")
    news: list[IndustryNewsItemResponse] = Field(description="Recent industry-related news.")
