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

function parseTableCells(state, node) {
  const headers = [];
  const alignments = [];
  const rows = [];

  for (let child = node.firstChild; child; child = child.nextSibling) {
    if (child.name === "TableHeader") {
      for (let cell = child.firstChild; cell; cell = cell.nextSibling) {
        if (cell.name === "TableCell") {
          headers.push(state.sliceDoc(cell.from, cell.to).trim());
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
          row.push(state.sliceDoc(cell.from, cell.to).trim());
        }
      }
      rows.push(row);
    }
  }

  return { headers, alignments, rows };
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
      if (a.headers[i] !== b.headers[i]) return false;
    }
    for (let i = 0; i < a.rows.length; i++) {
      if (a.rows[i].length !== b.rows[i].length) return false;
      for (let j = 0; j < a.rows[i].length; j++) {
        if (a.rows[i][j] !== b.rows[i][j]) return false;
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
      th.textContent = headers[i];
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
        td.textContent = row[i];
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
