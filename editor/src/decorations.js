import { Decoration } from "@codemirror/view";
import { syntaxTree } from "@codemirror/language";
import { RangeSet } from "@codemirror/state";
import {
  CheckboxWidget,
  HorizontalRuleWidget,
  ImageWidget,
  BulletWidget,
  CodeBlockLangWidget,
  MathWidget,
  FootnoteRefWidget,
  FootnoteDefWidget,
} from "./widgets.js";

function isLineActive(view, lineFrom, lineTo) {
  for (const range of view.state.selection.ranges) {
    const selLineFrom = view.state.doc.lineAt(range.from).from;
    const selLineTo = view.state.doc.lineAt(range.to).to;
    if (selLineTo >= lineFrom && selLineFrom <= lineTo) return true;
  }
  return false;
}

export function buildDecorations(view) {
  const decorations = [];
  const tree = syntaxTree(view.state);

  const footnoteNumberMap = new Map();
  let footnoteCount = 0;
  tree.iterate({
    enter(node) {
      if (node.name === "FootnoteDefinition") {
        const text = view.state.sliceDoc(node.from, node.to);
        const match = text.match(/^\[\^([a-zA-Z0-9_-]+)\]/);
        if (match && !footnoteNumberMap.has(match[1])) {
          footnoteNumberMap.set(match[1], ++footnoteCount);
        }
      }
    },
  });

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
          const lineText = view.state.doc.lineAt(node.from).text;
          const isTaskLine = /^\s*[-*+]\s+\[[ xX]\]/.test(lineText);
          if (isTaskLine) {
            decorations.push({
              from: node.from,
              to: node.to + 1,
              decoration: Decoration.replace({}),
            });
          } else {
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
          }
          break;
        }

        case "Blockquote": {
          const firstLine = view.state.doc.lineAt(node.from);
          const lastLine = view.state.doc.lineAt(node.to);
          for (let ln = firstLine.number; ln <= lastLine.number; ln++) {
            const line = view.state.doc.line(ln);
            decorations.push({
              from: line.from,
              to: line.from,
              decoration: Decoration.line({ class: "cm-blockquote-line" }),
            });
            const lineText = view.state.sliceDoc(line.from, line.to);
            const match = lineText.match(/^(\s*>\s?)/);
            if (match) {
              decorations.push({
                from: line.from,
                to: line.from + match[1].length,
                decoration: Decoration.replace({}),
              });
            }
          }
          break;
        }

        case "HorizontalRule": {
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.replace({
              widget: new HorizontalRuleWidget(),
            }),
          });
          break;
        }

        case "TaskMarker": {
          const text = view.state.sliceDoc(node.from, node.to);
          const isChecked = text.includes("x") || text.includes("X");
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.replace({
              widget: new CheckboxWidget(isChecked),
            }),
          });
          if (isChecked) {
            const taskLine = view.state.doc.lineAt(node.from);
            decorations.push({
              from: node.to,
              to: taskLine.to,
              decoration: Decoration.mark({ class: "cm-task-completed" }),
            });
          }
          break;
        }

        case "InlineMath": {
          const tex = view.state.sliceDoc(node.from + 1, node.to - 1);
          decorations.push({
            from: node.from,
            to: node.to,
            decoration: Decoration.replace({
              widget: new MathWidget(tex),
            }),
          });
          break;
        }

        case "FootnoteReference": {
          const refLine = view.state.doc.lineAt(node.from);
          if (/^\s*\[\^[a-zA-Z0-9_-]+\]:\s/.test(refLine.text)) break;
          const text = view.state.sliceDoc(node.from, node.to);
          const match = text.match(/^\[\^([a-zA-Z0-9_-]+)\]$/);
          if (match) {
            const identifier = match[1];
            const displayNumber = footnoteNumberMap.get(identifier) || identifier;
            decorations.push({
              from: node.from,
              to: node.to,
              decoration: Decoration.replace({
                widget: new FootnoteRefWidget(identifier, String(displayNumber)),
              }),
            });
          }
          break;
        }

        case "FootnoteDefinition": {
          const text = view.state.sliceDoc(node.from, node.to);
          const match = text.match(/^\s*\[\^([a-zA-Z0-9_-]+)\]:\s?/);
          if (match) {
            const identifier = match[1];
            const displayNumber = footnoteNumberMap.get(identifier) || identifier;
            const markerEnd = node.from + match[0].length;
            decorations.push({
              from: node.from,
              to: markerEnd,
              decoration: Decoration.replace({
                widget: new FootnoteDefWidget(identifier, String(displayNumber)),
              }),
            });
            if (markerEnd < node.to) {
              decorations.push({
                from: markerEnd,
                to: node.to,
                decoration: Decoration.mark({ class: "cm-footnote-def-text" }),
              });
            }
          }
          break;
        }

        case "Image": {
          const url = node.node.getChild("URL");
          const altText = node.node.getChild("LinkLabel");
          if (url) {
            const src = view.state.sliceDoc(url.from, url.to);
            const alt = altText ? view.state.sliceDoc(altText.from + 1, altText.to - 1) : "";
            decorations.push({
              from: node.from,
              to: node.to,
              decoration: Decoration.replace({
                widget: new ImageWidget(src, alt),
              }),
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
