import type { Commodity, CommodityCategory } from "@/lib/api/contracts";

export type MasterDataStatusFilter = "all" | "active" | "inactive";

export type CommodityCategoryDraft = {
  name: string;
  description: string;
};

export type CommodityDraft = {
  code: string;
  name: string;
  commodityCategoryId: string;
  scientificName: string;
  description: string;
};

export function commodityCategoryDraftFrom(
  category: CommodityCategory | null,
): CommodityCategoryDraft {
  return {
    name: category?.name ?? "",
    description: category?.description ?? "",
  };
}

export function commodityDraftFrom(
  commodity: Commodity | null,
): CommodityDraft {
  return {
    code: commodity?.code ?? "",
    name: commodity?.name ?? "",
    commodityCategoryId: commodity?.commodityCategoryId ?? "",
    scientificName: commodity?.scientificName ?? "",
    description: commodity?.description ?? "",
  };
}

export function optionalMasterDataText(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

export function normalizeCommodityCode(value: string): string {
  return value.trim().toUpperCase();
}

export function validateCommodityCategoryDraft(
  draft: CommodityCategoryDraft,
): string[] {
  const errors: string[] = [];
  const name = draft.name.trim();
  const description = draft.description.trim();

  if (!name) {
    errors.push("Nama kategori wajib diisi.");
  } else if (name.length > 150) {
    errors.push("Nama kategori maksimal 150 karakter.");
  }

  if (description.length > 500) {
    errors.push("Deskripsi kategori maksimal 500 karakter.");
  }

  return errors;
}

export function validateCommodityDraft(
  draft: CommodityDraft,
  isCreate: boolean,
): string[] {
  const errors: string[] = [];
  const code = normalizeCommodityCode(draft.code);
  const name = draft.name.trim();

  if (isCreate) {
    if (!code) {
      errors.push("Kode komoditas wajib diisi.");
    } else if (!/^[A-Z0-9_-]+$/.test(code)) {
      errors.push("Kode hanya boleh berisi huruf, angka, tanda hubung, dan garis bawah.");
    }
  }

  if (!name) {
    errors.push("Nama komoditas wajib diisi.");
  }

  if (!draft.commodityCategoryId) {
    errors.push("Kategori komoditas wajib dipilih.");
  }

  return errors;
}

export function matchesMasterDataStatus(
  isActive: boolean,
  filter: MasterDataStatusFilter,
): boolean {
  if (filter === "active") return isActive;
  if (filter === "inactive") return !isActive;
  return true;
}

export function filterCommodities(
  commodities: Commodity[],
  categories: CommodityCategory[],
  query: string,
  status: MasterDataStatusFilter,
): Commodity[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("id-ID");
  const categoryById = new Map(categories.map((category) => [category.id, category.name]));

  return commodities.filter((commodity) => {
    if (!matchesMasterDataStatus(commodity.isActive, status)) {
      return false;
    }

    if (!normalizedQuery) {
      return true;
    }

    const haystack = [
      commodity.code,
      commodity.name,
      commodity.scientificName ?? "",
      categoryById.get(commodity.commodityCategoryId) ?? "",
    ].join(" ").toLocaleLowerCase("id-ID");

    return haystack.includes(normalizedQuery);
  });
}
