import type { NextConfig } from "next";

const defaultApiOrigin = "http://localhost:5203";

function resolveApiOrigin(): string {
  const configuredOrigin =
    process.env.SIPACUL_API_ORIGIN?.trim() || defaultApiOrigin;
  const url = new URL(configuredOrigin);

  if (!['http:', 'https:'].includes(url.protocol)) {
    throw new Error("SIPACUL_API_ORIGIN must use http or https.");
  }

  if (url.pathname !== "/" || url.search || url.hash) {
    throw new Error("SIPACUL_API_ORIGIN must be an origin without a path.");
  }

  return url.origin;
}

const apiOrigin = resolveApiOrigin();

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: "/api/v1/:path*",
        destination: `${apiOrigin}/api/v1/:path*`,
      },
    ];
  },
};

export default nextConfig;