import type { Metadata } from "next";
import { SetupForm } from "@/components/setup-form";

export const metadata: Metadata = {
  title: "Setup awal",
};

export default function SetupPage() {
  return <SetupForm />;
}