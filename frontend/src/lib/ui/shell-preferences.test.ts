import { describe, expect, it, vi } from "vitest";
import {
  readSidebarCollapsed,
  readThemePreference,
  resolveSidebarCollapsed,
  resolveThemePreference,
  setSidebarCollapsed,
  setThemePreference,
} from "@/lib/ui/shell-preferences";

describe("application shell preferences", () => {
  it("uses a stored theme before the system preference", () => {
    expect(resolveThemePreference("light", true)).toBe("light");
    expect(resolveThemePreference("dark", false)).toBe("dark");
  });

  it("falls back to the system theme when no valid preference exists", () => {
    expect(resolveThemePreference(null, true)).toBe("dark");
    expect(resolveThemePreference("unexpected", false)).toBe("light");
  });

  it("persists and restores the theme", () => {
    vi.stubGlobal("window", {
      matchMedia: vi.fn().mockReturnValue({ matches: false }),
    });

    setThemePreference("dark");

    expect(readThemePreference()).toBe("dark");
  });

  it("only treats the literal true value as a collapsed sidebar", () => {
    expect(resolveSidebarCollapsed("true")).toBe(true);
    expect(resolveSidebarCollapsed("false")).toBe(false);
    expect(resolveSidebarCollapsed(null)).toBe(false);
  });

  it("persists and restores the desktop sidebar preference", () => {
    setSidebarCollapsed(true);
    expect(readSidebarCollapsed()).toBe(true);

    setSidebarCollapsed(false);
    expect(readSidebarCollapsed()).toBe(false);
  });
});
