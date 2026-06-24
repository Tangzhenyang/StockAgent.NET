from fastapi import Depends, FastAPI
from fastapi.responses import JSONResponse

from app.core.security import require_bearer_token
from app.models.contracts import EvidenceDocumentResponse, HealthResponse, MarketSnapshotResponse
from app.providers.errors import DataSourceProviderError
from app.services import evidence_service, market_service

app = FastAPI(title="Stock DataSource Gateway", version="0.1.0")


@app.exception_handler(DataSourceProviderError)
def data_source_provider_error_handler(_request, exc: DataSourceProviderError) -> JSONResponse:
    """Return explicit upstream provider failures to callers. 将上游数据源失败明确返回给调用方。"""

    return JSONResponse(
        status_code=502,
        content={
            "error": str(exc),
            "provider": exc.provider,
            "retryable": exc.retryable,
        },
    )


@app.get("/api/health", response_model=HealthResponse)
def health() -> HealthResponse:
    """Return public service health. 返回公开服务健康状态。"""

    return HealthResponse()


@app.get(
    "/api/market/snapshot",
    response_model=MarketSnapshotResponse,
    dependencies=[Depends(require_bearer_token)],
)
def market_snapshot(ticker: str) -> MarketSnapshotResponse:
    """Return a market and financial snapshot for a ticker. 返回指定股票代码的行情和财务快照。"""

    return market_service.get_market_snapshot(ticker)


@app.get(
    "/api/web/search",
    response_model=list[EvidenceDocumentResponse],
    dependencies=[Depends(require_bearer_token)],
)
def web_search(ticker: str, companyName: str = "") -> list[EvidenceDocumentResponse]:
    """Return evidence documents for a ticker and optional company name. 返回指定股票代码和公司名的证据文档。"""

    return evidence_service.search_evidence_documents(ticker, companyName)
