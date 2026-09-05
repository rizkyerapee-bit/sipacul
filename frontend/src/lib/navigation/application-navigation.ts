export type NavigationIconName =
  | "dashboard"
  | "land"
  | "sprout"
  | "harvest"
  | "sales"
  | "finance"
  | "share"
  | "history"
  | "catalog"
  | "team";

export type ApplicationNavigationItem = {
  id: string;
  label: string;
  caption: string;
  keywords: string[];
  permission: string | null;
  icon: NavigationIconName;
  path: string;
  groupId: string | null;
  groupLabel: string | null;
};

export type ApplicationNavigationGroup = {
  id: string;
  label: string;
  items: ApplicationNavigationItem[];
};

const COLLAPSED_GROUPS_STORAGE_KEY = "sipacul.navigation.collapsedGroups.v1";
const RECENT_PATHS_STORAGE_KEY = "sipacul.navigation.recentPaths.v1";

export const dashboardNavigationItem: ApplicationNavigationItem = {
  id: "dashboard",
  label: "Ringkasan",
  caption: "Kondisi usaha hari ini",
  keywords: ["dashboard", "beranda", "overview", "kondisi usaha", "ringkasan"],
  permission: null,
  icon: "dashboard",
  path: "/dashboard",
  groupId: null,
  groupLabel: null,
};

export const applicationNavigationGroups: ApplicationNavigationGroup[] = [
  {
    id: "operations",
    label: "Operasional",
    items: [
      {
        id: "lands",
        label: "Lahan",
        caption: "Lahan, petak, dan sewa",
        keywords: ["lahan", "petak", "blok", "sewa", "tanah"],
        permission: "lands.read",
        icon: "land",
        path: "/lands",
        groupId: "operations",
        groupLabel: "Operasional",
      },
      {
        id: "crop-cycles",
        label: "Siklus Budidaya",
        caption: "Musim dan siklus tanaman",
        keywords: ["budidaya", "siklus", "musim", "tanaman", "tanam"],
        permission: "cultivation.read",
        icon: "sprout",
        path: "/cultivation",
        groupId: "operations",
        groupLabel: "Operasional",
      },
      {
        id: "cultivation-activities",
        label: "Aktivitas Budidaya",
        caption: "Pekerjaan dan biaya lapangan",
        keywords: [
          "aktivitas",
          "aktifitas",
          "kegiatan",
          "pekerjaan",
          "pupuk",
          "pemupukan",
          "tenaga kerja",
          "alat",
          "biaya lapangan",
        ],
        permission: "cultivation.read",
        icon: "sprout",
        path: "/cultivation/activities",
        groupId: "operations",
        groupLabel: "Operasional",
      },
      {
        id: "harvest",
        label: "Panen",
        caption: "Hasil, batch, dan kualitas",
        keywords: ["panen", "hasil panen", "batch", "kualitas", "produksi"],
        permission: "harvest.read",
        icon: "harvest",
        path: "/harvest",
        groupId: "operations",
        groupLabel: "Operasional",
      },
      {
        id: "sales",
        label: "Penjualan",
        caption: "Transaksi hasil panen",
        keywords: ["penjualan", "jual", "pelanggan", "transaksi", "invoice"],
        permission: "sales.read",
        icon: "sales",
        path: "/sales",
        groupId: "operations",
        groupLabel: "Operasional",
      },
    ],
  },
  {
    id: "finance",
    label: "Keuangan",
    items: [
      {
        id: "receivables",
        label: "Piutang & Pembayaran",
        caption: "Tagihan dan pembayaran penjualan",
        keywords: ["piutang", "pembayaran", "tagihan", "jatuh tempo", "kas masuk"],
        permission: "finance.read",
        icon: "finance",
        path: "/finance",
        groupId: "finance",
        groupLabel: "Keuangan",
      },
      {
        id: "expenses",
        label: "Pengeluaran",
        caption: "Biaya dan kas keluar budidaya",
        keywords: ["pengeluaran", "biaya", "expense", "operasional", "kas keluar"],
        permission: "finance.read",
        icon: "finance",
        path: "/finance/expenses",
        groupId: "finance",
        groupLabel: "Keuangan",
      },
      {
        id: "profit-sharing",
        label: "Bagi Hasil",
        caption: "Investor, mitra, dan settlement",
        keywords: ["bagi hasil", "investor", "mitra", "keuntungan", "settlement"],
        permission: "profit-sharing.read",
        icon: "share",
        path: "/profit-sharing",
        groupId: "finance",
        groupLabel: "Keuangan",
      },
    ],
  },
  {
    id: "evaluation",
    label: "Evaluasi",
    items: [
      {
        id: "season-history",
        label: "Histori Musim",
        caption: "Riwayat lahan dan musim tanam",
        keywords: ["histori", "riwayat", "evaluasi", "musim", "lahan", "season"],
        permission: "finance.read",
        icon: "history",
        path: "/evaluations/season-history",
        groupId: "evaluation",
        groupLabel: "Evaluasi",
      },
    ],
  },
  {
    id: "master-data",
    label: "Master Data",
    items: [
      {
        id: "commodities",
        label: "Komoditas",
        caption: "Komoditas dan kategori",
        keywords: ["komoditas", "kategori", "nanas", "tebu", "cabai", "master data"],
        permission: "master-data.read",
        icon: "catalog",
        path: "/master-data/commodities",
        groupId: "master-data",
        groupLabel: "Master Data",
      },
      {
        id: "cultivation-sops",
        label: "SOP Budidaya",
        caption: "Standar dan tahapan budidaya",
        keywords: ["sop", "standar", "budidaya", "tahapan", "prosedur", "komoditas", "master data"],
        permission: "master-data.read",
        icon: "catalog",
        path: "/master-data/cultivation-sops",
        groupId: "master-data",
        groupLabel: "Master Data",
      },
    ],
  },
  {
    id: "organization",
    label: "Organisasi",
    items: [
      {
        id: "organization-members",
        label: "Anggota & Akses",
        caption: "Tim, peran, dan status akses",
        keywords: [
          "anggota",
          "akses",
          "tim",
          "organisasi",
          "role",
          "peran",
          "admin",
          "finance",
          "operator",
          "suspend",
        ],
        permission: "members.read",
        icon: "team",
        path: "/organization/members",
        groupId: "organization",
        groupLabel: "Organisasi",
      },
    ],
  },
];

export function flattenNavigationGroups(
  groups: ApplicationNavigationGroup[],
): ApplicationNavigationItem[] {
  return groups.flatMap((group) => group.items);
}

export const allApplicationNavigationItems: ApplicationNavigationItem[] = [
  dashboardNavigationItem,
  ...flattenNavigationGroups(applicationNavigationGroups),
];

export function filterNavigationGroups(
  groups: ApplicationNavigationGroup[],
  permissions: string[],
): ApplicationNavigationGroup[] {
  return groups
    .map((group) => ({
      ...group,
      items: group.items.filter(
        (item) => !item.permission || permissions.includes(item.permission),
      ),
    }))
    .filter((group) => group.items.length > 0);
}

export function getNavigationItemForPath(
  pathname: string,
  items: ApplicationNavigationItem[],
): ApplicationNavigationItem | null {
  const exact = items.find((item) => item.path === pathname);
  if (exact) {
    return exact;
  }

  const candidates = items
    .filter((item) => pathname.startsWith(`${item.path}/`))
    .sort((left, right) => right.path.length - left.path.length);

  return candidates[0] ?? null;
}

function normalizeSearchText(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("id-ID")
    .replace(/[^a-z0-9]+/g, " ")
    .trim();
}

function editDistance(left: string, right: string): number {
  const previous = Array.from({ length: right.length + 1 }, (_, index) => index);

  for (let leftIndex = 1; leftIndex <= left.length; leftIndex += 1) {
    const current = [leftIndex];

    for (let rightIndex = 1; rightIndex <= right.length; rightIndex += 1) {
      const substitutionCost = left[leftIndex - 1] === right[rightIndex - 1] ? 0 : 1;
      current[rightIndex] = Math.min(
        current[rightIndex - 1] + 1,
        previous[rightIndex] + 1,
        previous[rightIndex - 1] + substitutionCost,
      );
    }

    previous.splice(0, previous.length, ...current);
  }

  return previous[right.length];
}

function fuzzyTokenMatches(queryToken: string, targetToken: string): boolean {
  if (targetToken.includes(queryToken) || queryToken.includes(targetToken)) {
    return true;
  }

  if (queryToken.length < 4 || targetToken.length < 4) {
    return false;
  }

  const allowedDistance = Math.max(queryToken.length, targetToken.length) >= 8 ? 2 : 1;
  return editDistance(queryToken, targetToken) <= allowedDistance;
}

function searchScore(item: ApplicationNavigationItem, query: string): number {
  const normalizedQuery = normalizeSearchText(query);
  if (!normalizedQuery) {
    return 0;
  }

  const label = normalizeSearchText(item.label);
  const caption = normalizeSearchText(item.caption);
  const group = normalizeSearchText(item.groupLabel ?? "");
  const path = normalizeSearchText(item.path);
  const keywords = item.keywords.map(normalizeSearchText);
  const queryTokens = normalizedQuery.split(" ").filter(Boolean);
  const targetTokens = [
    ...label.split(" "),
    ...caption.split(" "),
    ...group.split(" "),
    ...path.split(" "),
    ...keywords.flatMap((keyword) => keyword.split(" ")),
  ].filter(Boolean);

  let score = 0;

  if (label === normalizedQuery) score += 240;
  if (label.startsWith(normalizedQuery)) score += 180;
  if (label.includes(normalizedQuery)) score += 140;
  if (keywords.some((keyword) => keyword === normalizedQuery)) score += 170;
  if (keywords.some((keyword) => keyword.includes(normalizedQuery))) score += 120;
  if (caption.includes(normalizedQuery)) score += 80;
  if (group.includes(normalizedQuery)) score += 55;
  if (path.includes(normalizedQuery)) score += 35;

  const matchedTokens = queryTokens.filter((queryToken) =>
    targetTokens.some((targetToken) => fuzzyTokenMatches(queryToken, targetToken)),
  );

  if (matchedTokens.length === queryTokens.length) {
    score += 60 + matchedTokens.length * 12;
  }

  return score;
}

export function searchNavigationItems(
  items: ApplicationNavigationItem[],
  query: string,
): ApplicationNavigationItem[] {
  const normalizedQuery = normalizeSearchText(query);
  if (!normalizedQuery) {
    return [];
  }

  return items
    .map((item, index) => ({
      item,
      index,
      score: searchScore(item, normalizedQuery),
    }))
    .filter((entry) => entry.score > 0)
    .sort((left, right) => right.score - left.score || left.index - right.index)
    .map((entry) => entry.item);
}

export function resolveCollapsedNavigationGroups(
  storedValue: string | null,
  allowedGroupIds: string[],
): string[] {
  if (!storedValue) {
    return [];
  }

  try {
    const parsed: unknown = JSON.parse(storedValue);
    if (!Array.isArray(parsed)) {
      return [];
    }

    const allowed = new Set(allowedGroupIds);
    return Array.from(new Set(
      parsed.filter(
        (value): value is string => typeof value === "string" && allowed.has(value),
      ),
    ));
  } catch {
    return [];
  }
}

export function readCollapsedNavigationGroups(
  allowedGroupIds: string[],
): string[] {
  return resolveCollapsedNavigationGroups(
    localStorage.getItem(COLLAPSED_GROUPS_STORAGE_KEY),
    allowedGroupIds,
  );
}

export function writeCollapsedNavigationGroups(groupIds: string[]): void {
  localStorage.setItem(COLLAPSED_GROUPS_STORAGE_KEY, JSON.stringify(groupIds));
}

export function resolveRecentNavigationPaths(
  storedValue: string | null,
  allowedPaths: string[],
  limit = 5,
): string[] {
  if (!storedValue) {
    return [];
  }

  try {
    const parsed: unknown = JSON.parse(storedValue);
    if (!Array.isArray(parsed)) {
      return [];
    }

    const allowed = new Set(allowedPaths);
    return Array.from(new Set(
      parsed.filter(
        (value): value is string => typeof value === "string" && allowed.has(value),
      ),
    )).slice(0, limit);
  } catch {
    return [];
  }
}

export function readRecentNavigationPaths(
  allowedPaths: string[],
  limit = 5,
): string[] {
  return resolveRecentNavigationPaths(
    localStorage.getItem(RECENT_PATHS_STORAGE_KEY),
    allowedPaths,
    limit,
  );
}

export function recordRecentNavigationPath(
  recentPaths: string[],
  path: string,
  allowedPaths: string[],
  limit = 5,
): string[] {
  if (!allowedPaths.includes(path)) {
    return recentPaths.slice(0, limit);
  }

  return [path, ...recentPaths.filter((item) => item !== path)]
    .filter((item) => allowedPaths.includes(item))
    .slice(0, limit);
}

export function writeRecentNavigationPaths(paths: string[]): void {
  localStorage.setItem(RECENT_PATHS_STORAGE_KEY, JSON.stringify(paths));
}
