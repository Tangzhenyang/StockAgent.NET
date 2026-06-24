from datetime import UTC, datetime

from app.models.contracts import EvidenceDocumentResponse
from app.providers import cninfo_announcements, hkex_announcements
from app.services import evidence_service


def setup_function():
    evidence_service._evidence_cache.clear()


def test_a_share_evidence_search_uses_real_cninfo_announcement_shape(client, auth_headers, monkeypatch):
    def load_real_cninfo(normalized, company_name, max_documents):
        return [
            EvidenceDocumentResponse(
                url="http://www.cninfo.com.cn/new/disclosure/detail?stockCode=600519&announcementId=1225114741",
                title="贵州茅台2025年年度报告",
                sourceType="annual-report",
                publishedAt=datetime(2026, 4, 17, tzinfo=UTC),
                text="贵州茅台2025年年度报告披露营业收入、销售净利率、现金流和风险事项。",
            )
        ][:max_documents]

    monkeypatch.setattr(cninfo_announcements, "load_a_share_announcements", load_real_cninfo)

    response = client.get(
        "/api/web/search?ticker=600519&companyName=贵州茅台",
        headers=auth_headers,
    )

    assert response.status_code == 200
    body = response.json()
    assert body[0]["url"].startswith("http://www.cninfo.com.cn/")
    assert body[0]["title"] == "贵州茅台2025年年度报告"
    assert body[0]["sourceType"] == "annual-report"
    assert "营业收入" in body[0]["text"]


def test_hong_kong_evidence_search_uses_hkex_and_financial_evidence(client, auth_headers, monkeypatch):
    def load_real_hk(normalized, company_name, max_documents):
        return [
            EvidenceDocumentResponse(
                url="https://www1.hkexnews.hk/search/titlesearch.xhtml?stock_code=00700",
                title="腾讯控股 财务指标证据",
                sourceType="annual-report",
                publishedAt=datetime(2026, 6, 17, tzinfo=UTC),
                text="腾讯控股最近报告期营业总收入196458000000，销售净利率30.23%，市盈率15.43。",
            )
        ][:max_documents]

    monkeypatch.setattr(hkex_announcements, "load_hk_announcements", load_real_hk)

    response = client.get(
        "/api/web/search?ticker=00700.HK&companyName=腾讯控股",
        headers=auth_headers,
    )

    assert response.status_code == 200
    body = response.json()
    assert body[0]["url"].startswith("https://www1.hkexnews.hk/")
    assert body[0]["title"] == "腾讯控股 财务指标证据"
    assert body[0]["sourceType"] == "annual-report"
    assert "销售净利率30.23%" in body[0]["text"]
