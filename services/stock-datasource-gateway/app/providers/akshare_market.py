from datetime import UTC, datetime
from math import isfinite
from typing import Any

import httpx

from app.models.contracts import MarketSnapshotResponse
from app.providers.errors import DataSourceProviderError
from app.utils.ticker import NormalizedTicker


def load_market_snapshot(normalized: NormalizedTicker) -> MarketSnapshotResponse:
    """Load a real market and financial snapshot from the configured real providers. 从真实数据提供器加载行情和财务快照。"""

    if normalized.market == "AShare":
        return _load_a_share_snapshot(None, normalized)

    try:
        import akshare as ak  # type: ignore[import-not-found]
    except Exception as exc:
        raise DataSourceProviderError(
            "AKShare is not installed or cannot be imported.",
            provider="akshare-market",
            retryable=False,
        ) from exc

    if normalized.market == "HongKong":
        return _load_hong_kong_snapshot(ak, normalized)

    return _load_a_share_snapshot(ak, normalized)


def _load_hong_kong_snapshot(ak: Any, normalized: NormalizedTicker) -> MarketSnapshotResponse:
    """Load a Hong Kong stock snapshot from AKShare spot and financial indicators. 加载港股行情和财务指标。"""

    quote_row = _first_successful_row(
        [
            lambda: _with_quote_meta(
                _find_first_row(ak.stock_hk_spot_em(), ["代码", "code"], normalized.ticker.removesuffix(".HK")),
                "akshare-hk-spot-em",
                "intraday-delayed",
            ),
            lambda: _with_quote_meta(
                _find_first_row(ak.stock_hk_spot(), ["代码", "code", "symbol"], normalized.ticker.removesuffix(".HK")),
                "akshare-hk-spot",
                "intraday-delayed",
            ),
        ],
        provider="akshare-hk-quote",
    )
    financial_row = _optional_row(lambda: ak.stock_hk_financial_indicator_em(symbol=normalized.ticker.removesuffix(".HK")))

    return MarketSnapshotResponse(
        ticker=normalized.ticker,
        market="HongKong",
        companyName=str(_pick(quote_row, "名称", "name", "中文名称", default="港股公司")),
        lastPrice=_to_float(_pick(quote_row, "最新价", "last", "最新", "收盘", default=None), required_name="lastPrice"),
        marketCap=_first_number(
            quote_row,
            financial_row,
            ["总市值", "market_cap", "总市值(港元)", "港股市值(港元)"],
            required_name="marketCap",
        ),
        peRatio=_first_number(
            quote_row,
            financial_row,
            ["市盈率", "pe", "市盈率TTM", "市盈率"],
            required_name="peRatio",
        ),
        revenueGrowthPercent=_first_optional_number(
            financial_row,
            ["营业总收入滚动环比增长(%)", "营业总收入同比增长(%)", "收入增长率"],
        ),
        netMarginPercent=_first_optional_number(financial_row, ["销售净利率(%)", "净利率", "NET_PROFIT_RATIO"]),
        quoteSource=str(quote_row.get("_quote_source", "akshare-hk-quote")),
        retrievedAt=datetime.now(UTC),
        priceFreshness=str(quote_row.get("_price_freshness", "intraday-delayed")),
    )


def _load_a_share_snapshot(ak: Any, normalized: NormalizedTicker) -> MarketSnapshotResponse:
    """Load an A-share snapshot from a fast single-stock quote endpoint. 从快速单股接口加载 A 股快照。"""

    quote_row = _first_successful_row(
        [
            lambda: _load_a_share_eastmoney_quote_row(normalized),
            lambda: _load_a_share_sina_quote_row(normalized),
        ],
        provider="akshare-a-quote",
    )
    financial_row = None
    profile_row = None
    last_price = _to_float(_pick(quote_row, "最新价", "last", "最新", "收盘", "close", default=None), required_name="lastPrice")

    return MarketSnapshotResponse(
        ticker=normalized.ticker,
        market="AShare",
        companyName=str(_pick(quote_row, "名称", "name", default=_pick(profile_row or {}, "A股简称", "公司名称", default="A股公司"))),
        lastPrice=last_price,
        marketCap=_a_share_market_cap(last_price, quote_row, financial_row, profile_row),
        peRatio=_a_share_pe_ratio(last_price, quote_row, financial_row),
        revenueGrowthPercent=_first_optional_number(
            financial_row,
            [
                "TOTALOPERATEREVETZ",
                "DJD_TOI_YOY",
                "营业总收入同比增长率",
                "主营业务收入增长率",
                "主营业务收入增长率(%)",
                "营业收入同比增长率",
                "营业总收入同比增长(%)",
            ],
        ),
        netMarginPercent=_first_optional_number(financial_row, ["XSJLL", "销售净利率", "净利率", "销售净利率(%)"]),
        quoteSource=str(quote_row.get("_quote_source", "akshare-a-quote")),
        retrievedAt=datetime.now(UTC),
        priceFreshness=str(quote_row.get("_price_freshness", "intraday-delayed")),
    )


def _load_a_share_financial_row(ak: Any, normalized: NormalizedTicker) -> dict[str, Any] | None:
    """Try multiple A-share financial indicator endpoints. 尝试多个 A 股财务指标接口。"""

    em_symbol = f"{normalized.ticker}.SH" if normalized.ticker.startswith(("6", "9")) else f"{normalized.ticker}.SZ"
    rows = [
        _optional_row_with_number(
            lambda: ak.stock_financial_analysis_indicator_em(symbol=em_symbol, indicator="按报告期"),
            ["EPSJB", "EPSXS"],
        ),
        _optional_last_row_with_number(
            lambda: ak.stock_financial_analysis_indicator(
                symbol=normalized.ticker,
                start_year=str(datetime.now(UTC).year - 3),
            ),
            ["摊薄每股收益(元)", "加权每股收益(元)", "每股收益_调整后(元)", "基本每股收益"],
        ),
    ]
    return next((row for row in rows if row), None)


def _load_a_share_profile_row(ak: Any, normalized: NormalizedTicker) -> dict[str, Any] | None:
    """Load A-share profile fields that can help derive missing market values. 加载可用于补齐市值等字段的 A 股资料。"""

    rows = [
        _optional_key_value_row(lambda: ak.stock_individual_info_em(symbol=normalized.ticker)),
        _optional_row(lambda: ak.stock_profile_cninfo(symbol=normalized.ticker)),
    ]
    return next((row for row in rows if row), None)


def _load_a_share_daily_quote_row(ak: Any, normalized: NormalizedTicker) -> dict[str, Any] | None:
    """Load latest A-share daily quote with outstanding shares from Sina. 从新浪日线加载最新 A 股价格和流通股本。"""

    symbol = _a_share_prefixed_symbol(normalized.ticker)
    end_date = datetime.now(UTC).strftime("%Y%m%d")
    start_date = (datetime.now(UTC).replace(month=1, day=1)).strftime("%Y%m%d")
    frame = ak.stock_zh_a_daily(symbol=symbol, start_date=start_date, end_date=end_date, adjust="")
    if getattr(frame, "empty", True):
        return None

    row = frame.iloc[-1].to_dict()
    close = _to_float(row.get("close"), required_name="close")
    outstanding_share = _to_float(row.get("outstanding_share"), required_name="outstanding_share", required=False)
    if outstanding_share:
        row["总市值"] = close * outstanding_share
    row["最新价"] = close
    row["_quote_source"] = "akshare-a-daily"
    row["_price_freshness"] = "daily-close-fallback"
    return row


def _load_a_share_tx_quote_row(ak: Any, normalized: NormalizedTicker) -> dict[str, Any] | None:
    """Load latest A-share daily quote from Tencent when Sina daily is unavailable. 新浪日线不可用时从腾讯日线加载最新 A 股价格。"""

    symbol = _a_share_prefixed_symbol(normalized.ticker)
    end_date = datetime.now(UTC).strftime("%Y%m%d")
    start_date = (datetime.now(UTC).replace(month=1, day=1)).strftime("%Y%m%d")
    frame = ak.stock_zh_a_hist_tx(symbol=symbol, start_date=start_date, end_date=end_date, adjust="")
    if getattr(frame, "empty", True):
        return None

    row = frame.iloc[-1].to_dict()
    row["最新价"] = _to_float(row.get("close"), required_name="close")
    row["_quote_source"] = "akshare-a-tx-daily"
    row["_price_freshness"] = "daily-close-fallback"
    return row


def _with_quote_meta(row: dict[str, Any] | None, quote_source: str, price_freshness: str) -> dict[str, Any] | None:
    """Attach quote source metadata to a provider row. 为行情行附加来源元数据。"""

    if row is None:
        return None

    row["_quote_source"] = quote_source
    row["_price_freshness"] = price_freshness
    return row


def _a_share_prefixed_symbol(ticker: str) -> str:
    """Return Sina/Tencent A-share symbol with exchange prefix. 返回带交易所前缀的 A 股代码。"""

    prefix = "sh" if ticker.startswith(("6", "9")) else "sz"
    return f"{prefix}{ticker}"


def _load_a_share_eastmoney_quote_row(normalized: NormalizedTicker) -> dict[str, Any] | None:
    """Load A-share intraday delayed quote from Eastmoney single-stock API. 从东方财富单股接口加载 A 股盘中延迟行情。"""

    secid = _eastmoney_secid(normalized.ticker)
    response = httpx.get(
        "https://push2.eastmoney.com/api/qt/stock/get",
        params={
            "secid": secid,
            "fields": "f43,f57,f58,f116,f162",
        },
        timeout=8,
    )
    response.raise_for_status()
    payload = response.json()
    data = payload.get("data") if isinstance(payload, dict) else None
    if not isinstance(data, dict):
        return None

    latest_price = _eastmoney_scaled_number(data.get("f43"))
    if latest_price == 0:
        return None
    market_cap = _eastmoney_plain_number(data.get("f116"))
    pe_ratio = _eastmoney_scaled_number(data.get("f162"))

    return {
        "代码": str(data.get("f57") or normalized.ticker),
        "名称": str(data.get("f58") or "A股公司"),
        "最新价": latest_price,
        "总市值": market_cap,
        "市盈率-动态": pe_ratio,
        "_quote_source": "eastmoney-push2-stock-get",
        "_price_freshness": "intraday-delayed",
    }


def _load_a_share_sina_quote_row(normalized: NormalizedTicker) -> dict[str, Any] | None:
    """Load A-share intraday delayed quote from Sina single-stock API. 从新浪单股接口加载 A 股盘中延迟行情。"""

    symbol = _a_share_prefixed_symbol(normalized.ticker)
    response = httpx.get(
        "http://hq.sinajs.cn/list=" + symbol,
        headers={
            "Referer": "https://finance.sina.com.cn/",
            "User-Agent": "Mozilla/5.0 StockAgent.NET datasource",
        },
        timeout=5,
    )
    response.raise_for_status()
    payload = _decode_sina_quote_payload(response)
    if not payload:
        return None

    parts = payload.split(",")
    if len(parts) < 4:
        return None

    latest_price = _to_float(parts[3], required_name="sinaLatestPrice", required=False)
    if latest_price == 0:
        return None

    return {
        "代码": normalized.ticker,
        "名称": parts[0] or "A股公司",
        "最新价": latest_price,
        "总市值": 0,
        "市盈率-动态": 0,
        "_quote_source": "sina-hq-single-stock",
        "_price_freshness": "intraday-delayed",
    }


def _decode_sina_quote_payload(response: Any) -> str:
    """Decode Sina quote JavaScript payload and return the comma-separated body. 解码新浪行情脚本并返回逗号分隔正文。"""

    content = getattr(response, "content", b"")
    if isinstance(content, bytes) and content:
        text = content.decode("gb18030", errors="ignore")
    else:
        text = str(getattr(response, "text", ""))

    start = text.find('"')
    end = text.rfind('"')
    if start == -1 or end <= start:
        return ""

    return text[start + 1 : end]


def _eastmoney_secid(ticker: str) -> str:
    """Build Eastmoney secid for A-share tickers. 构建东方财富 A 股 secid。"""

    exchange_id = "1" if ticker.startswith(("6", "9")) else "0"
    return f"{exchange_id}.{ticker}"


def _eastmoney_scaled_number(value: Any) -> float:
    """Convert Eastmoney cent-scaled quote fields into normal decimals. 将东方财富按 100 缩放的行情字段转成普通数值。"""

    number = _to_float(value, required_name="eastmoneyScaledValue", required=False)
    return number / 100 if abs(number) >= 100 else number


def _eastmoney_plain_number(value: Any) -> float:
    """Convert Eastmoney plain numeric fields. 转换东方财富普通数值字段。"""

    return _to_float(value, required_name="eastmoneyPlainValue", required=False)


def _a_share_market_cap(
    last_price: float,
    quote_row: dict[str, Any],
    financial_row: dict[str, Any] | None,
    profile_row: dict[str, Any] | None,
) -> float:
    """Read A-share market capitalization or derive it from share count. 读取 A 股总市值，缺失时用股本推导。"""

    direct = _first_optional_number_from_rows(
        [quote_row, financial_row, profile_row],
        ["总市值", "market_cap", "总市值(元)", "总市值（元）"],
    )
    if direct:
        return direct

    shares = _a_share_total_shares(quote_row, financial_row, profile_row)
    if shares:
        return last_price * shares

    return 0.0


def _a_share_total_shares(*rows: dict[str, Any] | None) -> float:
    """Read total shares from provider rows, normalizing common share units. 读取总股本并归一常见单位。"""

    share_keys = [
        ("总股本", 1),
        ("总股本(股)", 1),
        ("总股本（股）", 1),
        ("总股本(万股)", 10_000),
        ("总股本（万股）", 10_000),
        ("TOTAL_SHARE", 1),
        ("total_share", 1),
        ("总股数", 1),
        ("股本", 1),
    ]
    for row in rows:
        if row is None:
            continue
        for key, multiplier in share_keys:
            if key not in row:
                continue
            value = _to_float(row[key], required_name=key, required=False)
            if value != 0:
                return value * multiplier

    return 0.0


def _a_share_pe_ratio(
    last_price: float,
    quote_row: dict[str, Any],
    financial_row: dict[str, Any] | None,
) -> float:
    """Read PE ratio or derive it from latest EPS. 读取市盈率，缺失时用最新 EPS 推导。"""

    direct = _first_optional_number(quote_row, ["市盈率-动态", "市盈率", "pe"])
    if direct:
        return direct

    eps = _first_optional_number(
        financial_row,
        ["EPSJB", "EPSXS", "摊薄每股收益(元)", "加权每股收益(元)", "每股收益_调整后(元)", "基本每股收益"],
    )
    if eps:
        return last_price / eps

    return 0.0


def _first_successful_row(loaders: list[Any], provider: str) -> dict[str, Any]:
    """Return the first non-empty row from a list of provider calls. 从多个提供器调用中返回首个非空行。"""

    errors: list[str] = []
    for loader in loaders:
        try:
            row = loader()
            if row:
                return row
        except Exception as exc:
            errors.append(str(exc))

    raise DataSourceProviderError(
        f"No real quote row returned by {provider}. {'; '.join(errors)}",
        provider=provider,
        retryable=True,
    )


def _optional_row(loader: Any) -> dict[str, Any] | None:
    """Return first row from an optional provider call. 从可选提供器调用中返回首行。"""

    try:
        frame = loader()
    except Exception:
        return None

    if getattr(frame, "empty", True):
        return None

    return frame.iloc[0].to_dict()


def _optional_last_row(loader: Any) -> dict[str, Any] | None:
    """Return last row from an optional provider call. 从可选提供器调用中返回末行。"""

    try:
        frame = loader()
    except Exception:
        return None

    if getattr(frame, "empty", True):
        return None

    return frame.iloc[-1].to_dict()


def _optional_row_with_number(loader: Any, numeric_keys: list[str]) -> dict[str, Any] | None:
    """Return first row containing any valid numeric key. 返回首个包含有效数值字段的行。"""

    try:
        frame = loader()
    except Exception:
        return None

    if getattr(frame, "empty", True):
        return None

    for row in frame.to_dict("records"):
        if _first_optional_number(row, numeric_keys):
            return row

    return None


def _optional_last_row_with_number(loader: Any, numeric_keys: list[str]) -> dict[str, Any] | None:
    """Return latest row containing any valid numeric key. 返回最后一个包含有效数值字段的行。"""

    try:
        frame = loader()
    except Exception:
        return None

    if getattr(frame, "empty", True):
        return None

    rows = frame.to_dict("records")
    for row in reversed(rows):
        if _first_optional_number(row, numeric_keys):
            return row

    return None


def _optional_key_value_row(loader: Any) -> dict[str, Any] | None:
    """Convert a provider key/value frame into a dictionary. 将提供器的键值表转换为字典。"""

    try:
        frame = loader()
    except Exception:
        return None

    if getattr(frame, "empty", True):
        return None

    key_column = next((column for column in ["item", "项目", "指标", "key"] if column in frame.columns), None)
    value_column = next((column for column in ["value", "值", "数值"] if column in frame.columns), None)
    if key_column is None or value_column is None:
        return None

    row: dict[str, Any] = {}
    for record in frame.to_dict("records"):
        key = str(record.get(key_column, "")).strip()
        if key:
            row[key] = record.get(value_column)

    return row or None


def _find_first_row(frame: Any, candidate_columns: list[str], expected_code: str) -> dict[str, Any] | None:
    """Find the first matching row by code from a pandas-like frame. 从类似 pandas 的表中按代码查找首行。"""

    if getattr(frame, "empty", True):
        return None

    for column in candidate_columns:
        if column in frame.columns:
            expected = _normalize_code_for_match(expected_code)
            values = frame[column].astype(str).map(_normalize_code_for_match)
            matches = frame[values == expected]
            if not matches.empty:
                return matches.iloc[0].to_dict()

    return None


def _normalize_code_for_match(value: Any) -> str:
    """Normalize provider code values like SZ301308 or 600519.SH before matching. 规范化 SZ301308 或 600519.SH 等代码后再匹配。"""

    text = str(value).strip().upper()
    digits = "".join(character for character in text if character.isdigit())
    return digits.zfill(6) if len(digits) <= 6 else digits[-6:]


def _first_optional_number_from_rows(rows: list[dict[str, Any] | None], keys: list[str]) -> float:
    """Read the first non-zero number from multiple candidate rows. 从多行候选数据读取首个非零数值。"""

    for row in rows:
        value = _first_optional_number(row, keys)
        if value != 0:
            return value

    return 0.0


def _first_number(
    primary: dict[str, Any] | None,
    secondary: dict[str, Any] | None,
    keys: list[str],
    required_name: str,
) -> float:
    """Read a required numeric value from primary or secondary rows. 从主/次行读取必需数值。"""

    for row in [primary, secondary]:
        value = _first_optional_number(row, keys)
        if value != 0:
            return value

    raise DataSourceProviderError(
        f"Required market field {required_name} was not available from real providers.",
        provider="akshare-market",
        retryable=True,
    )


def _first_optional_number(row: dict[str, Any] | None, keys: list[str]) -> float:
    """Read an optional numeric value, returning 0 when absent. 读取可选数值，不存在时返回 0。"""

    if row is None:
        return 0.0

    for key in keys:
        if key in row:
            value = _to_float(row[key], required_name=key, required=False)
            if value != 0:
                return value

    return 0.0


def _pick(row: dict[str, Any], *keys: str, default: Any) -> Any:
    """Pick the first existing value from a row. 从行数据中选择第一个存在的值。"""

    for key in keys:
        value = row.get(key)
        if value not in (None, "", "-"):
            return value
    return default


def _to_float(value: Any, required_name: str, required: bool = True) -> float:
    """Convert provider values to float and optionally require a real value. 转换提供器数值，并可选要求真实值。"""

    try:
        result = float(str(value).replace(",", ""))
    except (TypeError, ValueError):
        result = 0.0

    if not isfinite(result):
        result = 0.0

    if required and result == 0:
        raise DataSourceProviderError(
            f"Required market field {required_name} was empty or non-numeric.",
            provider="akshare-market",
            retryable=True,
        )

    return result
