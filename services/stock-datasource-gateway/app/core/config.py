import os
from dataclasses import dataclass
from functools import lru_cache


@dataclass(frozen=True)
class Settings:
    """Environment-driven service settings. 基于环境变量的服务配置。"""

    data_source_api_key: str
    cache_ttl_market_seconds: int
    cache_ttl_evidence_seconds: int
    request_timeout_seconds: int
    max_evidence_documents: int
    max_evidence_text_characters: int


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    """Return cached settings to avoid reparsing environment variables. 返回缓存配置，避免重复解析环境变量。"""

    return Settings(
        data_source_api_key=os.getenv("DATA_SOURCE_API_KEY", "dev-secret"),
        cache_ttl_market_seconds=_get_int("CACHE_TTL_MARKET_SECONDS", 60),
        cache_ttl_evidence_seconds=_get_int("CACHE_TTL_EVIDENCE_SECONDS", 3600),
        request_timeout_seconds=_get_int("REQUEST_TIMEOUT_SECONDS", 20),
        max_evidence_documents=_get_int("MAX_EVIDENCE_DOCUMENTS", 8),
        max_evidence_text_characters=_get_int("MAX_EVIDENCE_TEXT_CHARACTERS", 12000),
    )


def _get_int(name: str, default: int) -> int:
    """Read an integer environment variable with a safe default. 读取整数环境变量，失败时使用安全默认值。"""

    value = os.getenv(name)
    if value is None:
        return default

    try:
        return int(value)
    except ValueError:
        return default
