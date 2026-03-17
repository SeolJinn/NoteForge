let editorView = null;
let contentChangeTimeout = null;

export function setEditorView(view) {
  editorView = view;
}

export function getEditorView() {
  return editorView;
}

export function setupInterop() {
  window.chrome.webview.addEventListener("message", (event) => {
    const msg = event.data;
    try {
      switch (msg.type) {
        case "setContent":
          handleSetContent(msg);
          break;
        case "getContent":
          postMessage({
            type: "contentResponse",
            id: msg.id,
            text: editorView ? editorView.state.doc.toString() : "",
          });
          break;
        case "setTheme":
          applyThemeVars(msg.vars);
          break;
        case "focus":
          if (editorView) editorView.focus();
          break;
        case "setReadOnly":
          window.dispatchEvent(
            new CustomEvent("noteforge-readonly", { detail: msg.value })
          );
          break;
        case "navigateToLine":
          if (editorView && msg.line >= 1) {
            const lineCount = editorView.state.doc.lines;
            const targetLine = Math.min(msg.line, lineCount);
            const line = editorView.state.doc.line(targetLine);
            editorView.dispatch({
              selection: { anchor: line.from, head: line.to },
              scrollIntoView: true,
            });
            editorView.focus();
          }
          break;
      }
    } catch (err) {
      postMessage({ type: "error", message: err.message, source: msg.type });
    }
  });
}

function handleSetContent(msg) {
  if (!editorView) return;
  editorView.dispatch({
    changes: { from: 0, to: editorView.state.doc.length, insert: msg.text || "" },
  });
}

function applyThemeVars(vars) {
  if (!vars) return;
  const root = document.documentElement;
  for (const [key, value] of Object.entries(vars)) {
    root.style.setProperty(key, value);
  }
}

export function postMessage(msg) {
  window.chrome.webview.postMessage(JSON.stringify(msg));
}

export function notifyContentChanged() {
  clearTimeout(contentChangeTimeout);
  contentChangeTimeout = setTimeout(() => {
    postMessage({ type: "contentChanged" });
  }, 200);
}

export function notifyLinkClicked(href) {
  postMessage({ type: "linkClicked", href });
}

export function notifySaveRequested() {
  postMessage({ type: "saveRequested" });
}

export function notifyReady() {
  postMessage({ type: "ready" });
}
