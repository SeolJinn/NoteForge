import "katex/dist/katex.min.css";
import { EditorView, keymap } from "@codemirror/view";
import { EditorState, Compartment, Prec } from "@codemirror/state";
import { markdown, codeLanguages } from "@codemirror/lang-markdown";
import { Strikethrough, TaskList, Table } from "@lezer/markdown";
import { blockWidgetField, blockAtomicRanges, trailingNewlineGuard } from "./block-widgets.js";
import { defaultKeymap, history, historyKeymap } from "@codemirror/commands";
import { searchKeymap, highlightSelectionMatches } from "@codemirror/search";
import { syntaxHighlighting, HighlightStyle, syntaxTree } from "@codemirror/language";
import { tags } from "@lezer/highlight";
import { javascript } from "@codemirror/lang-javascript";
import { html } from "@codemirror/lang-html";
import { css } from "@codemirror/lang-css";
import { json } from "@codemirror/lang-json";
import { python } from "@codemirror/lang-python";
import { livePreviewPlugin } from "./live-preview.js";
import { WikiLink } from "./wiki-link.js";
import { InlineMath, BlockMath } from "./math-extension.js";
import { FootnoteReference, FootnoteDefinition } from "./footnote-extension.js";
import { noteforgeTheme } from "./theme.js";
import {
  setupInterop,
  setEditorView,
  notifyContentChanged,
  notifyLinkClicked,
  notifySaveRequested,
  notifyReady,
} from "./interop.js";

try {
  const readOnlyCompartment = new Compartment();

  const saveKeymap = keymap.of([
    {
      key: "Mod-s",
      run: () => {
        notifySaveRequested();
        return true;
      },
    },
  ]);

  const boldItalicKeymap = keymap.of([
    {
      key: "Mod-b",
      run: (view) => {
        const { from, to } = view.state.selection.main;
        const selected = view.state.sliceDoc(from, to);
        view.dispatch({ changes: { from, to, insert: `**${selected}**` } });
        return true;
      },
    },
    {
      key: "Mod-i",
      run: (view) => {
        const { from, to } = view.state.selection.main;
        const selected = view.state.sliceDoc(from, to);
        view.dispatch({ changes: { from, to, insert: `*${selected}*` } });
        return true;
      },
    },
  ]);

  function isInsideTable(state) {
    const tree = syntaxTree(state);
    const pos = state.selection.main.head;
    let inside = false;
    tree.iterate({
      enter(node) {
        if (inside) return false;
        if (node.name === "Table" && node.from <= pos && node.to >= pos) {
          inside = true;
          return false;
        }
      },
    });
    return inside;
  }

  const tableKeymap = Prec.high(
    keymap.of([
      {
        key: "Tab",
        run: (view) => {
          if (!isInsideTable(view.state)) return false;
          const pos = view.state.selection.main.head;
          const line = view.state.doc.lineAt(pos);
          const text = line.text;
          const col = pos - line.from;
          const nextPipe = text.indexOf("|", col + 1);
          if (nextPipe !== -1) {
            let target = line.from + nextPipe + 1;
            while (target < line.to && view.state.sliceDoc(target, target + 1) === " ") target++;
            view.dispatch({ selection: { anchor: target } });
            return true;
          }
          const nextLineNum = line.number + 1;
          if (nextLineNum <= view.state.doc.lines) {
            const nextLine = view.state.doc.line(nextLineNum);
            if (/^\s*\|?\s*[-:]+/.test(nextLine.text)) {
              const skipNum = nextLineNum + 1;
              if (skipNum <= view.state.doc.lines) {
                const skipLine = view.state.doc.line(skipNum);
                const firstPipe = skipLine.text.indexOf("|");
                if (firstPipe !== -1) {
                  let target = skipLine.from + firstPipe + 1;
                  while (target < skipLine.to && view.state.sliceDoc(target, target + 1) === " ") target++;
                  view.dispatch({ selection: { anchor: target } });
                  return true;
                }
              }
            } else {
              const firstPipe = nextLine.text.indexOf("|");
              if (firstPipe !== -1) {
                let target = nextLine.from + firstPipe + 1;
                while (target < nextLine.to && view.state.sliceDoc(target, target + 1) === " ") target++;
                view.dispatch({ selection: { anchor: target } });
                return true;
              }
            }
          }
          return false;
        },
      },
      {
        key: "Enter",
        run: (view) => {
          if (!isInsideTable(view.state)) return false;
          const pos = view.state.selection.main.head;
          const line = view.state.doc.lineAt(pos);
          const pipes = (line.text.match(/\|/g) || []).length;
          const colCount = Math.max(pipes - 1, 1);
          const newRow = "\n|" + " |".repeat(colCount);
          view.dispatch({
            changes: { from: line.to, insert: newRow },
            selection: { anchor: line.to + 3 },
          });
          return true;
        },
      },
    ])
  );

  const contentChangeListener = EditorView.updateListener.of((update) => {
    if (update.docChanged) {
      notifyContentChanged();
    }
  });

  const clickHandler = EditorView.domEventHandlers({
    mousedown: (event, view) => {
      const target = event.target;

      if (target.classList.contains("cm-link") || target.classList.contains("cm-wikilink")) {
        const href = target.dataset.href;
        if (href) {
          event.preventDefault();
          notifyLinkClicked(href);
          return true;
        }
      }

      return false;
    },
  });

  const codeHighlight = HighlightStyle.define([
    { tag: tags.keyword, color: "#c678dd" },
    { tag: tags.operator, color: "#56b6c2" },
    { tag: tags.variableName, color: "#e06c75" },
    { tag: tags.function(tags.variableName), color: "#61afef" },
    { tag: tags.function(tags.propertyName), color: "#61afef" },
    { tag: tags.definition(tags.variableName), color: "#e5c07b" },
    { tag: tags.typeName, color: "#e5c07b" },
    { tag: tags.className, color: "#e5c07b" },
    { tag: tags.number, color: "#d19a66" },
    { tag: tags.string, color: "#98c379" },
    { tag: tags.bool, color: "#d19a66" },
    { tag: tags.null, color: "#d19a66" },
    { tag: tags.comment, color: "#5c6370", fontStyle: "italic" },
    { tag: tags.propertyName, color: "#e06c75" },
    { tag: tags.punctuation, color: "#abb2bf" },
    { tag: tags.bracket, color: "#abb2bf" },
    { tag: tags.tagName, color: "#e06c75" },
    { tag: tags.attributeName, color: "#d19a66" },
    { tag: tags.attributeValue, color: "#98c379" },
    { tag: tags.meta, color: "#abb2bf" },
  ]);

  const langMap = {
    javascript, js: javascript, jsx: javascript,
    typescript: () => javascript({ typescript: true }),
    ts: () => javascript({ typescript: true }),
    tsx: () => javascript({ jsx: true, typescript: true }),
    html, css, json, python, py: python,
  };

  function resolveLanguage(name) {
    const key = name.toLowerCase().trim();
    const lang = langMap[key];
    if (lang) {
      const result = typeof lang === "function" ? lang() : lang();
      return result.language ?? result;
    }
    return null;
  }

  let mdExtensions = [Strikethrough, TaskList];
  try {
    mdExtensions.push(WikiLink);
  } catch (e) {
    console.warn("WikiLink extension failed to load:", e);
  }
  try {
    mdExtensions.push(Table);
  } catch (e) {
    console.warn("Table extension failed to load:", e);
  }
  try {
    mdExtensions.push(InlineMath, BlockMath);
  } catch (e) {
    console.warn("Math extensions failed to load:", e);
  }
  try {
    mdExtensions.push(FootnoteReference, FootnoteDefinition);
  } catch (e) {
    console.warn("Footnote extensions failed to load:", e);
  }

  const view = new EditorView({
    state: EditorState.create({
      doc: "",
      extensions: [
        history(),
        tableKeymap,
        keymap.of([...defaultKeymap, ...historyKeymap, ...searchKeymap]),
        saveKeymap,
        boldItalicKeymap,
        markdown({ extensions: mdExtensions, codeLanguages: resolveLanguage }),
        syntaxHighlighting(codeHighlight),
        noteforgeTheme,
        highlightSelectionMatches(),
        contentChangeListener,
        clickHandler,
        blockWidgetField,
        blockAtomicRanges,
        trailingNewlineGuard,
        livePreviewPlugin,
        readOnlyCompartment.of(EditorState.readOnly.of(false)),
        EditorView.lineWrapping,
      ],
    }),
    parent: document.getElementById("editor"),
  });

  setEditorView(view);

  window.addEventListener("noteforge-readonly", (e) => {
    view.dispatch({
      effects: readOnlyCompartment.reconfigure(
        EditorState.readOnly.of(e.detail)
      ),
    });
  });

  setupInterop();
  notifyReady();
} catch (err) {
  document.body.innerHTML = `<pre style="color:red;padding:20px">Editor init error: ${err.message}\n${err.stack}</pre>`;
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.postMessage({ type: "error", message: err.message, source: "editor-init" });
    window.chrome.webview.postMessage({ type: "ready" });
  }
}
