from datetime import UTC, datetime
from typing import Any

from app.models.contracts import EvidenceDocumentResponse
from app.providers.errors import DataSourceProviderError
from app.utils.ticker import NormalizedTicker


def load_hk_announcements(
    normalized: NormalizedTicker,
    company_name: str,
    max_documents: int,
) -> list[EvidenceDocumentResponse]:
    """Load real Hong Kong financial evidence and HKEXnews trace links. 加载真实港股财务证据和 HKEXnews 可追溯链接。"""

    try:
        import akshare as ak  # type: ignore[import-not-found]
    except Exception as exc:
        raise DataSourceProviderError(
            "AKShare is not installed or cannot be imported.",
            provider="akshare-hk-evidence",
            retryable=False,
        ) from exc

    numeric_code = normalized.ticker.removesuffix(".HK")
    financial_row = _load_financial_row(ak, numeric_code)
    if financial_row is None:
        raise DataSourceProviderError(
            f"No real Hong Kong financial evidence found for {normalized.ticker}.",
            provider="akshare-hk-evidence",
            retryable=True,
        )

    display_name = company_name or str(_pick(financial_row, "SECURITY_NAME_ABBR", "证券简称", default=normalized.ticker))
    hkex_url = f"https://www1.hkexnews.hk/search/titlesearch.xhtml?stock_code={numeric_code}"
    documents = [
        EvidenceDocumentResponse(
            url=hkex_url,
            title=f"{display_name} 财务指标证据",
            sourceType="annual-report",
            publishedAt=datetime.now(UTC),
            text=_build_financial_text(display_name, financial_row),
        ),
        EvidenceDocumentResponse(
            url=hkex_url,
            title=f"{display_name} HKEXnews 公告检索入口",
            sourceType="announcement",
            publishedAt=datetime.now(UTC),
            text=f"{display_name} 的港交所公告可通过 HKEXnews 按股票代码 {numeric_code} 检索，作为年报、中报、公告及通函的原始来源入口。",
        ),
    ]
    return documents[:max_documents]


def _load_financial_row(ak: Any, numeric_code: str) -> dict[str, Any] | None:
    """Load latest Hong Kong financial indicator row from AKShare. 从 AKShare 加载最新港股财务指标行。"""

    try:
        frame = ak.stock_hk_financial_indicator_em(symbol=numeric_code)
    except Exception:
        return None

    if getattr(frame, "empty", True):
        return None

    return frame.iloc[0].to_dict()


def _build_financial_text(company_name: str, row: dict[str, Any]) -> str:
    """Build concise evidence text from real Hong Kong financial indicators. 从真实港股财务指标组装证据文本。"""

    revenue = _pick(row, "营业总收入", default="未知")
    revenue_growth = _pick(row, "营业总收入滚动环比增长(%)", default="未知")
    net_margin = _pick(row, "销售净利率(%)", default="未知")
    pe_ratio = _pick(row, "市盈率", default="未知")
    market_cap = _pick(row, "总市值(港元)", "港股市值(港元)", default="未知")
    profit = _pick(row, "净利润", default="未知")

    return (
        f"{company_name} 最近报告期财务指标：营业总收入 {revenue}，"
        f"营业总收入滚动环比增长 {revenue_growth}%，销售净利率 {net_margin}%，"
        f"净利润 {profit}，市盈率 {pe_ratio}，总市值 {market_cap} 港元。"
    )


def _pick(row: dict[str, Any], *keys: str, default: Any) -> Any:
    """Pick the first non-empty row value. 选择第一个非空行值。"""

    for key in keys:
        value = row.get(key)
        if value not in (None, "", "-"):
            return value
    return default
