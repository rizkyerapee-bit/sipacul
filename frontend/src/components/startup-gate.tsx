"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, getBootstrapStatus, getCurrentUser } from "@/lib/api/client";

export function StartupGate() {
  const router = useRouter();
  const [message, setMessage] = useState("Memeriksa kesiapan SiPacul...");

  useEffect(() => {
    let cancelled = false;

    async function resolveDestination() {
      try {
        const status = await getBootstrapStatus();

        if (cancelled) return;

        if (status.canBootstrap) {
          router.replace("/setup");
          return;
        }

        if (!status.isInitialized) {
          setMessage(
            status.isConfigured
              ? "Setup awal belum dapat dijalankan. Periksa konfigurasi backend."
              : "Token bootstrap belum dikonfigurasi pada backend.",
          );
          return;
        }

        try {
          await getCurrentUser();
          if (!cancelled) router.replace("/dashboard");
        } catch (error) {
          if (error instanceof ApiError && error.status === 401) {
            if (!cancelled) router.replace("/login");
            return;
          }

          throw error;
        }
      } catch (error) {
        if (!cancelled) {
          setMessage(
            error instanceof Error
              ? error.message
              : "Frontend tidak dapat menghubungi API SiPacul.",
          );
        }
      }
    }

    void resolveDestination();

    return () => {
      cancelled = true;
    };
  }, [router]);

  return (
    <main className="gate">
      <div className="gate__card">
        <span className="loader" aria-hidden="true" />
        <p>{message}</p>
      </div>
    </main>
  );
}