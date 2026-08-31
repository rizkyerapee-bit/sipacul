import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

const css = readFileSync(
  resolve(process.cwd(), "src/components/dashboard-shell.module.css"),
  "utf8",
);
const shell = readFileSync(
  resolve(process.cwd(), "src/components/dashboard-shell.tsx"),
  "utf8",
);

describe("dashboard shell design compatibility foundation", () => {
  it("pins the approved design tokens and desktop shell geometry", () => {
    expect(css).toContain("UI-001 COMPATIBILITY FOUNDATION");
    expect(css).toContain("--sidebar-w: 272px;");
    expect(css).toContain("--sidebar-w-collapsed: 64px;");
    expect(css).toContain("--topbar-h: 48px;");
    expect(css).toContain("--content-max-w: 1536px;");
    expect(css).toContain("--font-sans: Inter");
    expect(css).toContain('--font-serif: Georgia, "Times New Roman", serif;');
  });

  it("keeps legacy app tokens as compatibility aliases", () => {
    expect(css).toContain("--app-background: var(--background);");
    expect(css).toContain("--app-surface: var(--card);");
    expect(css).toContain("--app-green-700: var(--primary);");
    expect(css).toContain("--app-green-600: var(--ring);");
    expect(css).toContain("--app-green-50: var(--accent);");
    expect(css).toContain("--app-danger: var(--destructive);");
  });

  it("limits the structural restyle to desktop while mobile remains on the existing drawer contract", () => {
    expect(css).toContain("@media (min-width: 961px)");
    expect(css).toContain("grid-template-columns: var(--sidebar-w) minmax(0, 1fr);");
    expect(css).toContain("grid-template-columns: var(--sidebar-w-collapsed) minmax(0, 1fr);");
  });

  it("retains navigation captions as accessible metadata when the desktop visual caption is hidden", () => {
    expect(css).toContain(".navigationCopy small {");
    expect(css).toContain("display: none;");
    expect(shell).toContain('aria-label={`${item.label}. ${item.caption}`}');
    expect(shell).toContain('title={`${item.label} - ${item.caption}`}');
  });
});
