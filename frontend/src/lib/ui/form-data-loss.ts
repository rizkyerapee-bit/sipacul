export type FormCloseSource = "backdrop" | "explicit" | "escape";

export type FormCloseDecision = "ignore" | "close" | "confirm-discard";

export function resolveFormCloseDecision({
  source,
  isDirty,
  isSaving,
}: {
  source: FormCloseSource;
  isDirty: boolean;
  isSaving: boolean;
}): FormCloseDecision {
  if (isSaving || source === "backdrop") {
    return "ignore";
  }

  return isDirty ? "confirm-discard" : "close";
}

export function hasFormDraftChanged<T extends object>(baseline: T, current: T): boolean {
  const baselineRecord = baseline as Record<string, unknown>;
  const currentRecord = current as Record<string, unknown>;
  const keys = new Set([...Object.keys(baselineRecord), ...Object.keys(currentRecord)]);

  return [...keys].some((key) => !Object.is(baselineRecord[key], currentRecord[key]));
}
