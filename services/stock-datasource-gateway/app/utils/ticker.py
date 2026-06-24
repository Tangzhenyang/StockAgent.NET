from dataclasses import dataclass

from fastapi import HTTPException, status


@dataclass(frozen=True)
class NormalizedTicker:
    """Normalized ticker and resolved market. 规范化股票代码及其市场。"""

    ticker: str
    market: str


def normalize_ticker(raw_ticker: str) -> NormalizedTicker:
    """Normalize A-share and Hong Kong tickers for downstream providers. 规范化 A 股和港股代码。"""

    ticker = raw_ticker.strip().upper()
    if not ticker:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Ticker is required.")

    if ticker.endswith(".HK"):
        numeric = ticker.removesuffix(".HK")
        if numeric.isdigit() and 1 <= len(numeric) <= 5:
            return NormalizedTicker(f"{int(numeric):05d}.HK", "HongKong")

    if ticker.isdigit() and 1 <= len(ticker) <= 5:
        return NormalizedTicker(f"{int(ticker):05d}.HK", "HongKong")

    if ticker.isdigit() and len(ticker) == 6:
        return NormalizedTicker(ticker, "AShare")

    raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Unsupported ticker format.")
