class DataSourceProviderError(RuntimeError):
    """Raised when a real external data provider cannot return usable data. 真实外部数据源无法返回可用数据时抛出。"""

    def __init__(self, message: str, provider: str, retryable: bool = False) -> None:
        """Create a provider error with provider identity and retry hint. 创建带提供器标识和重试提示的数据源错误。"""

        super().__init__(message)
        self.provider = provider
        self.retryable = retryable
