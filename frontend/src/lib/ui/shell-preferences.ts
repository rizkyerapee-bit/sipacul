export type ThemePreference = "light" | "dark";

const THEME_STORAGE_KEY = "sipacul.theme";
const SIDEBAR_STORAGE_KEY = "sipacul.sidebarCollapsed";

export function resolveThemePreference(
  storedValue: string | null,
  prefersDark: boolean,
): ThemePreference {
  if (storedValue === "dark" || storedValue === "light") {
    return storedValue;
  }

  return prefersDark ? "dark" : "light";
}

export function readThemePreference(): ThemePreference {
  const prefersDark = window.matchMedia?.("(prefers-color-scheme: dark)").matches ?? false;

  return resolveThemePreference(
    localStorage.getItem(THEME_STORAGE_KEY),
    prefersDark,
  );
}

export function setThemePreference(theme: ThemePreference): void {
  localStorage.setItem(THEME_STORAGE_KEY, theme);
}

export function resolveSidebarCollapsed(storedValue: string | null): boolean {
  return storedValue === "true";
}

export function readSidebarCollapsed(): boolean {
  return resolveSidebarCollapsed(localStorage.getItem(SIDEBAR_STORAGE_KEY));
}

export function setSidebarCollapsed(collapsed: boolean): void {
  localStorage.setItem(SIDEBAR_STORAGE_KEY, String(collapsed));
}
