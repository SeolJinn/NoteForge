import { Decoration, ViewPlugin, WidgetType } from "@codemirror/view";
import { syntaxTree } from "@codemirror/language";
import { RangeSet } from "@codemirror/state";

class BulletWidget extends WidgetType {
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

class CodeBlockLangWidget extends WidgetType {
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

function isLineActive(view, lineFrom, lineTo) {
  for (const range of view.state.selection.ranges) {
    const selLineFrom = view.state.doc.lineAt(range.from).from;
    const selLineTo = view.state.doc.lineAt(range.to).to;
    if (selLineTo >= lineFrom && selLineFrom <= lineTo) return true;
  }
  return false;
}

function buildDecorations(view) {
  const decorations = [];
  const tree = syntaxTree(view.state);

  tree.iterate({
    enter(node) {
      const lineFrom = view.state.doc.lineAt(node.from).from;
      const lineTo = view.state.doc.lineAt(node.to).to;

      if (isLineActive(view, lineFrom, lineTo)) return;

      switch (node.name) {
        case "ATXHeading1":
        case "ATXHeading2":
        case "ATXHeading3":
        case "ATXHeading4":
        case "ATXHeading5":
        case "ATXHeading6": {
          const level = parseInt(node.name.slice(-1));
          decorations.push({
            from: lineFrom,
            to: lineFrom,
            decoration: Decoration.line({ class: `cm-heading-${level}` }),
          });
          const headerMark = node.node.getChild("HeaderMark");
          if (headerMark) {
            decorations.push({
              from: headerMark.from,
              to: headerMark.to + 1,
              decoration: Decoration.replace({}),
            });
          }
          break;
        }

        case "StrongEmphasis": {
          const marks = node.node.getChildren("EmphasisMark");
          for (const mark of marks) {
            decorations.push({
              from: mark.from,
              to: mark.to,
              decoration: Decoration.replace({}),
            });
          }
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.mark({ class: "cm-strong" }),
          });
          break;
        }

        case "Emphasis": {
          const marks = node.node.getChildren("EmphasisMark");
          for (const mark of marks) {
            decorations.push({
              from: mark.from,
              to: mark.to,
              decoration: Decoration.replace({}),
            });
          }
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.mark({ class: "cm-emphasis" }),
          });
          break;
        }

        case "Strikethrough": {
          const marks = node.node.getChildren("StrikethroughMark");
          for (const mark of marks) {
            decorations.push({
              from: mark.from,
              to: mark.to,
              decoration: Decoration.replace({}),
            });
          }
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.mark({ class: "cm-strikethrough" }),
          });
          break;
        }

        case "InlineCode": {
          const marks = node.node.getChildren("CodeMark");
          for (const mark of marks) {
            decorations.push({
              from: mark.from,
              to: mark.to,
              decoration: Decoration.replace({}),
            });
          }
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.mark({ class: "cm-inline-code" }),
          });
          break;
        }

        case "FencedCode": {
          const firstLine = view.state.doc.lineAt(node.from);
          const lastLine = view.state.doc.lineAt(node.to);

          for (let ln = firstLine.number; ln <= lastLine.number; ln++) {
            const line = view.state.doc.line(ln);
            decorations.push({
              from: line.from,
              to: line.from,
              decoration: Decoration.line({ class: "cm-code-block-line" }),
            });
          }

          const codeMarks = node.node.getChildren("CodeMark");
          const codeInfo = node.node.getChild("CodeInfo");

          if (codeMarks.length >= 1) {
            const openEnd = codeInfo ? codeInfo.to : codeMarks[0].to;
            decorations.push({
              from: codeMarks[0].from,
              to: openEnd,
              decoration: Decoration.replace({}),
            });

            if (codeInfo) {
              const lang = view.state.sliceDoc(codeInfo.from, codeInfo.to);
              decorations.push({
                from: codeMarks[0].from,
                to: codeMarks[0].from,
                decoration: Decoration.widget({
                  widget: new CodeBlockLangWidget(lang),
                  side: 1,
                }),
              });
            }
          }

          if (codeMarks.length >= 2) {
            decorations.push({
              from: codeMarks[1].from,
              to: codeMarks[1].to,
              decoration: Decoration.replace({}),
            });
          }

          break;
        }

        case "Link": {
          const linkMarks = node.node.getChildren("LinkMark");
          const url = node.node.getChild("URL");
          for (const mark of linkMarks) {
            decorations.push({
              from: mark.from,
              to: mark.to,
              decoration: Decoration.replace({}),
            });
          }
          if (url) {
            decorations.push({
              from: url.from,
              to: url.to,
              decoration: Decoration.replace({}),
            });
          }
          const hrefText = url ? view.state.sliceDoc(url.from, url.to) : "";
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.mark({
              class: "cm-link",
              attributes: { "data-href": hrefText },
            }),
          });
          break;
        }

        case "WikiLink": {
          decorations.push({
            from: node.from,
            to: node.from + 2,
            decoration: Decoration.replace({}),
          });
          decorations.push({
            from: node.to - 2,
            to: node.to,
            decoration: Decoration.replace({}),
          });
          const text = view.state.sliceDoc(node.from + 2, node.to - 2);
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.mark({
              class: "cm-wikilink",
              attributes: { "data-href": `[[${text}]]` },
            }),
          });
          break;
        }

        case "ListMark": {
          const marker = view.state.sliceDoc(node.from, node.to).trim();
          if (marker === "-" || marker === "*" || marker === "+") {
            decorations.push({
              from: node.from,
              to: node.to,
              decoration: Decoration.replace({
                widget: new BulletWidget(),
              }),
            });
          } else {
            decorations.push({
              from: node.from,
              to: node.to,
              decoration: Decoration.mark({ class: "cm-list-bullet" }),
            });
          }
          break;
        }
      }
    },
  });

  decorations.sort((a, b) => a.from - b.from || a.to - b.to);
  return RangeSet.of(decorations.map((d) => d.decoration.range(d.from, d.to)));
}

export const livePreviewPlugin = ViewPlugin.fromClass(
  class {
    decorations;

    constructor(view) {
      this.decorations = buildDecorations(view);
    }

    update(update) {
      if (update.docChanged || update.selectionSet || update.viewportChanged) {
        this.decorations = buildDecorations(update.view);
      }
    }
  },
  {
    decorations: (v) => v.decorations,
  }
);
