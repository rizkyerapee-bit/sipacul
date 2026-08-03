import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "SiPacul",
    template: "%s | SiPacul",
  },
  description: "Sistem Pencatatan Akuntansi Usaha Lestari",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="id">
      <body>{children}</body>
    </html>
  );
}