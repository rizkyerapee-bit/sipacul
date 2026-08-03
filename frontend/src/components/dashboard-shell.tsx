"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { BrandMark } from "@/components/brand-mark";
import { ApiError, getCurrentUser, getOrganization, logout } from "@/lib/api/client";
import type { CurrentUser, CurrentUserMembership, Organization } from "@/lib/api/contracts";
import { getRoleLabel, hasPermission, resolveSelectedMembership, setSelectedOrganizationId } from "@/lib/session/organization-selection";

type DashboardState = {
  user: CurrentUser;
  membership: CurrentUserMembership | null;
  organization: Organization | null;
};

const navigation = [
  { label: "Ringkasan", caption: "Kondisi usaha hari ini", permission: null, icon: "grid" },
  { label: "Lahan", caption: "Lahan dan petak", permission: "lands.read", icon: "land" },
  { label: "Budidaya", caption: "Siklus dan aktivitas", permission: "cultivation.read", icon: "sprout" },
  { label: "Panen", caption: "Hasil dan kualitas", permission: "harvest.read", icon: "harvest" },
  { label: "Penjualan", caption: "Transaksi dan piutang", permission: "sales.read", icon: "sales" },
  { label: "Keuangan", caption: "Biaya, modal, dan laba", permission: "finance.read", icon: "finance" },
  { label: "Bagi hasil", caption: "Alokasi investor dan mitra", permission: "profit-sharing.read", icon: "share" },
  { label: "Tim", caption: "Anggota dan peran", permission: "members.read", icon: "team" },
];

function NavigationIcon({ name }: { name: string }) {
  const paths: Record<string, string> = {
    grid: "M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z",
    land: "M3 17l6-10 5 6 3-4 4 8M3 20h18",
    sprout: "M12 21v-9M12 14c-4 0-7-2-7-6 4 0 7 2 7 6ZM12 11c4 0 7-2 7-6-4 0-7 2-7 6Z",
    harvest: "M6 20V9M10 20V5M14 20V8M18 20V4M4 20h16",
    sales: "M4 7h16v12H4zM7 7V5h10v2M8 13h8",
    finance: "M4 19V9M10 19V5M16 19v-7M22 19H2",
    share: "M8 12a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm8 6a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM10.5 10.5l3 3",
    team: "M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM2 21c0-4 3-7 7-7s7 3 7 7M17 11a3 3 0 1 0 0-6M17 14c3 0 5 2 5 5",
  };

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d={paths[name]} />
    </svg>
  );
}

export function DashboardShell() {
  const router = useRouter();
  const [state, setState] = useState<DashboardState | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSigningOut, setIsSigningOut] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function loadDashboard() {
      try {
        const user = await getCurrentUser();
        const membership = resolveSelectedMembership(user.memberships);
        const organization = membership
          ? await getOrganization(membership.organizationId)
          : null;

        if (!cancelled) setState({ user, membership, organization });
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

  const visibleNavigation = useMemo(() => {
    if (!state?.membership) return navigation.slice(0, 1);
    return navigation.filter((item) => !item.permission || hasPermission(state.membership!, item.permission));
  }, [state]);

  async function changeOrganization(organizationId: string) {
    if (!state) return;

    const membership = state.user.memberships.find((item) => item.organizationId === organizationId);
    if (!membership) return;

    setSelectedOrganizationId(organizationId);
    setErrorMessage(null);

    try {
      const organization = await getOrganization(organizationId);
      setState({ ...state, membership, organization });
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

  if (!state) {
    return (
      <main className="gate">
        <div className="gate__card">
          {errorMessage ? <div className="alert alert--error">{errorMessage}</div> : <><span className="loader" /><p>Memuat ruang kerja...</p></>}
        </div>
      </main>
    );
  }

  const firstName = state.user.email.split("@")[0];

  return (
    <div className="dashboard-layout">
      <aside className="sidebar">
        <BrandMark />
        <nav className="sidebar__nav" aria-label="Navigasi utama">
          {visibleNavigation.map((item, index) => (
            <button key={item.label} className={`nav-item ${index === 0 ? "nav-item--active" : ""}`} type="button">
              <span className="nav-item__icon"><NavigationIcon name={item.icon} /></span>
              <span><strong>{item.label}</strong><small>{item.caption}</small></span>
            </button>
          ))}
        </nav>
        <div className="sidebar__footer">
          <span className="connection-dot" /> API terhubung melalui proxy aman
        </div>
      </aside>

      <main className="dashboard-main">
        <header className="topbar">
          <div className="organization-switcher">
            <span>Organisasi aktif</span>
            <select value={state.membership?.organizationId ?? ""} onChange={(event) => void changeOrganization(event.target.value)} disabled={state.user.memberships.length === 0}>
              {state.user.memberships.map((membership) => (
                <option value={membership.organizationId} key={membership.membershipId}>
                  {membership.organizationId === state.organization?.id ? state.organization.name : membership.organizationId}
                </option>
              ))}
            </select>
          </div>
          <div className="user-menu">
            <span className="avatar">{state.user.email.slice(0, 1).toUpperCase()}</span>
            <span className="user-menu__copy"><strong>{firstName}</strong><small>{state.membership ? getRoleLabel(state.membership.role) : "Tanpa organisasi"}</small></span>
            <button type="button" onClick={() => void handleLogout()} disabled={isSigningOut}>{isSigningOut ? "Keluar..." : "Keluar"}</button>
          </div>
        </header>

        <section className="dashboard-content">
          {errorMessage && <div className="alert alert--error" role="alert">{errorMessage}</div>}

          <div className="welcome-row">
            <div>
              <span className="eyebrow">Ruang kerja agribisnis</span>
              <h1>Selamat pagi, {firstName}.</h1>
              <p>{state.organization ? `Pantau kegiatan ${state.organization.name} dari satu tempat.` : "Akun ini belum memiliki membership organisasi aktif."}</p>
            </div>
            <div className="season-chip"><span>Musim berjalan</span><strong>Siap dicatat</strong></div>
          </div>

          <div className="metric-grid">
            <article className="metric-card metric-card--green"><span>Lahan aktif</span><strong>â€”</strong><small>Menunggu integrasi modul</small></article>
            <article className="metric-card"><span>Siklus berjalan</span><strong>â€”</strong><small>Data backend siap dihubungkan</small></article>
            <article className="metric-card"><span>Biaya musim ini</span><strong>Rp â€”</strong><small>Keuangan akan tampil di sini</small></article>
            <article className="metric-card"><span>Proyeksi hasil</span><strong>â€”</strong><small>Berbasis catatan lapangan</small></article>
          </div>

          <div className="dashboard-grid">
            <article className="panel panel--wide">
              <div className="panel__header"><div><span className="eyebrow">Alur musim</span><h2>Dari lahan hingga evaluasi</h2></div><span className="status-pill">Fondasi siap</span></div>
              <div className="process-track">
                {["Lahan", "SOP", "Budidaya", "Panen", "Penjualan", "Evaluasi"].map((step, index) => (
                  <div key={step} className={index === 0 ? "process-step process-step--active" : "process-step"}><span>{String(index + 1).padStart(2, "0")}</span><strong>{step}</strong></div>
                ))}
              </div>
            </article>

            <article className="panel">
              <div className="panel__header"><div><span className="eyebrow">Akses akun</span><h2>Membership aktif</h2></div></div>
              <dl className="detail-list">
                <div><dt>Organisasi</dt><dd>{state.organization?.name ?? "Belum tersedia"}</dd></div>
                <div><dt>Kode</dt><dd>{state.organization?.code ?? "â€”"}</dd></div>
                <div><dt>Peran</dt><dd>{state.membership ? getRoleLabel(state.membership.role) : "â€”"}</dd></div>
                <div><dt>Permission</dt><dd>{state.membership?.permissions.length ?? 0} izin</dd></div>
              </dl>
            </article>
          </div>
        </section>
      </main>
    </div>
  );
}