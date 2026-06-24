from datetime import UTC, datetime, timedelta
from typing import Any

from app.models.contracts import EvidenceDocumentResponse
from app.providers.errors import DataSourceProviderError
from app.utils.ticker import NormalizedTicker


def load_a_share_announcements(
    normalized: NormalizedTicker,
    company_name: str,
    max_documents: int,
) -> list[EvidenceDocumentResponse]:
    """Load real A-share announcement evidence from CNInfo through AKShare. 通过 AKShare 从巨潮加载真实 A 股公告证据。"""

    try:
        import akshare as ak  # type: ignore[import-not-found]
    except Exception as exc:
        raise DataSourceProviderError(
            "AKShare is not installed or cannot be imported.",
            provider="akshare-cninfo",
            retryable=False,
        ) from exc

    rows = _load_cninfo_rows(ak, normalized, max_documents)
    if not rows:
        raise DataSourceProviderError(
            f"No real CNInfo announcements found for {normalized.ticker}.",
            provider="akshare-cninfo",
            retryable=True,
        )

    documents = [_row_to_document(row, company_name or _pick(row, "简称", default=normalized.ticker)) for row in rows]
    return documents[:max_documents]


def _load_cninfo_rows(ak: Any, normalized: NormalizedTicker, max_documents: int) -> list[dict[str, Any]]:
    """Load annual and general announcement rows from CNInfo. 从巨潮加载年报和普通公告行。"""

    end_date = datetime.now(UTC).strftime("%Y%m%d")
    start_date = (datetime.now(UTC) - timedelta(days=900)).strftime("%Y%m%d")
    rows: list[dict[str, Any]] = []
    categories = ["年报", "半年报", "一季报", "三季报", ""]

    for category in categories:
        try:
            frame = ak.stock_zh_a_disclosure_report_cninfo(
                symbol=normalized.ticker,
                market="沪深京",
                category=category,
                start_date=start_date,
                end_date=end_date,
            )
        except Exception:
            continue

        if getattr(frame, "empty", True):
            continue

        for row in frame.to_dict("records"):
            rows.append(row)
            if len(rows) >= max_documents:
                return rows

    return rows


def _row_to_document(row: dict[str, Any], company_name: str) -> EvidenceDocumentResponse:
    """Convert a CNInfo row to the StockAgent.NET evidence contract. 将巨潮行转换为 StockAgent.NET 证据契约。"""

    title = str(_pick(row, "公告标题", "title", default=f"{company_name} 公告"))
    published_at = _parse_date(_pick(row, "公告时间", "date", default=None))
    url = str(_pick(row, "公告链接", "url", default=f"http://www.cninfo.com.cn/new/disclosure/stock?stockCode={company_name}"))
    source_type = _classify_title(title)
    text = f"{title}。公告来源为巨潮资讯，发布时间为{published_at.date().isoformat() if published_at else '未知'}，原始链接：{url}"

    return EvidenceDocumentResponse(
        url=url,
        title=title,
        sourceType=source_type,
        publishedAt=published_at,
        text=text,
    )


def _classify_title(title: str) -> str:
    """Classify CNInfo announcement title into a stable source type. 将巨潮公告标题归类为稳定来源类型。"""

    if "年度报告" in title or "年报" in title:
        return "annual-report"
    if "半年度报告" in title or "半年报" in title:
        return "interim-report"
    if "季度报告" in title or "一季报" in title or "三季报" in title:
        return "quarterly-report"
    if "监管" in title or "问询" in title:
        return "regulatory"
    return "announcement"


def _parse_date(value: Any) -> datetime | None:
    """Parse provider date values into UTC datetimes. 将提供器日期值解析为 UTC 时间。"""

    if value is None:
        return None

    text = str(value)[:10]
    try:
        return datetime.fromisoformat(text).replace(tzinfo=UTC)
    except ValueError:
        return None


def _pick(row: dict[str, Any], *keys: str, default: Any) -> Any:
    """Pick the first non-empty row value. 选择第一个非空行值。"""

    for key in keys:
        value = row.get(key)
        if value not in (None, "", "-"):
            return value
    return default
