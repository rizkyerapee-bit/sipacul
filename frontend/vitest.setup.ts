import { afterEach, vi } from "vitest";

const values = new Map<string, string>();

const memoryStorage: Storage = {
  get length() {
    return values.size;
  },
  clear() {
    values.clear();
  },
  getItem(key) {
    return values.get(key) ?? null;
  },
  key(index) {
    return Array.from(values.keys())[index] ?? null;
  },
  removeItem(key) {
    values.delete(key);
  },
  setItem(key, value) {
    values.set(key, String(value));
  },
};

vi.stubGlobal("localStorage", memoryStorage);

afterEach(() => {
  localStorage.clear();
});