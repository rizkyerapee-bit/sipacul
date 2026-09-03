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
  it("contains every current direct application workspace once", () => {
    expect(allApplicationNavigationItems.map((item) => item.path)).toEqual([
      "/dashboard",
      "/lands",
      "/cultivation",
      "/cultivation/activities",
      "/harvest",
      "/sales",
      "/finance",
      "/finance/expenses",
      "/profit-sharing",
      "/evaluations/season-history",
      "/master-data/commodities",
    ]);
  });

  it("keeps child workspaces distinct from their parent route", () => {
    expect(getNavigationItemForPath(
      "/cultivation/activities",
      allApplicationNavigationItems,
    )?.id).toBe("cultivation-activities");

    expect(getNavigationItemForPath(
      "/finance/expenses",
      allApplicationNavigationItems,
    )?.id).toBe("expenses");

    expect(getNavigationItemForPath(
      "/cultivation/future-detail",
      allApplicationNavigationItems,
    )?.id).toBe("crop-cycles");
  });

  it("filters navigation groups by read permission", () => {
    const visible = filterNavigationGroups(applicationNavigationGroups, [
      "cultivation.read",
      "master-data.read",
    ]);

    expect(visible.map((group) => group.id)).toEqual([
      "operations",
      "master-data",
    ]);
    expect(visible.flatMap((group) => group.items.map((item) => item.path))).toEqual([
      "/cultivation",
      "/cultivation/activities",
      "/master-data/commodities",
    ]);
  });

  it("finds pages by function keywords and forgiving spelling", () => {
    expect(searchNavigationItems(allApplicationNavigationItems, "pupuk")[0]?.path)
      .toBe("/cultivation/activities");
    expect(searchNavigationItems(allApplicationNavigationItems, "aktvitas")[0]?.path)
      .toBe("/cultivation/activities");
    expect(searchNavigationItems(allApplicationNavigationItems, "piutang")[0]?.path)
      .toBe("/finance");
    expect(searchNavigationItems(allApplicationNavigationItems, "bagi hasil")[0]?.path)
      .toBe("/profit-sharing");
  });

  it("does not produce search results for a blank query", () => {
    expect(searchNavigationItems(allApplicationNavigationItems, "   ")).toEqual([]);
  });

  it("restores only known collapsed groups", () => {
    expect(resolveCollapsedNavigationGroups(
      JSON.stringify(["finance", "unknown", "finance", "evaluation"]),
      applicationNavigationGroups.map((group) => group.id),
    )).toEqual(["finance", "evaluation"]);

    expect(resolveCollapsedNavigationGroups("not-json", ["finance"])).toEqual([]);
  });

  it("keeps recent pages valid, unique, and bounded", () => {
    const allowedPaths = allApplicationNavigationItems.map((item) => item.path);

    expect(resolveRecentNavigationPaths(
      JSON.stringify([
        "/finance",
        "/unknown",
        "/finance",
        "/cultivation",
        "/lands",
      ]),
      allowedPaths,
      3,
    )).toEqual(["/finance", "/cultivation", "/lands"]);

    expect(recordRecentNavigationPath(
      ["/finance", "/lands", "/cultivation"],
      "/lands",
      allowedPaths,
      3,
    )).toEqual(["/lands", "/finance", "/cultivation"]);

    expect(recordRecentNavigationPath(
      ["/finance", "/lands"],
      "/unknown",
      allowedPaths,
      3,
    )).toEqual(["/finance", "/lands"]);
  });
});
