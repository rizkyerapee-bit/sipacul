import type { Metadata } from "next";
import { DashboardShell } from "@/components/dashboard-shell";

export const metadata: Metadata = {
  title: "Anggota & Akses",
};

export default function OrganizationMembersPage() {
  return <DashboardShell />;
}
