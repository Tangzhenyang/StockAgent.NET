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
