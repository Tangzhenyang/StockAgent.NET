from app.providers.errors import DataSourceProviderError
from app.services import evidence_service, market_service


def test_market_provider_error_returns_502(client, auth_headers, monkeypatch):
    def fail_market_snapshot(_ticker: str):
        raise DataSourceProviderError("market source failed", provider="akshare-market", retryable=True)

    monkeypatch.setattr(market_service, "get_market_snapshot", fail_market_snapshot)

    response = client.get("/api/market/snapshot?ticker=00700.HK", headers=auth_headers)

    assert response.status_code == 502
    assert response.json()["provider"] == "akshare-market"
    assert response.json()["retryable"] is True


def test_evidence_provider_error_returns_502(client, auth_headers, monkeypatch):
    def fail_evidence_search(_ticker: str, _company_name: str = ""):
        raise DataSourceProviderError("evidence source failed", provider="public-evidence", retryable=False)

    monkeypatch.setattr(evidence_service, "search_evidence_documents", fail_evidence_search)

    response = client.get(
        "/api/web/search?ticker=00700.HK&companyName=腾讯控股",
        headers=auth_headers,
    )

    assert response.status_code == 502
    assert response.json()["provider"] == "public-evidence"
    assert response.json()["retryable"] is False
