import type { Metadata } from "next";
import { DashboardShell } from "@/components/dashboard-shell";

export const metadata: Metadata = {
  title: "Siklus Budidaya",
};

export default function CultivationPage() {
  return <DashboardShell />;
}
