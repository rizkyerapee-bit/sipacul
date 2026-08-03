"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, getBootstrapStatus, getCurrentUser, login } from "@/lib/api/client";
import { BrandMark } from "@/components/brand-mark";

export function LoginForm() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function checkRoute() {
      try {
        const status = await getBootstrapStatus();
        if (!cancelled && status.canBootstrap) {
          router.replace("/setup");
          return;
        }

        await getCurrentUser();
        if (!cancelled) router.replace("/dashboard");
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) return;
        if (!cancelled && error instanceof Error) {
          setErrorMessage(error.message);
        }
      }
    }

    void checkRoute();

    return () => {
      cancelled = true;
    };
  }, [router]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await login({ email, password, rememberMe });
      router.replace("/dashboard");
      router.refresh();
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "Login gagal. Silakan coba kembali.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-layout">
      <section className="auth-story" aria-label="Tentang SiPacul">
        <BrandMark />
        <div className="auth-story__content">
          <span className="eyebrow eyebrow--light">Operasional agribisnis dalam satu alur</span>
          <h1>Catat musim tanam. Jaga biaya. Tumbuhkan hasil.</h1>
          <p>
            Dari lahan hingga pembagian keuntungan, keputusan usaha tersusun
            rapi dan dapat ditelusuri kembali.
          </p>
        </div>
        <div className="auth-story__metrics" aria-label="Cakupan SiPacul">
          <span><strong>01</strong> Budidaya</span>
          <span><strong>02</strong> Keuangan</span>
          <span><strong>03</strong> Evaluasi</span>
        </div>
      </section>

      <section className="auth-panel">
        <div className="auth-card">
          <div className="auth-card__header">
            <span className="eyebrow">Selamat datang kembali</span>
            <h2>Masuk ke SiPacul</h2>
            <p>Gunakan akun yang terdaftar pada organisasi Anda.</p>
          </div>

          <form onSubmit={handleSubmit} className="form-stack">
            <label className="field">
              <span>Email</span>
              <input
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="nama@perusahaan.id"
                maxLength={256}
                required
              />
            </label>

            <label className="field">
              <span>Password</span>
              <input
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder="Masukkan password"
                maxLength={1024}
                required
              />
            </label>

            <label className="check-row">
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(event) => setRememberMe(event.target.checked)}
              />
              <span>Pertahankan sesi pada perangkat ini</span>
            </label>

            {errorMessage && (
              <div className="alert alert--error" role="alert">
                {errorMessage}
              </div>
            )}

            <button className="button button--primary" disabled={isSubmitting}>
              {isSubmitting ? "Memproses..." : "Masuk ke dashboard"}
            </button>
          </form>

          <p className="auth-card__footnote">
            Sesi dilindungi cookie terenkripsi dan validasi CSRF dari backend.
          </p>
        </div>
      </section>
    </main>
  );
}