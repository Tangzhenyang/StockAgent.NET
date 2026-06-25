from datetime import UTC, datetime

from app.models.contracts import IndustryNewsItemResponse, IndustryProfileResponse
from app.utils.ticker import normalize_ticker


_KNOWN_INDUSTRIES = {
    "301308": {
        "company_name": "江波龙",
        "industry_name": "半导体存储",
        "sectors": ["半导体", "存储芯片", "Flash", "DRAM", "嵌入式存储", "企业级存储"],
        "keywords": ["存储芯片", "DRAM", "NAND Flash", "存储模组", "江波龙", "半导体周期"],
    }
}


def get_industry_profile(raw_ticker: str) -> IndustryProfileResponse:
    """Return industry profile and recent industry news for a ticker. 返回股票行业画像和近期行业新闻。"""

    normalized = normalize_ticker(raw_ticker)
    profile = _KNOWN_INDUSTRIES.get(normalized.ticker, _fallback_profile(normalized.ticker))
    return IndustryProfileResponse(
        ticker=normalized.ticker,
        companyName=profile["company_name"],
        industryName=profile["industry_name"],
        sectors=profile["sectors"],
        keywords=profile["keywords"],
        provider="akshare-news-with-local-industry-map",
        retrievedAt=datetime.now(UTC),
        news=_load_industry_news(normalized.ticker, profile["keywords"]),
    )


def _fallback_profile(ticker: str) -> dict[str, list[str] | str]:
    """Return a generic profile when no explicit local mapping exists. 无显式映射时返回通用画像。"""

    return {
        "company_name": ticker,
        "industry_name": "待识别行业",
        "sectors": ["待识别行业"],
        "keywords": [ticker, "行业新闻", "产业链", "业绩驱动"],
    }


def _load_industry_news(ticker: str, keywords: list[str]) -> list[IndustryNewsItemResponse]:
    """Load recent news from AKShare when available. 可用时从 AKShare 获取近期新闻。"""

    try:
        import akshare as ak  # type: ignore[import-not-found]

        frame = ak.stock_news_em(symbol=ticker)
    except Exception:
        frame = None

    if frame is None or getattr(frame, "empty", True):
        return _fallback_news(keywords)

    records = []
    for row in frame.to_dict("records"):
        title = str(_pick(row, "新闻标题", "标题", "title", default="行业新闻"))
        content = str(_pick(row, "新闻内容", "摘要", "summary", default=title))
        if not _matches_keywords(f"{title} {content}", keywords):
            continue

        records.append(
            IndustryNewsItemResponse(
                title=title,
                url=str(_pick(row, "新闻链接", "链接", "url", default="")),
                source=str(_pick(row, "文章来源", "来源", "source", default="akshare")),
                publishedAt=_parse_datetime(_pick(row, "发布时间", "时间", "datetime", default=None)),
                summary=content[:500],
            )
        )
        if len(records) >= 8:
            break

    return records or _fallback_news(keywords)


def _fallback_news(keywords: list[str]) -> list[IndustryNewsItemResponse]:
    """Return deterministic fallback industry notes. 返回确定性行业兜底信息。"""

    keyword_text = "、".join(keywords[:4])
    return [
        IndustryNewsItemResponse(
            title=f"{keyword_text} 行业信息待补充",
            url="https://example.local/industry-news",
            source="fallback",
            publishedAt=datetime.now(UTC),
            summary=f"当前数据源未返回可复核的最新行业新闻，后续需要围绕 {keyword_text} 补充公开来源。",
        )
    ]


def _matches_keywords(text: str, keywords: list[str]) -> bool:
    """Return whether text contains any industry keyword. 判断文本是否包含行业关键词。"""

    lowered = text.lower()
    return any(keyword.lower() in lowered for keyword in keywords)


def _pick(row: dict, *keys: str, default):
    """Pick first existing row value. 选择行中第一个存在的值。"""

    for key in keys:
        value = row.get(key)
        if value not in (None, "", "-"):
            return value
    return default


def _parse_datetime(value) -> datetime | None:
    """Parse provider datetime when possible. 尽量解析提供器时间。"""

    if value is None:
        return None

    try:
        parsed = datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    except ValueError:
        return None

    return parsed if parsed.tzinfo else parsed.replace(tzinfo=UTC)
