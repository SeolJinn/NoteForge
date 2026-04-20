import { build } from "esbuild";
import { readFileSync, writeFileSync } from "fs";

await build({
  entryPoints: ["src/editor.js"],
  bundle: true,
  minify: true,
  format: "iife",
  outdir: "dist",
  loader: {
    ".woff2": "empty",
    ".woff": "empty",
    ".ttf": "empty",
  },
});

const js = readFileSync("dist/editor.js", "utf8").replaceAll("</script>", "<\\/script>");
let katexCss = readFileSync("dist/editor.css", "utf8");

// Strip @font-face blocks to keep the HTML under WebView2's 2MB NavigateToString limit.
// KaTeX renders fine in Chromium (WebView2) using system math fonts.
katexCss = katexCss.replace(/@font-face\s*\{[^}]*\}/g, "");

const output = `<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
${katexCss}
  :root {
    --editor-bg: #1e1e1e;
    --editor-fg: #d4d4d4;
    --editor-selection: #264f78;
    --heading-color: #ffffff;
    --link-color: #4fc3f7;
    --code-bg: #2d2d2d;
    --code-fg: #ce9178;
    --border-color: #333333;
  }
  * { margin: 0; padding: 0; box-sizing: border-box; }
  html, body { height: 100%; background: var(--editor-bg); color: var(--editor-fg); }
  #editor { height: 100%; }
  .cm-editor { height: 100%; }
  .cm-scroller { overflow: auto; scrollbar-width: thin; scrollbar-color: #3f3f3f var(--editor-bg); }
  .cm-scroller:hover { scrollbar-color: #555555 var(--editor-bg); }
</style>
</head>
<body>
<div id="editor"></div>
<script>
${js}
</script>
</body>
</html>`;

writeFileSync("dist/inline-editor.html", output);
console.log("Built dist/inline-editor.html");
