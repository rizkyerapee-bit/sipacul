import type { Metadata } from "next";
import { DashboardShell } from "@/components/dashboard-shell";

export const metadata: Metadata = {
  title: "Lahan dan Petak",
};

export default function LandsPage() {
  return <DashboardShell />;
}
