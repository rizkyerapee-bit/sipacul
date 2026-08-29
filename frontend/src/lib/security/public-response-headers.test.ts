import { describe, expect, it } from "vitest";

import nextConfig from "../../../next.config";

const expectedHeaders = [
  {
    key: "Content-Security-Policy",
    value: "base-uri 'self'; frame-ancestors 'none'; object-src 'none'",
  },
  {
    key: "Referrer-Policy",
    value: "strict-origin-when-cross-origin",
  },
  {
    key: "X-Content-Type-Options",
    value: "nosniff",
  },
  {
    key: "X-Frame-Options",
    value: "DENY",
  },
];

describe("public response security headers", () => {
  it("applies the complete baseline to every public route", async () => {
    expect(nextConfig.headers).toBeTypeOf("function");

    const rules = await nextConfig.headers!();

    expect(rules).toEqual([
      {
        source: "/(.*)",
        headers: expectedHeaders,
      },
    ]);
  });

  it("keeps hosting-dependent and cache policy outside this checkpoint", async () => {
    const rules = await nextConfig.headers!();
    const names = rules[0].headers.map((header) => header.key.toLowerCase());

    expect(names).not.toContain("strict-transport-security");
    expect(names).not.toContain("cache-control");
    expect(names).not.toContain("access-control-allow-origin");
  });
});
