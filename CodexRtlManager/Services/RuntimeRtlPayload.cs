using System.Text.RegularExpressions;

namespace CodexRtlManager.Services;

/// <summary>
/// JavaScript injected into ChatGPT's Chromium renderer through the local DevTools protocol.
/// It changes only rendered prose and never edits the installed application files.
/// </summary>
public static class RuntimeRtlPayload
{
    private const string RtlLetterPattern =
        @"[\u0620-\u063F\u0641-\u064A\u066E-\u066F\u0671-\u06D3\u06D5\u06EE-\u06EF\u06FA-\u06FC\u06FF\u0750-\u077F\u08A0-\u08C9\uFB50-\uFBC2\uFBD3-\uFD8F\uFD92-\uFDC7\uFE70-\uFEFC]";

    private static readonly Regex RtlLetterRegex = new(
        RtlLetterPattern,
        RegexOptions.CultureInvariant);

    public static string Script => CreateScript(new FontSettingsService().GetActiveFont());

    public static string CreateScript(RuntimeFont font) =>
        $"({BrowserScript})({Json(font.MimeType)}, {Json(font.Format)}, {Json(font.Base64Data)}); true";

    public static bool ShouldUseRtl(string? text) =>
        !string.IsNullOrEmpty(text) && RtlLetterRegex.IsMatch(text);

    private static string Json(string value) => System.Text.Json.JsonSerializer.Serialize(value);

    private static readonly string BrowserScript = BrowserScriptTemplate.Replace(
        "__RTL_LETTER_PATTERN__",
        RtlLetterPattern,
        StringComparison.Ordinal);

    private const string BrowserScriptTemplate = """
function (fontMime, fontFormat, fontBase64) {
  const version = 4;
  if (window.__gptGateRtlState?.version === version) return;
  window.__gptGateRtlState?.dispose?.();
  document.getElementById('gptgate-rtl-style')?.remove();

  const style = document.createElement('style');
  style.id = 'gptgate-rtl-style';
  style.textContent = `
    @font-face {
      font-family: 'GptGateRuntimeFont';
      src: url(data:${fontMime};base64,${fontBase64}) format('${fontFormat}');
      font-style: normal;
      font-weight: 100 900;
      font-display: swap;
    }
    :root { --gptgate-rtl-font: 'GptGateRuntimeFont', Tahoma, Arial, sans-serif; }
    [data-gptgate-rtl="true"] {
      direction: rtl !important;
      text-align: right !important;
      unicode-bidi: isolate !important;
      font-family: var(--gptgate-rtl-font) !important;
      line-height: 1.8 !important;
    }
    pre, code, kbd, samp, input, textarea, [contenteditable="true"], [data-gptgate-keep-ltr="true"] {
      direction: ltr !important;
      text-align: left !important;
      unicode-bidi: isolate !important;
    }
  `;
  document.head.appendChild(style);

  const rtlLetter = /__RTL_LETTER_PATTERN__/u;
  const excluded = 'pre, code, kbd, samp, input, textarea, [contenteditable="true"]';
  const selectors = 'p, li, blockquote, [data-message-author-role]';
  const pendingRoots = new Set();
  let scheduled = false;

  const applyDirection = node => {
    if (!node.isConnected || node.matches(excluded) || node.closest(excluded)) return;
    const shouldUseRtl = rtlLetter.test(node.textContent || '');
    if (node.dataset.gptgateAutoDir === 'true') {
      node.removeAttribute('dir');
      delete node.dataset.gptgateAutoDir;
    }
    if (shouldUseRtl) node.dataset.gptgateRtl = 'true';
    else delete node.dataset.gptgateRtl;
  };

  const scanAddedRoot = root => {
    if (!(root instanceof Element)) return;
    if (root.matches(selectors)) applyDirection(root);
    root.querySelectorAll(selectors).forEach(applyDirection);
  };

  const flush = () => {
    scheduled = false;
    const roots = [...pendingRoots];
    pendingRoots.clear();
    roots.forEach(scanAddedRoot);
  };

  const schedule = () => {
    if (scheduled) return;
    scheduled = true;
    if ('requestIdleCallback' in window) {
      window.requestIdleCallback(flush, { timeout: 200 });
    } else {
      window.setTimeout(flush, 50);
    }
  };

  const enqueueRoot = root => {
    for (const pending of pendingRoots) {
      if (pending.contains(root)) return;
      if (root.contains(pending)) pendingRoots.delete(pending);
    }
    pendingRoots.add(root);
  };

  const enqueueCandidate = node => {
    const element = node instanceof Element ? node : node.parentElement;
    if (!element) return;
    const candidate = element.matches(selectors) ? element : element.closest(selectors);
    if (candidate) enqueueRoot(candidate);
  };

  document.querySelectorAll(selectors).forEach(applyDirection);
  const observer = new MutationObserver(records => {
    for (const record of records) {
      if (record.type === 'characterData') {
        enqueueCandidate(record.target);
        continue;
      }
      enqueueCandidate(record.target);
      for (const node of record.addedNodes) {
        if (node.nodeType === Node.ELEMENT_NODE) enqueueRoot(node);
        else enqueueCandidate(node);
      }
    }
    if (pendingRoots.size) schedule();
  });
  observer.observe(document.documentElement, {
    childList: true,
    subtree: true,
    characterData: true
  });

  window.__gptGateRtlState = {
    version,
    dispose() {
      observer.disconnect();
      pendingRoots.clear();
      style.remove();
      document.querySelectorAll('[data-gptgate-rtl]').forEach(node => {
        delete node.dataset.gptgateRtl;
      });
      document.querySelectorAll('[data-gptgate-auto-dir="true"]').forEach(node => {
        node.removeAttribute('dir');
        delete node.dataset.gptgateAutoDir;
      });
    }
  };
}
""";
}
