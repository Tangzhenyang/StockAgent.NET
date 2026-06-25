from app.core.cache import SimpleTtlCache
from app.core.config import get_settings
from app.models.contracts import MarketSnapshotResponse
from app.providers import akshare_market
from app.utils.ticker import normalize_ticker

_market_cache = SimpleTtlCache(maxsize=512, ttl_seconds=get_settings().cache_ttl_market_seconds)


def get_market_snapshot(raw_ticker: str) -> MarketSnapshotResponse:
    """Build the standard market snapshot response for StockAgent.NET. 为 StockAgent.NET 组装标准行情快照。"""

    normalized = normalize_ticker(raw_ticker)
    settings = get_settings()

    def load() -> MarketSnapshotResponse:
        """Load from the real market provider. 从真实行情提供器加载。"""

        snapshot = akshare_market.load_market_snapshot(normalized)
        return snapshot.model_copy(update={"cache_ttl_seconds": settings.cache_ttl_market_seconds})

    return _market_cache.get_or_set(f"market:{normalized.ticker}", load)
