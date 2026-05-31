export const WikiLink = {
  defineNodes: [{ name: "WikiLink", style: "link" }],
  parseInline: [
    {
      name: "WikiLink",
      before: "Link",
      parse(cx, next, pos) {
        if (next !== 91 || cx.char(pos + 1) !== 91) return -1;
        let end = pos + 2;
        while (end < cx.end) {
          if (cx.char(end) === 93 && cx.char(end + 1) === 93) {
            return cx.addElement(cx.elt("WikiLink", pos, end + 2));
          }
          if (cx.char(end) === 10) return -1;
          end++;
        }
        return -1;
      },
    },
  ],
};
