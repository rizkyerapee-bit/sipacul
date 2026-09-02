import { describe, expect, it } from "vitest";
import type { Commodity, CommodityCategory } from "@/lib/api/contracts";
import {
  commodityCategoryDraftFrom,
  commodityDraftFrom,
  filterCommodities,
  normalizeCommodityCode,
  optionalMasterDataText,
  validateCommodityCategoryDraft,
  validateCommodityDraft,
} from "@/lib/master-data/commodity-management";

const category: CommodityCategory = {
  id: "category-1",
  organizationId: "org-1",
  name: "Tanaman Hortikultura",
  description: "Sayur dan buah semusim",
  isActive: true,
  createdAt: "2026-08-31T00:00:00Z",
  updatedAt: null,
};

const commodity: Commodity = {
  id: "commodity-1",
  organizationId: "org-1",
  code: "CABAI",
  name: "Cabai Merah",
  commodityCategoryId: category.id,
  scientificName: "Capsicum annuum",
  description: "Komoditas uji",
  isActive: true,
  createdAt: "2026-08-31T00:00:00Z",
  updatedAt: null,
};

describe("commodity master-data helpers", () => {
  it("creates stable drafts for new and existing data", () => {
    expect(commodityCategoryDraftFrom(null)).toEqual({
      name: "",
      description: "",
    });
    expect(commodityCategoryDraftFrom(category).name).toBe("Tanaman Hortikultura");

    expect(commodityDraftFrom(null)).toEqual({
      code: "",
      name: "",
      commodityCategoryId: "",
      scientificName: "",
      description: "",
    });
    expect(commodityDraftFrom(commodity).code).toBe("CABAI");
  });

  it("normalizes commodity code and optional text", () => {
    expect(normalizeCommodityCode("  cabai-01  ")).toBe("CABAI-01");
    expect(optionalMasterDataText("  data  ")).toBe("data");
    expect(optionalMasterDataText("   ")).toBeNull();
  });

  it("validates category requirements and backend length limits", () => {
    expect(validateCommodityCategoryDraft({
      name: "",
      description: "",
    })).toContain("Nama kategori wajib diisi.");

    expect(validateCommodityCategoryDraft({
      name: "A".repeat(151),
      description: "B".repeat(501),
    })).toHaveLength(2);

    expect(validateCommodityCategoryDraft({
      name: "Tanaman Pangan",
      description: "",
    })).toEqual([]);
  });

  it("validates required commodity references and supported code characters", () => {
    expect(validateCommodityDraft({
      code: "CABAI MERAH",
      name: "",
      commodityCategoryId: "",
      scientificName: "",
      description: "",
    }, true)).toHaveLength(3);

    expect(validateCommodityDraft({
      code: "cabai-01",
      name: "Cabai",
      commodityCategoryId: "category-1",
      scientificName: "",
      description: "",
    }, true)).toEqual([]);
  });

  it("does not revalidate immutable code while editing", () => {
    expect(validateCommodityDraft({
      code: "",
      name: "Cabai",
      commodityCategoryId: "category-1",
      scientificName: "",
      description: "",
    }, false)).toEqual([]);
  });

  it("filters by status, code, name, category, and scientific name", () => {
    const inactive = {
      ...commodity,
      id: "commodity-2",
      code: "PADI",
      name: "Padi",
      scientificName: "Oryza sativa",
      isActive: false,
    };

    expect(filterCommodities([commodity, inactive], [category], "cabai", "all"))
      .toEqual([commodity]);
    expect(filterCommodities([commodity, inactive], [category], "hortikultura", "all"))
      .toEqual([commodity, inactive]);
    expect(filterCommodities([commodity, inactive], [category], "oryza", "inactive"))
      .toEqual([inactive]);
    expect(filterCommodities([commodity, inactive], [category], "", "active"))
      .toEqual([commodity]);
  });
});
