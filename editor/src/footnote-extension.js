export const FootnoteReference = {
  defineNodes: [{ name: "FootnoteReference" }],
  parseInline: [
    {
      name: "FootnoteReference",
      before: "Link",
      parse(cx, next, pos) {
        if (next !== 91) return -1;
        if (cx.char(pos + 1) !== 94) return -1;

        let end = pos + 2;
        const idStart = end;
        while (end < cx.end) {
          const ch = cx.char(end);
          if (ch === 93) {
            if (end === idStart) return -1;
            return cx.addElement(cx.elt("FootnoteReference", pos, end + 1));
          }
          if (
            (ch >= 48 && ch <= 57) ||
            (ch >= 65 && ch <= 90) ||
            (ch >= 97 && ch <= 122) ||
            ch === 45 || ch === 95
          ) {
            end++;
          } else {
            return -1;
          }
        }
        return -1;
      },
    },
  ],
};

export const FootnoteDefinition = {
  defineNodes: [
    { name: "FootnoteDefinition", block: true },
    { name: "FootnoteDefinitionMarker" },
  ],
  parseBlock: [
    {
      name: "FootnoteDefinition",
      parse(cx, line) {
        const match = /^\s*\[\^([a-zA-Z0-9_-]+)\]:\s/.exec(line.text);
        if (!match) return false;

        const start = cx.lineStart;
        const markerEnd = cx.lineStart + match[0].length;
        const lineEnd = cx.lineStart + line.text.length;

        cx.nextLine();

        const marker = cx.elt("FootnoteDefinitionMarker", start, markerEnd);
        cx.addElement(cx.elt("FootnoteDefinition", start, lineEnd, [marker]));
        return true;
      },
    },
  ],
};
