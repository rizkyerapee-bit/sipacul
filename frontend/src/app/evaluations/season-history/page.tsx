import type { Metadata } from "next";
import { DashboardShell } from "@/components/dashboard-shell";

export const metadata: Metadata = {
  title: "Histori Lahan & Evaluasi Musim",
};

export default function SeasonHistoryPage() {
  return <DashboardShell />;
}
