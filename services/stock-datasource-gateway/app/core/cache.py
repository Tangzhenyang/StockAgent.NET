from collections.abc import Callable
from typing import TypeVar

from cachetools import TTLCache

TValue = TypeVar("TValue")


class SimpleTtlCache:
    """Small wrapper around cachetools to keep cache usage explicit. 对 cachetools 的轻量包装，让缓存使用更清晰。"""

    def __init__(self, maxsize: int, ttl_seconds: int) -> None:
        """Create an in-process TTL cache. 创建进程内 TTL 缓存。"""

        self._cache: TTLCache[str, TValue] = TTLCache(maxsize=maxsize, ttl=ttl_seconds)

    def get_or_set(self, key: str, factory: Callable[[], TValue]) -> TValue:
        """Return cached value or create and store a new one. 返回缓存值，未命中时创建并写入。"""

        if key in self._cache:
            return self._cache[key]

        value = factory()
        self._cache[key] = value
        return value

    def clear(self) -> None:
        """Clear all cached values. 清空所有缓存值。"""

        self._cache.clear()
