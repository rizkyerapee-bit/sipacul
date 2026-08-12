import type { Metadata } from "next";
import { DashboardShell } from "@/components/dashboard-shell";

export const metadata: Metadata = {
  title: "Profitabilitas & Pembagian Hasil",
};

export default function ProfitSharingPage() {
  return <DashboardShell />;
}
