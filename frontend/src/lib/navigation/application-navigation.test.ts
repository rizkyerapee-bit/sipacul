import { describe, expect, it } from "vitest";
import {
  allApplicationNavigationItems,
  applicationNavigationGroups,
  filterNavigationGroups,
  getNavigationItemForPath,
  recordRecentNavigationPath,
  resolveCollapsedNavigationGroups,
  resolveRecentNavigationPaths,
  searchNavigationItems,
} from "@/lib/navigation/application-navigation";

describe("application navigation", () => {
  it("keeps routes unique and groups master data in the intended order", () => {
    const paths = allApplicationNavigationItems.map((item) => item.path);
    expect(new Set(paths).size).toBe(paths.length);

    const masterData = applicationNavigationGroups.find((group) => group.id === "master-data");
    expect(masterData?.items.map((item) => item.id)).toEqual([
      "commodities",
      "cultivation-sops",
    ]);
    expect(masterData?.items[1]).toMatchObject({
      label: "SOP Budidaya",
      permission: "master-data.read",
      path: "/master-data/cultivation-sops",
    });
  });

  it("filters protected groups with exact permission strings", () => {
    const groups = filterNavigationGroups(applicationNavigationGroups, [
      "cultivation.read",
      "master-data.read",
    ]);
    expect(groups.flatMap((group) => group.items.map((item) => item.id))).toEqual([
      "crop-cycles",
      "cultivation-activities",
      "commodities",
      "cultivation-sops",
    ]);
  });

  it("resolves exact and nested paths using the longest matching route", () => {
    expect(getNavigationItemForPath("/master-data/cultivation-sops", allApplicationNavigationItems)?.id)
      .toBe("cultivation-sops");
    expect(getNavigationItemForPath("/cultivation/activities/today", allApplicationNavigationItems)?.id)
      .toBe("cultivation-activities");
    expect(getNavigationItemForPath("/unknown", allApplicationNavigationItems)).toBeNull();
  });

  it("finds SOP from business language and tolerant search", () => {
    expect(searchNavigationItems(allApplicationNavigationItems, "sop")[0]?.id)
      .toBe("cultivation-sops");
    expect(searchNavigationItems(allApplicationNavigationItems, "standar budidaya")[0]?.id)
      .toBe("cultivation-sops");
    expect(searchNavigationItems(allApplicationNavigationItems, "tahpan")[0]?.id)
      .toBe("cultivation-sops");
    expect(searchNavigationItems(allApplicationNavigationItems, "")).toEqual([]);
  });

  it("sanitizes collapsed groups from storage", () => {
    expect(resolveCollapsedNavigationGroups(
      JSON.stringify(["finance", "finance", "unknown", 4]),
      ["operations", "finance"],
    )).toEqual(["finance"]);
    expect(resolveCollapsedNavigationGroups("not-json", ["finance"])).toEqual([]);
  });

  it("sanitizes and limits recent paths", () => {
    const allowed = allApplicationNavigationItems.map((item) => item.path);
    expect(resolveRecentNavigationPaths(
      JSON.stringify(["/lands", "/lands", "/unknown", "/master-data/cultivation-sops"]),
      allowed,
      3,
    )).toEqual(["/lands", "/master-data/cultivation-sops"]);
    expect(resolveRecentNavigationPaths("{}", allowed)).toEqual([]);
  });

  it("records recent navigation without duplicates", () => {
    const allowed = allApplicationNavigationItems.map((item) => item.path);
    expect(recordRecentNavigationPath(
      ["/lands", "/dashboard"],
      "/master-data/cultivation-sops",
      allowed,
      3,
    )).toEqual(["/master-data/cultivation-sops", "/lands", "/dashboard"]);
    expect(recordRecentNavigationPath(["/lands"], "/unknown", allowed)).toEqual(["/lands"]);
  });
});
