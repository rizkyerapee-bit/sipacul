import type {
  AntiforgeryTokenResponse,
  ApiProblem,
  BootstrapOwnerRequest,
  BootstrapOwnerResponse,
  BootstrapStatus,
  CurrentUser,
  LoginRequest,
  Organization,
} from "@/lib/api/contracts";

const API_PREFIX = "/api/v1";
const BOOTSTRAP_TOKEN_HEADER = "X-SiPacul-Bootstrap-Token";

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ApiProblem | null;

  constructor(status: number, message: string, problem: ApiProblem | null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}

async function readProblem(response: Response): Promise<ApiProblem | null> {
  const contentType = response.headers.get("content-type") ?? "";

  if (!contentType.includes("json")) {
    return null;
  }

  try {
    return (await response.json()) as ApiProblem;
  } catch {
    return null;
  }
}

async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  if (init.body !== undefined && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_PREFIX}${path}`, {
    ...init,
    headers,
    credentials: "include",
    cache: "no-store",
  });

  if (!response.ok) {
    const problem = await readProblem(response);
    const message =
      problem?.detail ||
      problem?.title ||
      `Permintaan gagal dengan status ${response.status}.`;

    throw new ApiError(response.status, message, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function csrfRequest<T>(
  path: string,
  init: RequestInit,
): Promise<T> {
  const token = await apiRequest<AntiforgeryTokenResponse>("/auth/csrf");
  const headers = new Headers(init.headers);
  headers.set(token.headerName, token.requestToken);

  return apiRequest<T>(path, {
    ...init,
    headers,
  });
}

export function getBootstrapStatus(): Promise<BootstrapStatus> {
  return apiRequest<BootstrapStatus>("/bootstrap/status");
}

export function bootstrapOwner(
  bootstrapToken: string,
  request: BootstrapOwnerRequest,
): Promise<BootstrapOwnerResponse> {
  return csrfRequest<BootstrapOwnerResponse>("/bootstrap/owner", {
    method: "POST",
    headers: {
      [BOOTSTRAP_TOKEN_HEADER]: bootstrapToken,
    },
    body: JSON.stringify(request),
  });
}

export function login(request: LoginRequest): Promise<CurrentUser> {
  return csrfRequest<CurrentUser>("/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function logout(): Promise<void> {
  return csrfRequest<void>("/auth/logout", {
    method: "POST",
  });
}

export function getCurrentUser(): Promise<CurrentUser> {
  return apiRequest<CurrentUser>("/auth/me");
}

export function getOrganization(
  organizationId: string,
): Promise<Organization> {
  return apiRequest<Organization>(
    `/organizations/${encodeURIComponent(organizationId)}`,
  );
}