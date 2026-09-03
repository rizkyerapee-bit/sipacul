import type { Metadata } from "next";
import { DashboardShell } from "@/components/dashboard-shell";

export const metadata: Metadata = {
  title: "SOP Budidaya",
};

export default function CultivationSopPage() {
  return <DashboardShell />;
}
