import type { Metadata } from "next";
import { DashboardShell } from "@/components/dashboard-shell";

export const metadata: Metadata = {
  title: "Master Komoditas",
};

export default function MasterCommodityPage() {
  return <DashboardShell />;
}
