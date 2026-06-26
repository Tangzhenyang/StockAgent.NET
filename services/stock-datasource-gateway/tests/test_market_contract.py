import pytest

from app.models.contracts import MarketSnapshotResponse
from app.providers import akshare_market
from app.services import market_service
from app.utils.ticker import normalize_ticker


def setup_function():
    market_service._market_cache.clear()


@pytest.fixture(autouse=True)
def disable_eastmoney_realtime_quote(monkeypatch):
    """Disable real network quote calls unless a test explicitly overrides them. 默认关闭真实网络行情调用，避免测试打外网。"""

    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_quote_row", lambda _normalized: None)
    monkeypatch.setattr(akshare_market, "_load_a_share_tencent_quote_row", lambda _normalized: None, raising=False)
    monkeypatch.setattr(akshare_market, "_load_a_share_sina_quote_row", lambda _normalized: None, raising=False)
    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_financial_row", lambda _normalized: None, raising=False)


def test_market_snapshot_contract(client, auth_headers, monkeypatch):
    def load_real_snapshot(normalized):
        return MarketSnapshotResponse(
            ticker=normalized.ticker,
            market="HongKong",
            companyName="真实腾讯控股",
            lastPrice=445.6,
            marketCap=4_056_728_000_000,
            peRatio=15.43,
            revenueGrowthPercent=2.18,
            netMarginPercent=30.23,
        )

    monkeypatch.setattr(akshare_market, "load_market_snapshot", load_real_snapshot, raising=False)

    response = client.get("/api/market/snapshot?ticker=00700.HK", headers=auth_headers)

    assert response.status_code == 200
    body = response.json()
    assert body["ticker"] == "00700.HK"
    assert body["market"] == "HongKong"
    assert body["companyName"] == "真实腾讯控股"
    assert body["lastPrice"] == 445.6
    assert body["marketCap"] == 4_056_728_000_000
    assert body["peRatio"] == 15.43
    assert body["revenueGrowthPercent"] == 2.18
    assert body["netMarginPercent"] == 30.23


def test_market_snapshot_returns_502_when_provider_has_no_real_data(client, auth_headers, monkeypatch):
    from app.providers.errors import DataSourceProviderError

    def fail_real_snapshot(_normalized):
        raise DataSourceProviderError("no real quote", provider="akshare-market")

    monkeypatch.setattr(akshare_market, "load_market_snapshot", fail_real_snapshot, raising=False)

    response = client.get("/api/market/snapshot?ticker=00700.HK", headers=auth_headers)

    assert response.status_code == 502
    assert response.json()["provider"] == "akshare-market"


def test_a_share_snapshot_fails_fast_when_single_stock_quote_is_unavailable(monkeypatch):
    def fake_missing_eastmoney_quote(normalized):
        return None

    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_quote_row", fake_missing_eastmoney_quote, raising=False)

    class FakeAk:
        @staticmethod
        def stock_zh_a_spot_em():
            raise AssertionError("slow realtime list should not be reached")

        @staticmethod
        def stock_zh_a_spot():
            raise AssertionError("secondary slow realtime list should not be reached")

        @staticmethod
        def stock_zh_a_daily(symbol, start_date, end_date, adjust):
            raise AssertionError("slow daily fallback should not be reached")

        @staticmethod
        def stock_financial_analysis_indicator(symbol, start_year):
            raise AssertionError("slow financial enrichment should not be reached")

        @staticmethod
        def stock_profile_cninfo(symbol):
            raise AssertionError("slow profile enrichment should not be reached")

    with pytest.raises(Exception, match="No real quote row returned"):
        akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("600519"))


def test_a_share_snapshot_does_not_use_slow_spot_lists_when_single_quote_fails(monkeypatch):
    def fake_missing_eastmoney_quote(normalized):
        return None

    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_quote_row", fake_missing_eastmoney_quote, raising=False)

    class FakeAk:
        @staticmethod
        def stock_zh_a_spot_em():
            raise AssertionError("slow realtime list should not be reached")

        @staticmethod
        def stock_zh_a_spot():
            raise AssertionError("secondary slow realtime list should not be reached")

        @staticmethod
        def stock_zh_a_daily(symbol, start_date, end_date, adjust):
            raise AssertionError("slow daily fallback should not be reached")

        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            raise AssertionError("slow financial enrichment should not be reached")

        @staticmethod
        def stock_profile_cninfo(symbol):
            raise AssertionError("slow profile enrichment should not be reached")

    with pytest.raises(Exception, match="No real quote row returned"):
        akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("301308"))


def test_a_share_snapshot_prefers_single_stock_realtime_over_daily_close(monkeypatch):
    pd = __import__("pandas")

    def fake_eastmoney_realtime(normalized):
        return {
            "代码": normalized.ticker,
            "名称": "江波龙",
            "最新价": 659.01,
            "总市值": 278_800_000_000,
            "市盈率-动态": 67.3,
            "_quote_source": "eastmoney-push2-stock-get",
            "_price_freshness": "intraday-delayed",
        }

    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_quote_row", fake_eastmoney_realtime, raising=False)

    class FakeAk:
        @staticmethod
        def stock_zh_a_spot_em():
            raise RuntimeError("spot list unavailable")

        @staticmethod
        def stock_zh_a_spot():
            raise RuntimeError("sina list unavailable")

        @staticmethod
        def stock_zh_a_daily(symbol, start_date, end_date, adjust):
            return pd.DataFrame([{"close": 619.98, "outstanding_share": 282_000_000}])

        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            return pd.DataFrame([{"EPSJB": 5.0, "TOTALOPERATEREVETZ": 132.7, "XSJLL": 40.1}])

        @staticmethod
        def stock_profile_cninfo(symbol):
            return pd.DataFrame([{"A股简称": "江波龙"}])

    snapshot = akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("301308"))

    assert snapshot.last_price == 659.01
    assert snapshot.quote_source == "eastmoney-push2-stock-get"
    assert snapshot.price_freshness == "intraday-delayed"


def test_a_share_snapshot_uses_sina_single_stock_quote_when_eastmoney_times_out(monkeypatch):
    def fake_eastmoney_timeout(normalized):
        raise TimeoutError("_ssl.c:993: The handshake operation timed out")

    def fake_sina_realtime(normalized):
        return {
            "代码": normalized.ticker,
            "名称": "江波龙",
            "最新价": 659.01,
            "总市值": 0,
            "市盈率-动态": 0,
            "_quote_source": "sina-hq-single-stock",
            "_price_freshness": "intraday-delayed",
        }

    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_quote_row", fake_eastmoney_timeout, raising=False)
    monkeypatch.setattr(akshare_market, "_load_a_share_sina_quote_row", fake_sina_realtime, raising=False)

    snapshot = akshare_market._load_a_share_snapshot(None, normalize_ticker("301308"))

    assert snapshot.last_price == 659.01
    assert snapshot.company_name == "江波龙"
    assert snapshot.quote_source == "sina-hq-single-stock"


def test_a_share_snapshot_keeps_sina_price_and_merges_metrics(monkeypatch):
    def fake_sina_realtime(normalized):
        return {
            "代码": normalized.ticker,
            "名称": "江波龙",
            "最新价": 658.18,
            "总市值": 0,
            "市盈率-动态": 0,
            "_quote_source": "sina-hq-single-stock",
            "_price_freshness": "intraday-delayed",
        }

    def fake_tencent_metrics(normalized):
        return {
            "代码": normalized.ticker,
            "名称": "江波龙",
            "最新价": 650.00,
            "总市值": 278_800_000_000,
            "市盈率-动态": 18.05,
            "_quote_source": "tencent-qt-single-stock",
            "_price_freshness": "intraday-delayed",
        }

    monkeypatch.setattr(akshare_market, "_load_a_share_sina_quote_row", fake_sina_realtime, raising=False)
    monkeypatch.setattr(akshare_market, "_load_a_share_tencent_quote_row", fake_tencent_metrics, raising=False)

    snapshot = akshare_market._load_a_share_snapshot(None, normalize_ticker("301308"))

    assert snapshot.last_price == 658.18
    assert snapshot.market_cap == 278_800_000_000
    assert snapshot.pe_ratio == 18.05
    assert snapshot.quote_source == "sina-hq-single-stock+tencent-qt-single-stock"


def test_a_share_snapshot_uses_akshare_financial_metrics_when_fast_financial_fails(monkeypatch):
    pd = __import__("pandas")

    def fake_sina_realtime(normalized):
        return {
            "代码": normalized.ticker,
            "名称": "江波龙",
            "最新价": 658.18,
            "总市值": 0,
            "市盈率-动态": 0,
            "_quote_source": "sina-hq-single-stock",
            "_price_freshness": "intraday-delayed",
        }

    class FakeAk:
        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            return pd.DataFrame([{"EPSJB": 10.0, "TOTALOPERATEREVETZ": 132.7, "XSJLL": 40.1}])

        @staticmethod
        def stock_financial_analysis_indicator(symbol, start_year):
            raise AssertionError("secondary financial endpoint should not be reached")

    monkeypatch.setattr(akshare_market, "_load_a_share_sina_quote_row", fake_sina_realtime, raising=False)
    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_financial_row", lambda _normalized: None, raising=False)

    snapshot = akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("301308"))

    assert snapshot.pe_ratio == 65.818
    assert snapshot.revenue_growth_percent == 132.7
    assert snapshot.net_margin_percent == 40.1


def test_a_share_snapshot_uses_tencent_metrics_when_eastmoney_fails(monkeypatch):
    def fake_failed_eastmoney(normalized):
        raise TimeoutError("eastmoney unavailable")

    def fake_tencent_realtime(normalized):
        return {
            "代码": normalized.ticker,
            "名称": "江波龙",
            "最新价": 658.18,
            "总市值": 278_800_000_000,
            "市盈率-动态": 18.05,
            "_quote_source": "tencent-qt-single-stock",
            "_price_freshness": "intraday-delayed",
        }

    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_quote_row", fake_failed_eastmoney, raising=False)
    monkeypatch.setattr(akshare_market, "_load_a_share_tencent_quote_row", fake_tencent_realtime, raising=False)

    snapshot = akshare_market._load_a_share_snapshot(None, normalize_ticker("301308"))

    assert snapshot.last_price == 658.18
    assert snapshot.market_cap == 278_800_000_000
    assert snapshot.pe_ratio == 18.05
    assert snapshot.quote_source == "tencent-qt-single-stock"


def test_a_share_snapshot_merges_fast_financial_indicators(monkeypatch):
    def fake_eastmoney_quote(normalized):
        return {
            "代码": normalized.ticker,
            "名称": "江波龙",
            "最新价": 658.18,
            "总市值": 278_800_000_000,
            "市盈率-动态": 18.05,
            "_quote_source": "eastmoney-push2-stock-get",
            "_price_freshness": "intraday-delayed",
        }

    def fake_financial_row(normalized):
        return {
            "TOTAL_OPERATE_INCOME_YOY": 132.79,
            "XSJLL": 40.16,
        }

    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_quote_row", fake_eastmoney_quote, raising=False)
    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_financial_row", fake_financial_row, raising=False)

    snapshot = akshare_market._load_a_share_snapshot(None, normalize_ticker("301308"))

    assert snapshot.revenue_growth_percent == 132.79
    assert snapshot.net_margin_percent == 40.16


def test_eastmoney_single_stock_quote_retries_http_when_https_times_out(monkeypatch):
    class FakeResponse:
        @staticmethod
        def raise_for_status():
            return None

        @staticmethod
        def json():
            return {
                "data": {
                    "f43": 67730,
                    "f57": "301308",
                    "f58": "江波龙",
                    "f116": 278_800_000_000,
                    "f162": 67.3,
                }
            }

    requested_urls = []

    def fake_get(url, *args, **kwargs):
        requested_urls.append(url)
        if str(url).startswith("https://"):
            raise TimeoutError("_ssl.c:993: The handshake operation timed out")
        return FakeResponse()

    monkeypatch.setattr(akshare_market.httpx, "get", fake_get)

    row = akshare_market._load_a_share_eastmoney_quote_row(normalize_ticker("301308"))

    assert requested_urls == [
        "https://push2.eastmoney.com/api/qt/stock/get",
        "http://push2.eastmoney.com/api/qt/stock/get",
    ]
    assert row["最新价"] == 677.3
    assert row["总市值"] == 278_800_000_000
    assert row["市盈率-动态"] == 67.3
    assert row["_quote_source"] == "eastmoney-push2-stock-get-http"


def test_tencent_single_stock_quote_parses_market_cap_and_pe(monkeypatch):
    parts = [""] * 50
    parts[1] = "江波龙"
    parts[2] = "301308"
    parts[3] = "658.18"
    parts[39] = "18.05"
    parts[45] = "2788.00"
    payload = "v_sz301308=\"" + "~".join(parts) + "\";"

    class FakeResponse:
        content = payload.encode("gb18030")

        @staticmethod
        def raise_for_status():
            return None

    monkeypatch.setattr(akshare_market.httpx, "get", lambda *args, **kwargs: FakeResponse())

    row = akshare_market._load_a_share_tencent_quote_row(normalize_ticker("301308"))

    assert row["名称"] == "江波龙"
    assert row["最新价"] == 658.18
    assert row["总市值"] == 278_800_000_000
    assert row["市盈率-动态"] == 18.05
    assert row["_quote_source"] == "tencent-qt-single-stock"


def test_eastmoney_fast_financial_row_parses_growth_and_margin(monkeypatch):
    class FakeResponse:
        @staticmethod
        def raise_for_status():
            return None

        @staticmethod
        def json():
            return {
                "data": [
                    {
                        "REPORT_DATE": "2026-03-31",
                        "TOTAL_OPERATE_INCOME_YOY": 132.79,
                        "XSJLL": 40.16,
                    }
                ]
            }

    monkeypatch.setattr(akshare_market.httpx, "get", lambda *args, **kwargs: FakeResponse())

    row = akshare_market._load_a_share_eastmoney_financial_row(normalize_ticker("301308"))

    assert row["TOTAL_OPERATE_INCOME_YOY"] == 132.79
    assert row["XSJLL"] == 40.16


def test_a_share_snapshot_skips_all_slow_fallbacks_after_failed_single_stock_quote(monkeypatch):
    def fake_failed_eastmoney_realtime(normalized):
        return None

    monkeypatch.setattr(
        akshare_market,
        "_load_a_share_eastmoney_quote_row",
        fake_failed_eastmoney_realtime,
        raising=False,
    )

    class FakeAk:
        @staticmethod
        def stock_zh_a_spot_em():
            raise AssertionError("slow realtime list should not be reached")

        @staticmethod
        def stock_zh_a_spot():
            raise AssertionError("secondary slow realtime list should not be reached")

        @staticmethod
        def stock_zh_a_daily(symbol, start_date, end_date, adjust):
            raise AssertionError("slow daily fallback should not be reached")

        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            raise AssertionError("slow financial enrichment should not be reached")

        @staticmethod
        def stock_profile_cninfo(symbol):
            raise AssertionError("slow profile enrichment should not be reached")

    with pytest.raises(Exception, match="No real quote row returned"):
        akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("301308"))


def test_a_share_market_cap_derives_from_total_shares():
    market_cap = akshare_market._a_share_market_cap(
        659.01,
        {"最新价": 659.01},
        None,
        {"总股本": 422_000_000},
    )

    assert market_cap == 659.01 * 422_000_000


def test_a_share_market_cap_returns_zero_when_missing():
    market_cap = akshare_market._a_share_market_cap(
        659.01,
        {"最新价": 659.01},
        None,
        None,
    )

    assert market_cap == 0


def test_eastmoney_single_stock_quote_accepts_price_only_to_avoid_slow_list_fallback(monkeypatch):
    class FakeResponse:
        @staticmethod
        def raise_for_status():
            return None

        @staticmethod
        def json():
            return {
                "data": {
                    "f43": 65901,
                    "f57": "301308",
                    "f58": "江波龙",
                    "f116": "-",
                    "f162": "-",
                }
            }

    monkeypatch.setattr(akshare_market.httpx, "get", lambda *args, **kwargs: FakeResponse())

    row = akshare_market._load_a_share_eastmoney_quote_row(normalize_ticker("301308"))

    assert row["最新价"] == 659.01
    assert row["总市值"] == 0
    assert row["市盈率-动态"] == 0
    assert row["_quote_source"] == "eastmoney-push2-stock-get"


def test_sina_single_stock_quote_parses_intraday_price(monkeypatch):
    class FakeResponse:
        content = (
            b'var hq_str_sz301308="'
            b'\xbd\xad\xb2\xa8\xc1\xfa,655.000,619.980,659.010,688.800,640.000,'
            b'0,0,169000,11720000000,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'
            b'2026-06-25,11:30:00,00";'
        )

        @staticmethod
        def raise_for_status():
            return None

    monkeypatch.setattr(akshare_market.httpx, "get", lambda *args, **kwargs: FakeResponse())

    row = akshare_market._load_a_share_sina_quote_row(normalize_ticker("301308"))

    assert row["名称"] == "江波龙"
    assert row["最新价"] == 659.01
    assert row["_quote_source"] == "sina-hq-single-stock"


def test_a_share_snapshot_allows_missing_pe_when_price_exists(monkeypatch):
    pd = __import__("pandas")

    def fake_price_only_eastmoney(normalized):
        return {
            "代码": normalized.ticker,
            "名称": "江波龙",
            "最新价": 659.01,
            "总市值": 0,
            "市盈率-动态": 0,
            "_quote_source": "eastmoney-push2-stock-get",
            "_price_freshness": "intraday-delayed",
        }

    monkeypatch.setattr(akshare_market, "_load_a_share_eastmoney_quote_row", fake_price_only_eastmoney, raising=False)

    class FakeAk:
        @staticmethod
        def stock_zh_a_spot_em():
            raise AssertionError("slow realtime list should not be reached")

        @staticmethod
        def stock_zh_a_spot():
            raise AssertionError("secondary realtime list should not be reached")

        @staticmethod
        def stock_zh_a_daily(symbol, start_date, end_date, adjust):
            raise AssertionError("daily fallback should not be reached")

        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            return pd.DataFrame([{"TOTALOPERATEREVETZ": 132.7, "XSJLL": 40.1}])

        @staticmethod
        def stock_financial_analysis_indicator(symbol, start_year):
            return pd.DataFrame([])

        @staticmethod
        def stock_individual_info_em(symbol):
            return pd.DataFrame([{"item": "股票简称", "value": "江波龙"}])

        @staticmethod
        def stock_profile_cninfo(symbol):
            return pd.DataFrame([{"A股简称": "江波龙"}])

    snapshot = akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("301308"))

    assert snapshot.last_price == 659.01
    assert snapshot.market_cap == 0
    assert snapshot.pe_ratio == 0
    assert snapshot.quote_source == "eastmoney-push2-stock-get"


def test_find_first_row_matches_prefixed_and_suffixed_a_share_codes():
    pd = __import__("pandas")
    frame = pd.DataFrame(
        [
            {"代码": "SZ301308", "名称": "江波龙", "最新价": 659.01},
            {"代码": "600519.SH", "名称": "贵州茅台", "最新价": 1241.41},
        ]
    )

    sz_row = akshare_market._find_first_row(frame, ["代码"], "301308")
    sh_row = akshare_market._find_first_row(frame, ["代码"], "600519")

    assert sz_row["名称"] == "江波龙"
    assert sh_row["名称"] == "贵州茅台"


def test_a_share_financial_loader_uses_latest_sina_row():
    pd = __import__("pandas")

    class FakeAk:
        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            raise RuntimeError("em unavailable")

        @staticmethod
        def stock_financial_analysis_indicator(symbol, start_year):
            return pd.DataFrame(
                [
                    {"日期": "2025-12-31", "摊薄每股收益(元)": 10.0},
                    {"日期": "2026-03-31", "摊薄每股收益(元)": 22.4822},
                ]
            )

    row = akshare_market._load_a_share_financial_row(FakeAk, normalize_ticker("600519"))

    assert row["日期"] == "2026-03-31"
    assert row["摊薄每股收益(元)"] == 22.4822


def test_a_share_financial_loader_skips_nan_eastmoney_eps():
    pd = __import__("pandas")

    class FakeAk:
        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            return pd.DataFrame(
                [
                    {"REPORT_DATE": "2026-06-30", "EPSJB": float("nan")},
                    {"REPORT_DATE": "2026-03-31", "EPSJB": 5.0, "TOTALOPERATEREVETZ": 6.3},
                ]
            )

        @staticmethod
        def stock_financial_analysis_indicator(symbol, start_year):
            raise RuntimeError("sina unavailable")

    row = akshare_market._load_a_share_financial_row(FakeAk, normalize_ticker("600519"))

    assert row["REPORT_DATE"] == "2026-03-31"
    assert row["EPSJB"] == 5.0


def test_a_share_pe_ratio_returns_zero_when_missing():
    pe_ratio = akshare_market._a_share_pe_ratio(100.0, {"最新价": 100.0}, None)

    assert pe_ratio == 0
