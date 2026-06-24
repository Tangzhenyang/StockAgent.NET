from app.core.cache import SimpleTtlCache
from app.core.config import get_settings
from app.models.contracts import EvidenceDocumentResponse
from app.providers import cninfo_announcements, hkex_announcements
from app.providers.text_extract import trim_text
from app.utils.ticker import normalize_ticker

_settings = get_settings()
_evidence_cache = SimpleTtlCache(maxsize=512, ttl_seconds=_settings.cache_ttl_evidence_seconds)


def search_evidence_documents(raw_ticker: str, company_name: str = "") -> list[EvidenceDocumentResponse]:
    """Build evidence documents for StockAgent.NET. 为 StockAgent.NET 组装证据文档。"""

    normalized = normalize_ticker(raw_ticker)
    cache_key = f"evidence:{normalized.ticker}:{company_name}"

    def load() -> list[EvidenceDocumentResponse]:
        """Load evidence from the market-specific announcement provider. 从市场对应公告提供器加载证据。"""

        if normalized.market == "HongKong":
            documents = hkex_announcements.load_hk_announcements(normalized, company_name, _settings.max_evidence_documents)
        else:
            documents = cninfo_announcements.load_a_share_announcements(
                normalized,
                company_name,
                _settings.max_evidence_documents,
            )

        return [
            document.model_copy(update={"text": trim_text(document.text, _settings.max_evidence_text_characters)})
            for document in documents
        ]

    return _evidence_cache.get_or_set(cache_key, load)
