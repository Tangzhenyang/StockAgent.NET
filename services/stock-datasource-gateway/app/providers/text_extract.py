import logging

logger = logging.getLogger(__name__)


def trim_text(text: str, max_characters: int) -> str:
    """Trim extracted text to protect API response size. 裁剪提取文本，控制 API 响应大小。"""

    normalized = " ".join(text.split())
    return normalized[:max_characters]


def extract_text_from_url(url: str, max_characters: int) -> str:
    """Placeholder-compatible text extraction hook for future PDF/HTML parsing. 面向未来 PDF/HTML 解析的文本提取入口。"""

    logger.info("Text extraction requested for %s; returning empty text until provider parsing is enabled.", url)
    return trim_text("", max_characters)
