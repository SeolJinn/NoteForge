import { WidgetType } from "@codemirror/view";
import katex from "katex";
import { getEditorView } from "./interop.js";

export class CheckboxWidget extends WidgetType {
  constructor(checked) {
    super();
    this.checked = checked;
  }
  toDOM() {
    const span = document.createElement("span");
    span.className = this.checked ? "cm-task-checkbox cm-task-checked" : "cm-task-checkbox cm-task-unchecked";
    span.textContent = this.checked ? "✓" : "";
    return span;
  }
  eq(other) {
    return this.checked === other.checked;
  }
}

export class HorizontalRuleWidget extends WidgetType {
  toDOM() {
    const hr = document.createElement("hr");
    hr.className = "cm-hr";
    return hr;
  }
  eq() {
    return true;
  }
}

export class ImageWidget extends WidgetType {
  constructor(src, alt) {
    super();
    this.src = src;
    this.alt = alt;
  }
  toDOM() {
    const container = document.createElement("div");
    container.className = "cm-image-container";
    const img = document.createElement("img");
    img.className = "cm-image";
    img.src = this.src;
    img.alt = this.alt;
    img.onerror = () => {
      container.innerHTML = "";
      const fallback = document.createElement("span");
      fallback.className = "cm-image-error";
      fallback.textContent = `Image not found: ${this.alt || this.src}`;
      container.appendChild(fallback);
    };
    container.appendChild(img);
    return container;
  }
  eq(other) {
    return this.src === other.src && this.alt === other.alt;
  }
}

export class BulletWidget extends WidgetType {
  toDOM() {
    const span = document.createElement("span");
    span.className = "cm-list-bullet-marker";
    span.textContent = "•";
    return span;
  }
  eq() {
    return true;
  }
}

export class CodeBlockLangWidget extends WidgetType {
  constructor(lang) {
    super();
    this.lang = lang;
  }
  toDOM() {
    const span = document.createElement("span");
    span.className = "cm-code-block-lang";
    span.textContent = this.lang;
    return span;
  }
  eq(other) {
    return this.lang === other.lang;
  }
}

export class MathWidget extends WidgetType {
  constructor(tex) {
    super();
    this.tex = tex;
  }
  toDOM() {
    const span = document.createElement("span");
    span.className = "cm-math-inline";
    try {
      span.innerHTML = katex.renderToString(this.tex, {
        throwOnError: false,
        displayMode: false,
      });
    } catch (e) {
      span.textContent = this.tex;
    }
    return span;
  }
  eq(other) {
    return this.tex === other.tex;
  }
}

export class FootnoteRefWidget extends WidgetType {
  constructor(identifier, displayNumber) {
    super();
    this.identifier = identifier;
    this.displayNumber = displayNumber;
  }
  toDOM() {
    const sup = document.createElement("sup");
    sup.className = "cm-footnote-ref";
    sup.textContent = this.displayNumber;
    sup.dataset.footnote = this.identifier;
    const id = this.identifier;
    sup.addEventListener("mousedown", (e) => {
      e.preventDefault();
      e.stopPropagation();
      const view = getEditorView();
      if (!view) return;
      setTimeout(() => {
        const doc = view.state.doc.toString();
        const idx = doc.indexOf(`[^${id}]:`);
        if (idx !== -1) {
          const line = view.state.doc.lineAt(idx);
          const nextLineNum = line.number + 1;
          const anchor = nextLineNum <= view.state.doc.lines
            ? view.state.doc.line(nextLineNum).from
            : line.to;
          view.dispatch({ selection: { anchor }, scrollIntoView: true });
          view.focus();
        }
      }, 0);
    });
    return sup;
  }
  eq(other) {
    return this.identifier === other.identifier && this.displayNumber === other.displayNumber;
  }
  ignoreEvent() { return false; }
}

export class FootnoteDefWidget extends WidgetType {
  constructor(identifier, displayNumber) {
    super();
    this.identifier = identifier;
    this.displayNumber = displayNumber;
  }
  toDOM() {
    const span = document.createElement("span");
    span.className = "cm-footnote-def";
    span.style.cursor = "pointer";
    const sup = document.createElement("sup");
    sup.textContent = this.displayNumber;
    span.appendChild(sup);
    span.appendChild(document.createTextNode(". "));
    const backlink = document.createElement("span");
    backlink.className = "cm-footnote-backlink";
    backlink.textContent = "↩";
    span.appendChild(backlink);
    const id = this.identifier;
    span.addEventListener("mousedown", (e) => {
      e.preventDefault();
      e.stopPropagation();
      const view = getEditorView();
      if (!view) return;
      setTimeout(() => {
        const doc = view.state.doc.toString();
        const pattern = `[^${id}]`;
        let idx = doc.indexOf(pattern);
        while (idx !== -1) {
          if (doc[idx + pattern.length] !== ":") break;
          idx = doc.indexOf(pattern, idx + 1);
        }
        if (idx !== -1) {
          view.dispatch({ selection: { anchor: idx }, scrollIntoView: true });
          view.focus();
        }
      }, 0);
    });
    return span;
  }
  eq(other) {
    return this.identifier === other.identifier && this.displayNumber === other.displayNumber;
  }
  ignoreEvent() { return false; }
}
