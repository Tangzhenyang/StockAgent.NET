def test_market_requires_bearer_token(client):
    response = client.get("/api/market/snapshot?ticker=00700.HK")

    assert response.status_code == 401


def test_market_rejects_wrong_bearer_token(client):
    response = client.get(
        "/api/market/snapshot?ticker=00700.HK",
        headers={"Authorization": "Bearer wrong-secret"},
    )

    assert response.status_code == 401
