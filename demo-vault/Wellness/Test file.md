# Markdown Preview Test Document
# This is a Test

> **Goal:** This file is meant to stress-test a Markdown renderer/preview.
> It includes most common (and some uncommon) features.

---
TEST
## 1. Headings

# H1 – Level 1
## H2 – Level 2
### H3 – Level 3
#### H4 – Level 4
##### H5 – Level 5
###### H6 – Level 6

---

## 2. Paragraphs, Line Breaks, and Emphasis

This is a normal paragraph. It contains some **bold text**, some *italic text*,
and some ***bold italic text***.

This is a second paragraph with  
a manual line break in the middle (two spaces at the end of the line).

You can also ~~strike through text~~ when needed.

Inline `code` can appear in the middle of a sentence.

---

## 3. Links and Images

### 3.1 Standard Links

- Inline link: [OpenAI](https://www.openai.com)
- Reference link: [GitHub][github]

[github]: https://github.com "GitHub Homepage"

### 3.2 Images

Inline image (may or may not render depending on your preview environment):

![Test image](https://picsum.photos/400/200)
---

## 4. Lists

### 4.1 Unordered Lists

- Item 1
- Item 2
  - Nested item 2.1
  - Nested item 2.2
    - Deeply nested item 2.2.1
- Item 3

Alternative bullets:

* Asterisk item
* Another asterisk item
  * Nested asterisk item

### 4.2 Ordered Lists

1. First item
2. Second item
   1. Sub-item 2.1
   2. Sub-item 2.2
3. Third item

### 4.3 Mixed List

1. First
   - Sub bullet 1
   - Sub bullet 2
2. Second
   - Sub bullet

---

## 5. Task Lists

- [ ] Unchecked task
- [x] Checked task
- [ ] Another task
  - [x] Subtask A
  - [x] Subtask B

---

## 6. Blockquotes

> This is a single-line blockquote.

> This is a multi-line blockquote.
> It spans multiple lines and should render as one block.

Nested blockquotes:

> Level 1
>> Level 2
>>> Level 3

Blockquote with other elements:

> ### Quoted Heading
> - Item 1
> - Item 2
> 
> **Bold text inside a quote.**

---

## 7. Code

### 7.1 Inline Code

Use the `console.log()` function in JavaScript.

### 7.2 Fenced Code Blocks

```Bash
# Bash example
echo "Hello, world!"
ls -la
```

// JavaScript example
```js
function greet(name) {
  console.log(`Hello, ${name}!`);
}

greet("Markdown Tester");
```

8. Tables
8.1 Simple Table
| Name    | Age | City     |
| ------- | --- | -------- |
| Alice   | 30  | London   |
| Bob     | 25  | Paris    |
| Charlie | 35  | New York |

8.2 Table with formatting
| Feature  | Supported | Notes                          |
| -------- | --------: | ------------------------------ |
| **Bold** |       Yes | Works in table cells           |
| *Italic* |       Yes | Also works here                |
| `Code`   |       Yes | Inline code in table cells     |
| Links    |       Yes | [Example](https://example.com) |

14. HTML Inside Markdown
<p> This is a paragraph written in <strong>HTML</strong> inside Markdown. It may or may not be allowed, depending on the renderer settings. </p> <div> <em>Custom HTML block</em> with a <span style="text-decoration: underline;">span</span>. </div>

15. Collapsible Sections (GitHub-style details/summary)
<details> <summary>Click to expand</summary>
Hidden content inside the collapsible section.
It can contain lists
Bold text
Code
</details>