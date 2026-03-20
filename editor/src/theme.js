import { EditorView } from "@codemirror/view";

export const noteforgeTheme = EditorView.theme({
  "&": {
    backgroundColor: "var(--editor-bg)",
    color: "var(--editor-fg)",
    fontSize: "15px",
    fontFamily: "'Segoe UI', system-ui, sans-serif",
  },
  ".cm-content": {
    caretColor: "var(--editor-fg)",
    lineHeight: "1.6",
    padding: "16px 20px",
  },
  ".cm-cursor": {
    borderLeftColor: "var(--editor-fg)",
  },
  "&.cm-focused .cm-selectionBackground, .cm-selectionBackground": {
    backgroundColor: "var(--editor-selection) !important",
  },
  ".cm-gutters": {
    display: "none",
  },
  ".cm-activeLine": {
    backgroundColor: "transparent",
  },
  ".cm-heading-1": { fontSize: "2em", fontWeight: "700", color: "var(--heading-color)" },
  ".cm-heading-2": { fontSize: "1.6em", fontWeight: "700", color: "var(--heading-color)" },
  ".cm-heading-3": { fontSize: "1.35em", fontWeight: "600", color: "var(--heading-color)" },
  ".cm-heading-4": { fontSize: "1.15em", fontWeight: "600", color: "var(--heading-color)" },
  ".cm-heading-5": { fontSize: "1.05em", fontWeight: "600", color: "var(--heading-color)" },
  ".cm-heading-6": { fontSize: "1em", fontWeight: "600", color: "var(--heading-color)" },
  ".cm-strong": { fontWeight: "700" },
  ".cm-emphasis": { fontStyle: "italic" },
  ".cm-strikethrough": { textDecoration: "line-through" },
  ".cm-inline-code": {
    fontFamily: "'Cascadia Code', 'Consolas', monospace",
    backgroundColor: "var(--code-bg)",
    color: "var(--code-fg)",
    padding: "1px 4px",
    borderRadius: "3px",
    fontSize: "0.9em",
  },
  ".cm-code-block-line": {
    fontFamily: "'Cascadia Code', 'Consolas', monospace",
    backgroundColor: "var(--code-bg)",
    fontSize: "0.9em",
    padding: "0 16px",
  },
  ".cm-code-block-line:first-of-type": {
    borderRadius: "6px 6px 0 0",
    paddingTop: "8px",
  },
  ".cm-code-block-line:last-of-type": {
    borderRadius: "0 0 6px 6px",
    paddingBottom: "8px",
  },
  ".cm-code-block-lang": {
    float: "right",
    color: "var(--editor-fg)",
    opacity: "0.5",
    fontSize: "0.8em",
    fontFamily: "'Segoe UI', system-ui, sans-serif",
  },
  ".cm-link": {
    color: "var(--link-color)",
    textDecoration: "underline",
    cursor: "pointer",
  },
  ".cm-wikilink": {
    color: "var(--link-color)",
    textDecoration: "underline",
    cursor: "pointer",
  },
  ".cm-list-bullet": { color: "var(--editor-fg)", opacity: "0.6" },
  ".cm-list-bullet-marker": {
    color: "var(--editor-fg)",
    opacity: "0.6",
    fontSize: "1.2em",
    lineHeight: "1",
    paddingRight: "4px",
  },
  ".cm-blockquote-line": {
    borderLeft: "3px solid var(--link-color)",
    paddingLeft: "12px",
    color: "var(--editor-fg)",
    opacity: "0.85",
    fontStyle: "italic",
  },
  ".cm-hr": {
    border: "none",
    borderTop: "2px solid var(--border-color)",
    margin: "8px 0",
    display: "block",
  },
  ".cm-task-checkbox": {
    display: "inline-block",
    width: "18px",
    height: "18px",
    lineHeight: "18px",
    textAlign: "center",
    borderRadius: "4px",
    marginRight: "6px",
    verticalAlign: "middle",
    fontSize: "12px",
    fontWeight: "bold",
  },
  ".cm-task-unchecked": {
    border: "2px solid var(--border-color)",
    backgroundColor: "transparent",
    color: "transparent",
  },
  ".cm-task-checked": {
    border: "2px solid var(--link-color)",
    backgroundColor: "var(--link-color)",
    color: "#ffffff",
  },
  ".cm-task-completed": {
    textDecoration: "line-through",
    opacity: "0.5",
  },
  ".cm-image-container": {
    display: "block",
    margin: "8px 0",
  },
  ".cm-image": {
    maxWidth: "100%",
    maxHeight: "400px",
    borderRadius: "6px",
    border: "1px solid var(--border-color)",
  },
  ".cm-image-error": {
    color: "var(--code-fg)",
    fontStyle: "italic",
    fontSize: "0.9em",
    opacity: "0.6",
  },
  ".cm-hide-syntax": {
    fontSize: "0",
    width: "0",
    display: "inline-block",
    overflow: "hidden",
  },
  ".cm-search": {
    backgroundColor: "var(--code-bg)",
    color: "var(--editor-fg)",
  },
  ".cm-searchMatch": {
    backgroundColor: "rgba(255, 200, 0, 0.3)",
  },
  ".cm-searchMatch-selected": {
    backgroundColor: "rgba(255, 200, 0, 0.6)",
  },
});
