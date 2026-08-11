import type { Metadata } from "next";
import { DashboardShell } from "@/components/dashboard-shell";

export const metadata: Metadata = {
  title: "Pengeluaran & Biaya Budidaya",
};

export default function CultivationExpensesPage() {
  return <DashboardShell />;
}
