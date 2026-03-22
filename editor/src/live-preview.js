import { ViewPlugin } from "@codemirror/view";
import { buildDecorations } from "./decorations.js";

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
