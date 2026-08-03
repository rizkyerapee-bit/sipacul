"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, bootstrapOwner, getBootstrapStatus } from "@/lib/api/client";
import { BrandMark } from "@/components/brand-mark";

type SetupFields = {
  organizationCode: string;
  organizationName: string;
  organizationLegalName: string;
  organizationTimeZone: string;
  email: string;
  password: string;
  bootstrapToken: string;
};

const initialFields: SetupFields = {
  organizationCode: "",
  organizationName: "",
  organizationLegalName: "",
  organizationTimeZone: "Asia/Jakarta",
  email: "",
  password: "",
  bootstrapToken: "",
};

export function SetupForm() {
  const router = useRouter();
  const [fields, setFields] = useState(initialFields);
  const [isReady, setIsReady] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

  useEffect(() => {
    let cancelled = false;

    async function checkStatus() {
      try {
        const status = await getBootstrapStatus();
        if (cancelled) return;

        if (status.isInitialized) {
          router.replace("/login");
          return;
        }

        if (!status.canBootstrap) {
          setErrorMessage(
            status.isConfigured
              ? "Setup awal belum tersedia. Periksa kondisi database."
              : "Token bootstrap belum dikonfigurasi pada backend.",
          );
          return;
        }

        setIsReady(true);
      } catch (error) {
        if (!cancelled) {
          setErrorMessage(
            error instanceof Error
              ? error.message
              : "Status setup tidak dapat diperiksa.",
          );
        }
      }
    }

    void checkStatus();

    return () => {
      cancelled = true;
    };
  }, [router]);

  function updateField<K extends keyof SetupFields>(key: K, value: SetupFields[K]) {
    setFields((current) => ({ ...current, [key]: value }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);
    setValidationErrors([]);
    setIsSubmitting(true);

    try {
      await bootstrapOwner(fields.bootstrapToken, {
        organizationCode: fields.organizationCode.trim(),
        organizationName: fields.organizationName.trim(),
        organizationLegalName: fields.organizationLegalName.trim() || null,
        organizationTimeZone: fields.organizationTimeZone.trim(),
        email: fields.email.trim(),
        password: fields.password,
      });

      setFields((current) => ({ ...current, bootstrapToken: "", password: "" }));
      router.replace("/login?setup=success");
    } catch (error) {
      if (error instanceof ApiError) {
        setValidationErrors(error.problem?.errors ?? []);
      }

      setErrorMessage(
        error instanceof Error
          ? error.message
          : "Setup Owner pertama gagal.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="setup-layout">
      <header className="setup-header">
        <BrandMark />
        <span className="status-pill">Setup satu kali</span>
      </header>

      <section className="setup-grid">
        <aside className="setup-intro">
          <span className="eyebrow">Langkah awal</span>
          <h1>Siapkan organisasi dan Owner pertama.</h1>
          <p>
            Data ini menjadi fondasi tenant pertama SiPacul. Akun Owner memiliki
            akses penuh dan dapat menambahkan anggota setelah login.
          </p>
          <ol className="step-list">
            <li><span>1</span><div><strong>Identitas usaha</strong><small>Kode, nama, dan zona waktu.</small></div></li>
            <li><span>2</span><div><strong>Akun Owner</strong><small>Email dan password pengelola utama.</small></div></li>
            <li><span>3</span><div><strong>Otorisasi awal</strong><small>Token dari konfigurasi backend.</small></div></li>
          </ol>
        </aside>

        <div className="setup-card">
          <form onSubmit={handleSubmit} className="form-stack">
            <fieldset disabled={!isReady || isSubmitting}>
              <legend>Organisasi</legend>
              <div className="form-grid form-grid--two">
                <label className="field">
                  <span>Kode organisasi</span>
                  <input value={fields.organizationCode} onChange={(event) => updateField("organizationCode", event.target.value)} placeholder="ERAPEE-FARM" maxLength={30} required />
                </label>
                <label className="field">
                  <span>Zona waktu</span>
                  <input value={fields.organizationTimeZone} onChange={(event) => updateField("organizationTimeZone", event.target.value)} placeholder="Asia/Jakarta" required />
                </label>
              </div>
              <label className="field">
                <span>Nama organisasi</span>
                <input value={fields.organizationName} onChange={(event) => updateField("organizationName", event.target.value)} placeholder="Nama usaha pertanian" required />
              </label>
              <label className="field">
                <span>Nama legal <small>(opsional)</small></span>
                <input value={fields.organizationLegalName} onChange={(event) => updateField("organizationLegalName", event.target.value)} placeholder="PT, CV, koperasi, atau kelompok tani" />
              </label>
            </fieldset>

            <fieldset disabled={!isReady || isSubmitting}>
              <legend>Owner pertama</legend>
              <label className="field">
                <span>Email</span>
                <input type="email" autoComplete="email" value={fields.email} onChange={(event) => updateField("email", event.target.value)} placeholder="owner@perusahaan.id" maxLength={256} required />
              </label>
              <label className="field">
                <span>Password</span>
                <input type="password" autoComplete="new-password" value={fields.password} onChange={(event) => updateField("password", event.target.value)} placeholder="Gunakan password yang kuat" maxLength={1024} required />
              </label>
              <label className="field">
                <span>Token bootstrap</span>
                <input type="password" autoComplete="off" value={fields.bootstrapToken} onChange={(event) => updateField("bootstrapToken", event.target.value)} placeholder="Token dari konfigurasi backend" required />
                <small>Token hanya dikirim saat setup dan tidak disimpan oleh frontend.</small>
              </label>
            </fieldset>

            {errorMessage && <div className="alert alert--error" role="alert">{errorMessage}</div>}
            {validationErrors.length > 0 && (
              <ul className="validation-list">
                {validationErrors.map((error) => <li key={error}>{error}</li>)}
              </ul>
            )}

            <button className="button button--primary" disabled={!isReady || isSubmitting}>
              {isSubmitting ? "Membuat organisasi..." : "Buat organisasi dan Owner"}
            </button>
          </form>
        </div>
      </section>
    </main>
  );
}