import { StateField, EditorState } from "@codemirror/state";
import { Decoration, EditorView, WidgetType } from "@codemirror/view";
import { syntaxTree } from "@codemirror/language";
import katex from "katex";

function isRangeActive(state, from, to) {
  for (const range of state.selection.ranges) {
    if (range.from <= to && range.to >= from) return true;
  }
  return false;
}

const INLINE_MARK_NAMES = new Set([
  "EmphasisMark",
  "CodeMark",
  "StrikethroughMark",
  "LinkMark",
  "URL",
  "LinkTitle",
]);

function parseInlineSegments(state, node) {
  const segments = [];
  let pos = node.from;
  for (let child = node.firstChild; child; child = child.nextSibling) {
    if (child.from > pos) {
      segments.push({ type: "text", value: state.sliceDoc(pos, child.from) });
    }
    pos = child.to;
    if (INLINE_MARK_NAMES.has(child.name)) continue;
    segments.push(parseInlineNode(state, child));
  }
  if (pos < node.to) {
    segments.push({ type: "text", value: state.sliceDoc(pos, node.to) });
  }
  return segments;
}

function parseInlineNode(state, node) {
  switch (node.name) {
    case "StrongEmphasis":
      return { type: "strong", children: parseInlineSegments(state, node) };
    case "Emphasis":
      return { type: "emphasis", children: parseInlineSegments(state, node) };
    case "Strikethrough":
      return { type: "strikethrough", children: parseInlineSegments(state, node) };
    case "InlineCode": {
      const marks = node.getChildren("CodeMark");
      const from = marks.length ? marks[0].to : node.from;
      const to = marks.length > 1 ? marks[marks.length - 1].from : node.to;
      return { type: "code", value: state.sliceDoc(from, to) };
    }
    case "Link": {
      const url = node.getChild("URL");
      const href = url ? state.sliceDoc(url.from, url.to) : "";
      return { type: "link", href, children: parseInlineSegments(state, node) };
    }
    case "WikiLink": {
      const text = state.sliceDoc(node.from + 2, node.to - 2);
      return { type: "wikilink", href: `[[${text}]]`, children: [{ type: "text", value: text }] };
    }
    default:
      return { type: "text", value: state.sliceDoc(node.from, node.to) };
  }
}

function trimSegments(segments) {
  if (segments.length > 0) {
    const first = segments[0];
    if (first.type === "text") first.value = first.value.replace(/^\s+/, "");
    const last = segments[segments.length - 1];
    if (last.type === "text") last.value = last.value.replace(/\s+$/, "");
  }
  return segments.filter((s) => !(s.type === "text" && s.value === ""));
}

function parseCell(state, cell) {
  return {
    text: state.sliceDoc(cell.from, cell.to).trim(),
    segments: trimSegments(parseInlineSegments(state, cell)),
  };
}

function parseTableCells(state, node) {
  const headers = [];
  const alignments = [];
  const rows = [];

  for (let child = node.firstChild; child; child = child.nextSibling) {
    if (child.name === "TableHeader") {
      for (let cell = child.firstChild; cell; cell = cell.nextSibling) {
        if (cell.name === "TableCell") {
          headers.push(parseCell(state, cell));
        }
      }
    } else if (child.name === "TableDelimiter") {
      const delimText = state.sliceDoc(child.from, child.to);
      const parts = delimText.split("|").filter((p) => p.trim());
      for (const part of parts) {
        const trimmed = part.trim();
        if (trimmed.startsWith(":") && trimmed.endsWith(":")) alignments.push("center");
        else if (trimmed.endsWith(":")) alignments.push("right");
        else alignments.push("left");
      }
    } else if (child.name === "TableRow") {
      const row = [];
      for (let cell = child.firstChild; cell; cell = cell.nextSibling) {
        if (cell.name === "TableCell") {
          row.push(parseCell(state, cell));
        }
      }
      rows.push(row);
    }
  }

  return { headers, alignments, rows };
}

function renderInlineSegments(parent, segments) {
  for (const seg of segments) {
    parent.appendChild(renderInlineSegment(seg));
  }
}

function renderInlineSegment(seg) {
  if (seg.type === "text") {
    return document.createTextNode(seg.value);
  }
  if (seg.type === "code") {
    const el = document.createElement("span");
    el.className = "cm-inline-code";
    el.textContent = seg.value;
    return el;
  }

  const classMap = {
    strong: "cm-strong",
    emphasis: "cm-emphasis",
    strikethrough: "cm-strikethrough",
    link: "cm-link",
    wikilink: "cm-wikilink",
  };

  const el = document.createElement("span");
  el.className = classMap[seg.type] ?? "";
  if (seg.href) el.dataset.href = seg.href;
  renderInlineSegments(el, seg.children ?? []);
  return el;
}

class TableWidget extends WidgetType {
  constructor(tableData) {
    super();
    this.tableData = tableData;
  }

  get estimatedHeight() {
    return (1 + this.tableData.rows.length) * 32;
  }

  eq(other) {
    const a = this.tableData;
    const b = other.tableData;
    if (a.headers.length !== b.headers.length) return false;
    if (a.rows.length !== b.rows.length) return false;
    for (let i = 0; i < a.headers.length; i++) {
      if (a.headers[i].text !== b.headers[i].text) return false;
    }
    for (let i = 0; i < a.rows.length; i++) {
      if (a.rows[i].length !== b.rows[i].length) return false;
      for (let j = 0; j < a.rows[i].length; j++) {
        if (a.rows[i][j].text !== b.rows[i][j].text) return false;
      }
    }
    return true;
  }

  toDOM() {
    const { headers, alignments, rows } = this.tableData;
    const table = document.createElement("table");
    table.className = "cm-table-widget";

    const thead = document.createElement("thead");
    const headerRow = document.createElement("tr");
    for (let i = 0; i < headers.length; i++) {
      const th = document.createElement("th");
      renderInlineSegments(th, headers[i].segments);
      if (alignments[i]) th.style.textAlign = alignments[i];
      headerRow.appendChild(th);
    }
    thead.appendChild(headerRow);
    table.appendChild(thead);

    const tbody = document.createElement("tbody");
    for (const row of rows) {
      const tr = document.createElement("tr");
      for (let i = 0; i < row.length; i++) {
        const td = document.createElement("td");
        renderInlineSegments(td, row[i].segments);
        if (alignments[i]) td.style.textAlign = alignments[i];
        tr.appendChild(td);
      }
      tbody.appendChild(tr);
    }
    table.appendChild(tbody);

    return table;
  }

  ignoreEvent() {
    return false;
  }
}

class BlockMathWidget extends WidgetType {
  constructor(tex) {
    super();
    this.tex = tex;
  }

  get estimatedHeight() {
    return 60;
  }

  eq(other) {
    return this.tex === other.tex;
  }

  toDOM() {
    const div = document.createElement("div");
    div.className = "cm-math-block";
    try {
      div.innerHTML = katex.renderToString(this.tex, {
        throwOnError: false,
        displayMode: true,
      });
    } catch (e) {
      div.textContent = this.tex;
    }
    return div;
  }

  ignoreEvent() {
    return false;
  }
}

function buildBlockDecorations(state) {
  const decorations = [];
  const tree = syntaxTree(state);

  tree.iterate({
    enter(node) {
      if (node.name === "Table") {
        const from = state.doc.lineAt(node.from).from;
        const to = state.doc.lineAt(node.to).to;

        if (isRangeActive(state, from, to)) return;

        const tableData = parseTableCells(state, node.node);
        if (tableData.headers.length === 0) return;

        decorations.push(
          Decoration.replace({
            widget: new TableWidget(tableData),
            block: true,
          }).range(from, to)
        );
      }

      if (node.name === "BlockMath") {
        const from = state.doc.lineAt(node.from).from;
        const to = state.doc.lineAt(node.to).to;

        if (isRangeActive(state, from, to)) return;

        const fullText = state.sliceDoc(node.from, node.to);
        const tex = fullText.replace(/^\$\$\s*\n?/, "").replace(/\n?\s*\$\$$/, "");
        if (!tex.trim()) return;

        decorations.push(
          Decoration.replace({
            widget: new BlockMathWidget(tex),
            block: true,
          }).range(from, to)
        );
      }
    },
  });

  decorations.sort((a, b) => a.from - b.from || a.to - b.to);
  return Decoration.set(decorations);
}

export const blockWidgetField = StateField.define({
  create(state) {
    return buildBlockDecorations(state);
  },
  update(value, tr) {
    if (tr.docChanged || tr.selection) {
      return buildBlockDecorations(tr.state);
    }
    const oldTree = syntaxTree(tr.startState);
    const newTree = syntaxTree(tr.state);
    if (oldTree !== newTree) {
      return buildBlockDecorations(tr.state);
    }
    return value.map(tr.changes);
  },
  provide: (f) => EditorView.decorations.from(f),
});

export const blockAtomicRanges = EditorView.atomicRanges.of((view) => {
  return view.state.field(blockWidgetField);
});

export const trailingNewlineGuard = EditorState.transactionFilter.of((tr) => {
  if (!tr.docChanged) return tr;
  const doc = tr.newDoc;
  const lastLine = doc.line(doc.lines);
  if (lastLine.text.trim() === "") return tr;

  const tree = syntaxTree(tr.state);
  let insideBlock = false;
  tree.iterate({
    enter(node) {
      if (node.name === "Table" || node.name === "BlockMath") {
        if (node.to >= lastLine.from) insideBlock = true;
      }
    },
  });
  if (insideBlock) {
    return [tr, { changes: { from: doc.length, insert: "\n" } }];
  }
  return tr;
});
