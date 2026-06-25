import pytest
from fastapi import HTTPException

from app.utils.ticker import normalize_ticker


def test_normalizes_short_hong_kong_ticker():
    normalized = normalize_ticker("700")

    assert normalized.ticker == "00700.HK"
    assert normalized.market == "HongKong"


def test_keeps_normalized_hong_kong_ticker():
    normalized = normalize_ticker("00700.HK")

    assert normalized.ticker == "00700.HK"
    assert normalized.market == "HongKong"


def test_resolves_a_share_ticker():
    normalized = normalize_ticker("600519")

    assert normalized.ticker == "600519"
    assert normalized.market == "AShare"


def test_resolves_suffixed_a_share_ticker():
    normalized = normalize_ticker("300476.SZ")

    assert normalized.ticker == "300476"
    assert normalized.market == "AShare"


def test_rejects_unsupported_ticker():
    with pytest.raises(HTTPException):
        normalize_ticker("not-a-code")
