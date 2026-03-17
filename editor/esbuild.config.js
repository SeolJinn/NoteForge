import { build } from "esbuild";
import { readFileSync, writeFileSync } from "fs";

await build({
  entryPoints: ["src/editor.js"],
  bundle: true,
  minify: true,
  format: "iife",
  outfile: "dist/editor-bundle.js",
});

const js = readFileSync("dist/editor-bundle.js", "utf8").replaceAll("</script>", "<\\/script>");

const output = `<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
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
  .cm-scroller { overflow: auto; }
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
