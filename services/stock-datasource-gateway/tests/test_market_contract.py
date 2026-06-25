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


def test_a_share_snapshot_uses_single_stock_daily_when_realtime_lists_fail(monkeypatch):
    pd = __import__("pandas")

    class FakeAk:
        @staticmethod
        def stock_zh_a_spot_em():
            raise RuntimeError("spot list unavailable")

        @staticmethod
        def stock_zh_a_spot():
            raise RuntimeError("sina list unavailable")

        @staticmethod
        def stock_zh_a_daily(symbol, start_date, end_date, adjust):
            assert symbol == "sh600519"
            return pd.DataFrame(
                [
                    {
                        "date": "2026-06-22",
                        "close": 1241.41,
                        "outstanding_share": 1_250_082_000,
                    }
                ]
            )

        @staticmethod
        def stock_financial_analysis_indicator(symbol, start_year):
            assert symbol == "600519"
            return pd.DataFrame(
                [
                    {
                        "日期": "2026-03-31",
                        "主营业务收入增长率(%)": 6.538,
                        "销售净利率(%)": 52.2245,
                        "摊薄每股收益(元)": 22.4822,
                    }
                ]
            )

        @staticmethod
        def stock_profile_cninfo(symbol):
            return pd.DataFrame([{"公司名称": "贵州茅台酒股份有限公司", "A股简称": "贵州茅台"}])

    snapshot = akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("600519"))

    assert snapshot.company_name == "贵州茅台"
    assert snapshot.last_price == 1241.41
    assert snapshot.market_cap == 1241.41 * 1_250_082_000
    assert snapshot.pe_ratio > 0
    assert snapshot.revenue_growth_percent == 6.538
    assert snapshot.net_margin_percent == 52.2245


def test_a_share_snapshot_prefers_realtime_spot_over_daily_close(monkeypatch):
    pd = __import__("pandas")

    class FakeAk:
        @staticmethod
        def stock_zh_a_spot_em():
            return pd.DataFrame(
                [
                    {
                        "代码": "301308",
                        "名称": "江波龙",
                        "最新价": 700.25,
                        "总市值": 190_000_000_000,
                        "市盈率-动态": 68.2,
                    }
                ]
            )

        @staticmethod
        def stock_zh_a_spot():
            raise AssertionError("daily fallback should not be reached when realtime spot is available")

        @staticmethod
        def stock_zh_a_daily(symbol, start_date, end_date, adjust):
            raise AssertionError("daily fallback should not be reached when realtime spot is available")

        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            return pd.DataFrame([{"EPSJB": 5.0, "TOTALOPERATEREVETZ": 132.7, "XSJLL": 40.1}])

        @staticmethod
        def stock_profile_cninfo(symbol):
            return pd.DataFrame([{"A股简称": "江波龙"}])

    snapshot = akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("301308"))

    assert snapshot.last_price == 700.25
    assert snapshot.quote_source == "akshare-a-spot-em"
    assert snapshot.price_freshness == "intraday-delayed"


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


def test_a_share_snapshot_maps_eastmoney_financial_fields():
    pd = __import__("pandas")

    class FakeAk:
        @staticmethod
        def stock_zh_a_spot_em():
            raise RuntimeError("spot list unavailable")

        @staticmethod
        def stock_zh_a_spot():
            raise RuntimeError("sina list unavailable")

        @staticmethod
        def stock_zh_a_daily(symbol, start_date, end_date, adjust):
            return pd.DataFrame([{"close": 100.0, "outstanding_share": 10_000.0}])

        @staticmethod
        def stock_financial_analysis_indicator_em(symbol, indicator):
            return pd.DataFrame([{"EPSJB": 5.0, "TOTALOPERATEREVETZ": 6.3, "XSJLL": 52.2}])

        @staticmethod
        def stock_profile_cninfo(symbol):
            return pd.DataFrame([{"A股简称": "测试公司"}])

    snapshot = akshare_market._load_a_share_snapshot(FakeAk, normalize_ticker("600519"))

    assert snapshot.pe_ratio == 20.0
    assert snapshot.revenue_growth_percent == 6.3
    assert snapshot.net_margin_percent == 52.2
