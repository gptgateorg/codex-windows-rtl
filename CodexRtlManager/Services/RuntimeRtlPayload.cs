namespace CodexRtlManager.Services;

/// <summary>
/// JavaScript injected into ChatGPT's Chromium renderer through the local DevTools protocol.
/// It changes only rendered prose and never edits the installed application files.
/// </summary>
public static class RuntimeRtlPayload
{
    public static string Script => $"({BrowserScript})(); true";

    private const string BrowserScript = """
function () {
  if (window.__gptGateRtlLoaded) return;
  window.__gptGateRtlLoaded = true;

  const style = document.createElement('style');
  style.id = 'gptgate-rtl-style';
  style.textContent = `
    :root { --gptgate-rtl-font: Vazirmatn, Tahoma, Arial, sans-serif; }
    [data-gptgate-rtl="true"] {
      direction: rtl !important;
      text-align: right !important;
      unicode-bidi: plaintext !important;
      font-family: var(--gptgate-rtl-font) !important;
      line-height: 1.8 !important;
    }
    pre, code, kbd, samp, [data-gptgate-keep-ltr="true"] {
      direction: ltr !important;
      text-align: left !important;
      unicode-bidi: isolate !important;
    }
  `;
  document.head.appendChild(style);

  const rtl = /[\u0590-\u08FF\uFB1D-\uFDFF\uFE70-\uFEFF]/;
  const excluded = 'pre, code, kbd, samp, input, textarea, [contenteditable="true"]';
  const selectors = 'p, li, blockquote, [data-message-author-role]';

  const update = root => {
    const nodes = [];
    if (root instanceof Element && root.matches(selectors)) nodes.push(root);
    if (root.querySelectorAll) nodes.push(...root.querySelectorAll(selectors));
    for (const node of nodes) {
      if (node.matches(excluded) || node.closest('pre, code')) continue;
      const text = node.textContent || '';
      if (rtl.test(text)) node.dataset.gptgateRtl = 'true';
      else delete node.dataset.gptgateRtl;
    }
  };

  update(document);
  new MutationObserver(records => {
    for (const record of records) {
      for (const node of record.addedNodes) {
        if (node.nodeType === Node.ELEMENT_NODE) update(node);
      }
    }
  }).observe(document.documentElement, { childList: true, subtree: true });
}
""";
}
