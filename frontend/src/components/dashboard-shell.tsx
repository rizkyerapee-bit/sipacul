"use client";

import { useEffect, useMemo, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { BrandMark } from "@/components/brand-mark";
import { DashboardOverview } from "@/components/dashboard-overview";
import { CropCycleManagement } from "@/components/crop-cycle-management";
import { CultivationActivityManagement } from "@/components/cultivation-activity-management";
import { ExpenseManagement } from "@/components/expense-management";
import { HarvestManagement } from "@/components/harvest-management";
import { LandManagement } from "@/components/land-management";
import { CommodityManagement } from "@/components/commodity-management";
import { ProfitSharingManagement } from "@/components/profit-sharing-management";
import { ReceivableManagement } from "@/components/receivable-management";
import { SaleManagement } from "@/components/sale-management";
import { SeasonHistoryManagement } from "@/components/season-history-management";
import { ApiError, getCurrentUser, getOrganization, logout } from "@/lib/api/client";
import type { CurrentUser, CurrentUserMembership, Organization } from "@/lib/api/contracts";
import { getRoleLabel, hasPermission, resolveSelectedMembership, setSelectedOrganizationId } from "@/lib/session/organization-selection";
import {
  readSidebarCollapsed,
  readThemePreference,
  setSidebarCollapsed,
  setThemePreference,
  type ThemePreference,
} from "@/lib/ui/shell-preferences";
import styles from "./dashboard-shell.module.css";

type DashboardState = {
  user: CurrentUser;
  membership: CurrentUserMembership | null;
  organization: Organization | null;
};

type IconName =
  | "dashboard"
  | "land"
  | "sprout"
  | "harvest"
  | "sales"
  | "finance"
  | "share"
  | "team"
  | "menu"
  | "close"
  | "collapse"
  | "expand"
  | "sun"
  | "moon"
  | "chevron"
  | "logout"
  | "shield"
  | "check"
  | "trend"
  | "wallet"
  | "history"
  | "catalog";

type NavigationItem = {
  label: string;
  caption: string;
  permission: string | null;
  icon: IconName;
  path: string | null;
};

const navigation: NavigationItem[] = [
  { label: "Ringkasan", caption: "Kondisi usaha hari ini", permission: null, icon: "dashboard", path: "/dashboard" },
  { label: "Lahan", caption: "Lahan dan petak", permission: "lands.read", icon: "land", path: "/lands" },
  { label: "Master data", caption: "Komoditas dan kategori", permission: "master-data.read", icon: "catalog", path: "/master-data/commodities" },
  { label: "Budidaya", caption: "Siklus dan aktivitas", permission: "cultivation.read", icon: "sprout", path: "/cultivation" },
  { label: "Panen", caption: "Hasil dan kualitas", permission: "harvest.read", icon: "harvest", path: "/harvest" },
  { label: "Penjualan", caption: "Transaksi hasil panen", permission: "sales.read", icon: "sales", path: "/sales" },
  { label: "Keuangan", caption: "Kas, piutang, dan biaya", permission: "finance.read", icon: "finance", path: "/finance" },
  { label: "Bagi hasil", caption: "Investor dan mitra", permission: "profit-sharing.read", icon: "share", path: "/profit-sharing" },
  { label: "Evaluasi", caption: "Histori lahan & musim", permission: "finance.read", icon: "history", path: "/evaluations/season-history" },
  { label: "Tim", caption: "Anggota dan peran", permission: "members.read", icon: "team", path: null },
];

const iconPaths: Record<IconName, string> = {
  dashboard: "M4 4h6v6H4V4Zm10 0h6v6h-6V4ZM4 14h6v6H4v-6Zm10 0h6v6h-6v-6Z",
  land: "M3 6.5 9 4l6 2.5L21 4v13.5L15 20l-6-2.5L3 20V6.5Zm6-2.5v13.5M15 6.5V20",
  sprout: "M12 21v-9m0 2c-4 0-7-2-7-6 4 0 7 2 7 6Zm0-3c4 0 7-2 7-6-4 0-7 2-7 6Z",
  harvest: "M5 20h14M7 20V9m4 11V5m4 15V8m4 12V4M5 9c2 0 4 1 6 3m0-7c2 0 3 1 4 3m0 0c2-1 3-2 4-4",
  sales: "M4 7h16v12H4V7Zm3 0V5h10v2m-9 5h8m-8 3h5",
  finance: "M4 19V10m6 9V5m6 14v-6m4 6H2",
  share: "M8 12a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm8 6a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm-5.5-7.5 3 3",
  team: "M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM2 21c0-4 3-7 7-7s7 3 7 7m8-10a3 3 0 1 0 0-6m0 9c3 0 5 2 5 5",
  menu: "M4 7h16M4 12h16M4 17h16",
  close: "m6 6 12 12M18 6 6 18",
  collapse: "m14 7-5 5 5 5",
  expand: "m10 7 5 5-5 5",
  sun: "M12 4V2m0 20v-2m8-8h2M2 12h2m13.7-5.7 1.4-1.4M4.9 19.1l1.4-1.4m11.4 0 1.4 1.4M4.9 4.9l1.4 1.4M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z",
  moon: "M20 15.2A8 8 0 0 1 8.8 4 8 8 0 1 0 20 15.2Z",
  chevron: "m8 10 4 4 4-4",
  logout: "M10 17l5-5-5-5m5 5H3m11-8h5a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-5",
  shield: "M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Zm-3-10 2 2 4-4",
  check: "m5 12 4 4L19 6",
  trend: "m4 17 5-5 4 4 7-8m-5 0h5v5",
  wallet: "M4 6h14a2 2 0 0 1 2 2v11H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h12m4 7h-5a2 2 0 0 0 0 4h5",
  history: "M3 12a9 9 0 1 0 3-6.7L3 8m0-5v5h5m4-1v6l4 2",
  catalog: "M4 4h6v6H4V4Zm10 0h6v6h-6V4ZM4 14h6v6H4v-6Zm10 0h6v6h-6v-6Z",
};

function AppIcon({ name }: { name: IconName }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d={iconPaths[name]} />
    </svg>
  );
}

export function DashboardShell() {
  const router = useRouter();
  const pathname = usePathname();
  const [state, setState] = useState<DashboardState | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSigningOut, setIsSigningOut] = useState(false);
  const [isMobileNavigationOpen, setIsMobileNavigationOpen] = useState(false);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [theme, setTheme] = useState<ThemePreference>("light");

  useEffect(() => {
    let cancelled = false;

    async function loadDashboard() {
      try {
        const user = await getCurrentUser();
        const membership = resolveSelectedMembership(user.memberships);
        const organization = membership
          ? await getOrganization(membership.organizationId)
          : null;

        if (!cancelled) {
          setState({ user, membership, organization });
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          router.replace("/login");
          return;
        }

        if (!cancelled) {
          setErrorMessage(error instanceof Error ? error.message : "Dashboard gagal dimuat.");
        }
      }
    }

    void loadDashboard();

    return () => {
      cancelled = true;
    };
  }, [router]);

  useEffect(() => {
    const animationFrame = window.requestAnimationFrame(() => {
      setTheme(readThemePreference());
      setIsSidebarCollapsed(readSidebarCollapsed());
    });

    return () => window.cancelAnimationFrame(animationFrame);
  }, []);

  useEffect(() => {
    function closeTransientNavigation(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setIsMobileNavigationOpen(false);
        setIsProfileOpen(false);
      }
    }

    window.addEventListener("keydown", closeTransientNavigation);
    return () => window.removeEventListener("keydown", closeTransientNavigation);
  }, []);

  useEffect(() => {
    if (!isMobileNavigationOpen) {
      return;
    }

    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = originalOverflow;
    };
  }, [isMobileNavigationOpen]);

  const visibleNavigation = useMemo(() => {
    if (!state?.membership) {
      return navigation.slice(0, 1);
    }

    return navigation.filter(
      (item) => !item.permission || hasPermission(state.membership!, item.permission),
    );
  }, [state]);

  async function changeOrganization(organizationId: string) {
    if (!state) {
      return;
    }

    const membership = state.user.memberships.find(
      (item) => item.organizationId === organizationId,
    );

    if (!membership) {
      return;
    }

    setSelectedOrganizationId(organizationId);
    setErrorMessage(null);
    setIsProfileOpen(false);

    try {
      const organization = await getOrganization(organizationId);
      setState((current) => current
        ? { ...current, membership, organization }
        : current);
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Organisasi gagal dimuat.");
    }
  }

  async function handleLogout() {
    setIsSigningOut(true);
    setErrorMessage(null);

    try {
      await logout();
      setSelectedOrganizationId(null);
      router.replace("/login");
      router.refresh();
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Logout gagal.");
      setIsSigningOut(false);
    }
  }

  function toggleTheme() {
    const nextTheme: ThemePreference = theme === "dark" ? "light" : "dark";
    setTheme(nextTheme);
    setThemePreference(nextTheme);
  }

  function toggleSidebar() {
    const nextValue = !isSidebarCollapsed;
    setIsSidebarCollapsed(nextValue);
    setSidebarCollapsed(nextValue);
  }

  if (!state) {
    return (
      <main className="gate">
        <div className="gate__card">
          {errorMessage
            ? <div className="alert alert--error">{errorMessage}</div>
            : <><span className="loader" /><p>Memuat ruang kerja...</p></>}
        </div>
      </main>
    );
  }

  const firstName = state.user.email.split("@")[0].replace(/[._-]+/g, " ");
  const roleLabel = state.membership
    ? getRoleLabel(state.membership.role)
    : "Tanpa organisasi";
  const sidebarClassName = [
    styles.sidebar,
    isSidebarCollapsed ? styles.sidebarCollapsed : "",
    isMobileNavigationOpen ? styles.sidebarOpen : "",
  ].filter(Boolean).join(" ");

  return (
    <div
      className={`${styles.shell} ${isSidebarCollapsed ? styles.shellCollapsed : ""}`}
      data-theme={theme}
    >
      <button
        className={`${styles.drawerBackdrop} ${isMobileNavigationOpen ? styles.drawerBackdropVisible : ""}`}
        type="button"
        aria-label="Tutup navigasi"
        tabIndex={isMobileNavigationOpen ? 0 : -1}
        onClick={() => setIsMobileNavigationOpen(false)}
      />

      <aside className={sidebarClassName} aria-label="Navigasi aplikasi">
        <div className={styles.sidebarHeader}>
          <div className={styles.brandHolder}>
            <BrandMark compact={isSidebarCollapsed && !isMobileNavigationOpen} />
          </div>
          <button
            className={styles.mobileCloseButton}
            type="button"
            aria-label="Tutup navigasi"
            onClick={() => setIsMobileNavigationOpen(false)}
          >
            <AppIcon name="close" />
          </button>
        </div>

        <div className={styles.navigationLabel}>Menu utama</div>
        <nav className={styles.navigation}>
          {visibleNavigation.map((item) => {
            const isActive = item.path === "/cultivation"
              ? pathname.startsWith("/cultivation")
              : item.path === "/master-data/commodities"
                ? pathname.startsWith("/master-data")
                : item.path === "/finance"
                  ? pathname.startsWith("/finance")
                  : item.path === "/profit-sharing"
                    ? pathname.startsWith("/profit-sharing")
                    : item.path === "/evaluations/season-history"
                      ? pathname.startsWith("/evaluations")
                      : item.path === pathname;
            const isAvailable = item.path !== null;

            return (
              <button
                key={item.label}
                className={`${styles.navigationItem} ${isActive ? styles.navigationItemActive : ""}`}
                type="button"
                disabled={!isAvailable}
                aria-current={isActive ? "page" : undefined}
                aria-label={`${item.label}. ${item.caption}`}
                title={`${item.label} - ${item.caption}`}
                onClick={() => {
                  setIsMobileNavigationOpen(false);
                  if (item.path && !isActive) {
                    router.push(item.path);
                  }
                }}
              >
                <span className={styles.navigationIcon}><AppIcon name={item.icon} /></span>
                <span className={styles.navigationCopy}>
                  <strong>{item.label}</strong>
                  <small>{item.caption}</small>
                </span>
                {!isAvailable && <span className={styles.soonBadge}>Segera</span>}
              </button>
            );
          })}
        </nav>

        <div className={styles.sidebarStatus}>
          <span className={styles.connectionDot} />
          <span>
            <strong>API terhubung</strong>
            <small>Cookie &amp; CSRF aman</small>
          </span>
        </div>

        <button
          className={styles.collapseButton}
          type="button"
          aria-label={isSidebarCollapsed ? "Perlebar sidebar" : "Perkecil sidebar"}
          onClick={toggleSidebar}
        >
          <AppIcon name={isSidebarCollapsed ? "expand" : "collapse"} />
        </button>
      </aside>

      <div className={styles.workspace}>
        <header className={styles.topbar}>
          <div className={styles.topbarStart}>
            <button
              className={styles.mobileMenuButton}
              type="button"
              aria-label="Buka navigasi"
              aria-expanded={isMobileNavigationOpen}
              onClick={() => setIsMobileNavigationOpen(true)}
            >
              <AppIcon name="menu" />
            </button>

            <div className={styles.organizationSwitcher}>
              <span>Organisasi aktif</span>
              <select
                value={state.membership?.organizationId ?? ""}
                onChange={(event) => void changeOrganization(event.target.value)}
                disabled={state.user.memberships.length === 0}
                aria-label="Pilih organisasi aktif"
              >
                {state.user.memberships.map((membership) => (
                  <option value={membership.organizationId} key={membership.membershipId}>
                    {membership.organizationId === state.organization?.id
                      ? state.organization.name
                      : `Organisasi ${membership.organizationId.slice(0, 8)}`}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className={styles.topbarActions}>
            <span className={styles.roleBadge}>
              <AppIcon name="shield" />
              {roleLabel}
            </span>
            <button
              className={styles.iconButton}
              type="button"
              aria-label={theme === "dark" ? "Gunakan tema terang" : "Gunakan tema gelap"}
              onClick={toggleTheme}
            >
              <AppIcon name={theme === "dark" ? "sun" : "moon"} />
            </button>

            <div className={styles.profileArea}>
              <button
                className={styles.profileButton}
                type="button"
                aria-expanded={isProfileOpen}
                onClick={() => setIsProfileOpen((current) => !current)}
              >
                <span className={styles.avatar}>{state.user.email.slice(0, 1).toUpperCase()}</span>
                <span className={styles.profileCopy}>
                  <strong>{firstName}</strong>
                  <small>{state.user.email}</small>
                </span>
                <span className={styles.profileChevron}><AppIcon name="chevron" /></span>
              </button>

              {isProfileOpen && (
                <div className={styles.profileMenu}>
                  <div className={styles.profileMenuIdentity}>
                    <span className={styles.avatar}>{state.user.email.slice(0, 1).toUpperCase()}</span>
                    <span>
                      <strong>{firstName}</strong>
                      <small>{state.user.email}</small>
                    </span>
                  </div>
                  <div className={styles.profileMenuRole}>
                    <AppIcon name="shield" />
                    <span><small>Peran aktif</small><strong>{roleLabel}</strong></span>
                  </div>
                  <button
                    className={styles.logoutButton}
                    type="button"
                    disabled={isSigningOut}
                    onClick={() => void handleLogout()}
                  >
                    <AppIcon name="logout" />
                    {isSigningOut ? "Keluar..." : "Keluar dari SiPacul"}
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>

        <main className={styles.content}>
          {errorMessage && <div className={styles.errorAlert} role="alert">{errorMessage}</div>}
          {pathname === "/master-data/commodities" ? (
            <CommodityManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/lands" ? (
            <LandManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/harvest" ? (
            <HarvestManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/sales" ? (
            <SaleManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/finance/expenses" ? (
            <ExpenseManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/finance" ? (
            <ReceivableManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/profit-sharing" ? (
            <ProfitSharingManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/evaluations/season-history" ? (
            <SeasonHistoryManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/cultivation/activities" ? (
            <CultivationActivityManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : pathname === "/cultivation" ? (
            <CropCycleManagement
              key={state.membership?.organizationId ?? "no-organization"}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          ) : (
            <DashboardOverview
              key={state.membership?.organizationId ?? "no-organization"}
              firstName={firstName}
              organization={state.organization}
              organizationId={state.membership?.organizationId ?? null}
              permissions={state.membership?.permissions ?? []}
            />
          )}
        </main>
      </div>
    </div>
  );
}
