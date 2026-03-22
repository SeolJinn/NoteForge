export const InlineMath = {
  defineNodes: [{ name: "InlineMath" }],
  parseInline: [
    {
      name: "InlineMath",
      parse(cx, next, pos) {
        if (next !== 36) return -1;
        if (cx.char(pos + 1) === 36) return -1;

        if (pos > cx.offset && cx.char(pos - 1) === 92) return -1;

        const afterOpen = cx.char(pos + 1);
        if (afterOpen === 32 || afterOpen === 10 || afterOpen === -1) return -1;

        let end = pos + 1;
        while (end < cx.end) {
          const ch = cx.char(end);
          if (ch === 10) return -1;
          if (ch === 92) { end += 2; continue; }
          if (ch === 36) {
            if (cx.char(end - 1) === 32) return -1;
            if (end + 1 < cx.end && cx.char(end + 1) === 36) return -1;
            return cx.addElement(cx.elt("InlineMath", pos, end + 1));
          }
          end++;
        }
        return -1;
      },
    },
  ],
};

export const BlockMath = {
  defineNodes: [{ name: "BlockMath", block: true }],
  parseBlock: [
    {
      name: "BlockMath",
      parse(cx, line) {
        if (!/^\$\$\s*$/.test(line.text)) return false;

        const start = cx.lineStart;
        while (cx.nextLine()) {
          if (/^\$\$\s*$/.test(line.text)) {
            const end = cx.lineStart + line.text.length;
            cx.nextLine();
            cx.addElement(cx.elt("BlockMath", start, end));
            return true;
          }
        }
        return false;
      },
    },
  ],
};
