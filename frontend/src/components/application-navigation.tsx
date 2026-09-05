"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { BrandMark } from "@/components/brand-mark";
import {
  allApplicationNavigationItems,
  applicationNavigationGroups,
  dashboardNavigationItem,
  filterNavigationGroups,
  flattenNavigationGroups,
  getNavigationItemForPath,
  readCollapsedNavigationGroups,
  readRecentNavigationPaths,
  recordRecentNavigationPath,
  searchNavigationItems,
  writeCollapsedNavigationGroups,
  writeRecentNavigationPaths,
  type ApplicationNavigationItem,
  type NavigationIconName,
} from "@/lib/navigation/application-navigation";
import styles from "./application-navigation.module.css";

type Props = {
  pathname: string;
  permissions: string[];
  hasMembership: boolean;
  isSidebarCollapsed: boolean;
  isMobileNavigationOpen: boolean;
  onCloseMobileNavigation: () => void;
  onToggleSidebar: () => void;
};

type UtilityIconName =
  | "search"
  | "close"
  | "collapse"
  | "expand"
  | "chevron"
  | "recent";

type IconName = NavigationIconName | UtilityIconName;

const iconPaths: Record<IconName, string> = {
  dashboard: "M4 4h6v6H4V4Zm10 0h6v6h-6V4ZM4 14h6v6H4v-6Zm10 0h6v6h-6v-6Z",
  land: "M3 6.5 9 4l6 2.5L21 4v13.5L15 20l-6-2.5L3 20V6.5Zm6-2.5v13.5M15 6.5V20",
  sprout: "M12 21v-9m0 2c-4 0-7-2-7-6 4 0 7 2 7 6Zm0-3c4 0 7-2 7-6-4 0-7 2-7 6Z",
  harvest: "M5 20h14M7 20V9m4 11V5m4 15V8m4 12V4M5 9c2 0 4 1 6 3m0-7c2 0 3 1 4 3m0 0c2-1 3-2 4-4",
  sales: "M4 7h16v12H4V7Zm3 0V5h10v2m-9 5h8m-8 3h5",
  finance: "M4 19V10m6 9V5m6 14v-6m4 6H2",
  share: "M8 12a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm8 6a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm-5.5-7.5 3 3",
  history: "M3 12a9 9 0 1 0 3-6.7L3 8m0-5v5h5m4-1v6l4 2",
  catalog: "M4 4h6v6H4V4Zm10 0h6v6h-6V4ZM4 14h6v6H4v-6Zm10 0h6v6h-6v-6Z",
  team: "M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM2 21c0-4 3-7 7-7s7 3 7 7m8-10a3 3 0 1 0 0-6m0 9c3 0 5 2 5 5",
  search: "m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
  close: "m6 6 12 12M18 6 6 18",
  collapse: "m14 7-5 5 5 5",
  expand: "m10 7 5 5-5 5",
  chevron: "m8 10 4 4 4-4",
  recent: "M3 12a9 9 0 1 0 3-6.7L3 8m0-5v5h5m4-1v6l4 2",
};

const groupIds = applicationNavigationGroups.map((group) => group.id);
const allPaths = allApplicationNavigationItems.map((item) => item.path);

function AppIcon({ name }: { name: IconName }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d={iconPaths[name]} />
    </svg>
  );
}

function NavigationButton({
  item,
  isActive,
  onOpen,
}: {
  item: ApplicationNavigationItem;
  isActive: boolean;
  onOpen: (item: ApplicationNavigationItem) => void;
}) {
  return (
    <button
      className={`${styles.navigationItem} ${isActive ? styles.navigationItemActive : ""}`}
      type="button"
      aria-current={isActive ? "page" : undefined}
      aria-label={`${item.label}. ${item.caption}`}
      title={`${item.label} - ${item.caption}`}
      onClick={() => onOpen(item)}
    >
      <span className={styles.navigationIcon}>
        <AppIcon name={item.icon} />
      </span>
      <span className={styles.navigationCopy}>
        <strong>{item.label}</strong>
        <small>{item.caption}</small>
      </span>
    </button>
  );
}

export function ApplicationNavigation({
  pathname,
  permissions,
  hasMembership,
  isSidebarCollapsed,
  isMobileNavigationOpen,
  onCloseMobileNavigation,
  onToggleSidebar,
}: Props) {
  const router = useRouter();
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [collapsedGroups, setCollapsedGroups] = useState<string[]>([]);
  const [recentPaths, setRecentPaths] = useState<string[]>([]);
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");

  const visibleGroups = useMemo(
    () => hasMembership
      ? filterNavigationGroups(applicationNavigationGroups, permissions)
      : [],
    [hasMembership, permissions],
  );

  const visibleItems = useMemo(
    () => [dashboardNavigationItem, ...flattenNavigationGroups(visibleGroups)],
    [visibleGroups],
  );

  const activeItem = useMemo(
    () => getNavigationItemForPath(pathname, visibleItems),
    [pathname, visibleItems],
  );
  const activeGroupId = activeItem?.groupId ?? null;

  const searchResults = useMemo(
    () => searchNavigationItems(visibleItems, searchQuery).slice(0, 9),
    [searchQuery, visibleItems],
  );

  const recentItems = useMemo(
    () => recentPaths
      .map((path) => visibleItems.find((item) => item.path === path) ?? null)
      .filter((item): item is ApplicationNavigationItem => item !== null)
      .slice(0, 5),
    [recentPaths, visibleItems],
  );

  useEffect(() => {
    const animationFrame = window.requestAnimationFrame(() => {
      setCollapsedGroups(readCollapsedNavigationGroups(groupIds));
      setRecentPaths(readRecentNavigationPaths(allPaths));
    });

    return () => window.cancelAnimationFrame(animationFrame);
  }, []);

  useEffect(() => {
    const currentItem = getNavigationItemForPath(pathname, visibleItems);
    if (!currentItem) {
      return;
    }

    const animationFrame = window.requestAnimationFrame(() => {
      setRecentPaths((current) => {
        const next = recordRecentNavigationPath(
          current,
          currentItem.path,
          allPaths,
        );
        writeRecentNavigationPaths(next);
        return next;
      });
    });

    return () => window.cancelAnimationFrame(animationFrame);
  }, [pathname, visibleItems]);

  const openSearch = useCallback(() => {
    onCloseMobileNavigation();
    setSearchQuery("");
    setIsSearchOpen(true);
  }, [onCloseMobileNavigation]);

  const closeSearch = useCallback(() => {
    setIsSearchOpen(false);
    setSearchQuery("");
  }, []);

  useEffect(() => {
    function handleShortcut(event: KeyboardEvent) {
      if ((event.ctrlKey || event.metaKey) && event.key.toLocaleLowerCase() === "k") {
        event.preventDefault();
        openSearch();
        return;
      }

      if (event.key === "Escape" && isSearchOpen) {
        event.preventDefault();
        closeSearch();
      }
    }

    window.addEventListener("keydown", handleShortcut);
    return () => window.removeEventListener("keydown", handleShortcut);
  }, [closeSearch, isSearchOpen, openSearch]);

  useEffect(() => {
    if (!isSearchOpen) {
      return;
    }

    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const animationFrame = window.requestAnimationFrame(() => {
      searchInputRef.current?.focus();
    });

    return () => {
      window.cancelAnimationFrame(animationFrame);
      document.body.style.overflow = originalOverflow;
    };
  }, [isSearchOpen]);

  function openNavigationItem(item: ApplicationNavigationItem) {
    onCloseMobileNavigation();
    closeSearch();

    if (item.path !== pathname) {
      router.push(item.path);
    }
  }

  function toggleGroup(groupId: string) {
    if (activeGroupId === groupId) {
      return;
    }

    setCollapsedGroups((current) => {
      const next = current.includes(groupId)
        ? current.filter((item) => item !== groupId)
        : [...current, groupId];

      writeCollapsedNavigationGroups(next);
      return next;
    });
  }

  const sidebarClassName = [
    styles.sidebar,
    isSidebarCollapsed ? styles.sidebarCollapsed : "",
    isMobileNavigationOpen ? styles.sidebarOpen : "",
  ].filter(Boolean).join(" ");

  const hasSearchQuery = searchQuery.trim().length > 0;
  const commandItems = hasSearchQuery ? searchResults : recentItems;

  return (
    <>
      <button
        className={`${styles.drawerBackdrop} ${isMobileNavigationOpen ? styles.drawerBackdropVisible : ""}`}
        type="button"
        aria-label="Tutup navigasi"
        tabIndex={isMobileNavigationOpen ? 0 : -1}
        onClick={onCloseMobileNavigation}
      />

      {isSearchOpen && (
        <div className={styles.commandLayer}>
          <button
            className={styles.commandBackdrop}
            type="button"
            aria-label="Tutup pencarian halaman"
            onClick={closeSearch}
          />
          <section
            className={styles.commandPanel}
            role="dialog"
            aria-modal="true"
            aria-labelledby="navigation-search-title"
          >
            <div className={styles.commandHeader}>
              <div className={styles.commandTitle}>
                <span className={styles.commandTitleIcon}><AppIcon name="search" /></span>
                <span>
                  <strong id="navigation-search-title">Cari halaman atau fungsi</strong>
                  <small>Temukan workspace SiPacul tanpa mengingat letak menunya.</small>
                </span>
              </div>
              <button
                className={styles.commandClose}
                type="button"
                aria-label="Tutup pencarian"
                onClick={closeSearch}
              >
                <AppIcon name="close" />
              </button>
            </div>

            <label className={styles.commandSearch}>
              <AppIcon name="search" />
              <input
                ref={searchInputRef}
                type="search"
                value={searchQuery}
                placeholder="Contoh: pupuk, piutang, aktivitas, komoditas..."
                aria-label="Cari halaman atau fungsi SiPacul"
                onChange={(event) => setSearchQuery(event.target.value)}
              />
              <kbd>Esc</kbd>
            </label>

            <div className={styles.commandResults}>
              <div className={styles.commandSectionLabel}>
                <span>{hasSearchQuery ? "Hasil pencarian" : "Terakhir dibuka"}</span>
                <small>
                  {hasSearchQuery
                    ? `${commandItems.length} halaman`
                    : "Maksimal 5 halaman"}
                </small>
              </div>

              {commandItems.length > 0 ? (
                <div className={styles.commandResultList}>
                  {commandItems.map((item) => (
                    <button
                      className={styles.commandResult}
                      type="button"
                      key={item.id}
                      onClick={() => openNavigationItem(item)}
                    >
                      <span className={styles.commandResultIcon}>
                        <AppIcon name={item.icon} />
                      </span>
                      <span className={styles.commandResultCopy}>
                        <strong>{item.label}</strong>
                        <small>{item.caption}</small>
                      </span>
                      <span className={styles.commandResultMeta}>
                        {item.groupLabel ?? "Utama"}
                      </span>
                    </button>
                  ))}
                </div>
              ) : (
                <div className={styles.commandEmpty}>
                  <AppIcon name={hasSearchQuery ? "search" : "recent"} />
                  <strong>
                    {hasSearchQuery
                      ? "Halaman tidak ditemukan"
                      : "Belum ada halaman terakhir"}
                  </strong>
                  <small>
                    {hasSearchQuery
                      ? "Coba istilah fungsi lain, misalnya pupuk, pembayaran, atau panen."
                      : "Buka beberapa halaman SiPacul. Riwayat navigasi akan muncul di sini."}
                  </small>
                </div>
              )}
            </div>

            <footer className={styles.commandFooter}>
              <span><kbd>Ctrl</kbd><kbd>K</kbd> buka pencarian</span>
              <span>Pencarian hanya menampilkan halaman yang boleh Anda akses.</span>
            </footer>
          </section>
        </div>
      )}

      <aside className={sidebarClassName} aria-label="Navigasi aplikasi">
        <div className={styles.sidebarHeader}>
          <div className={styles.brandHolder}>
            <BrandMark compact={isSidebarCollapsed && !isMobileNavigationOpen} />
          </div>
          <button
            className={styles.mobileCloseButton}
            type="button"
            aria-label="Tutup navigasi"
            onClick={onCloseMobileNavigation}
          >
            <AppIcon name="close" />
          </button>
        </div>

        <button
          className={styles.navigationSearchButton}
          type="button"
          aria-label="Cari halaman atau fungsi. Pintasan Ctrl K."
          title="Cari halaman atau fungsi - Ctrl+K"
          onClick={openSearch}
        >
          <span className={styles.navigationSearchIcon}><AppIcon name="search" /></span>
          <span className={styles.navigationSearchCopy}>Cari halaman atau fungsi</span>
          <kbd>Ctrl K</kbd>
        </button>

        <nav className={styles.navigation} aria-label="Halaman SiPacul">
          <div className={styles.navigationHome}>
            <NavigationButton
              item={dashboardNavigationItem}
              isActive={activeItem?.id === dashboardNavigationItem.id}
              onOpen={openNavigationItem}
            />
          </div>

          <div className={styles.navigationGroups}>
            {visibleGroups.map((group) => {
              const isExpanded = (isSidebarCollapsed && !isMobileNavigationOpen)
                || activeGroupId === group.id
                || !collapsedGroups.includes(group.id);

              return (
                <section className={styles.navigationGroup} key={group.id}>
                  <button
                    className={styles.navigationGroupToggle}
                    type="button"
                    aria-expanded={isExpanded}
                    aria-controls={`navigation-group-${group.id}`}
                    onClick={() => toggleGroup(group.id)}
                  >
                    <span>{group.label}</span>
                    <span
                      className={`${styles.navigationGroupChevron} ${isExpanded ? styles.navigationGroupChevronOpen : ""}`}
                    >
                      <AppIcon name="chevron" />
                    </span>
                  </button>

                  <div
                    className={`${styles.navigationGroupItems} ${isExpanded ? "" : styles.navigationGroupItemsCollapsed}`}
                    id={`navigation-group-${group.id}`}
                  >
                    {group.items.map((item) => (
                      <NavigationButton
                        item={item}
                        isActive={activeItem?.id === item.id}
                        onOpen={openNavigationItem}
                        key={item.id}
                      />
                    ))}
                  </div>
                </section>
              );
            })}
          </div>
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
          onClick={onToggleSidebar}
        >
          <AppIcon name={isSidebarCollapsed ? "expand" : "collapse"} />
        </button>
      </aside>
    </>
  );
}
