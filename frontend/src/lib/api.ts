import Cookies from "js-cookie";

const TOKEN_COOKIE = "token";

export function getToken(): string | undefined {
  return Cookies.get(TOKEN_COOKIE);
}

export function setToken(token: string) {
  Cookies.set(TOKEN_COOKIE, token, { expires: 8, sameSite: "lax" });
}

export function clearToken() {
  Cookies.remove(TOKEN_COOKIE);
}

export class ApiError extends Error {
  status: number;
  body: unknown;
  errors?: Record<string, string[]>;

  constructor(status: number, body: unknown, message: string) {
    super(message);
    this.status = status;
    this.body = body;
    if (
      body &&
      typeof body === "object" &&
      "errors" in body &&
      body.errors &&
      typeof body.errors === "object" &&
      !Array.isArray(body.errors)
    ) {
      this.errors = body.errors as Record<string, string[]>;
    }
  }
}

function extractMessage(body: unknown, fallback: string): string {
  if (body && typeof body === "object") {
    const b = body as Record<string, unknown>;
    if (typeof b.message === "string") return b.message;
    if (Array.isArray(b.errors) && typeof b.errors[0] === "string") return b.errors[0];
    if (b.errors && typeof b.errors === "object") {
      const first = Object.values(b.errors as Record<string, string[]>)[0];
      if (Array.isArray(first) && first.length > 0) return first[0];
    }
  }
  return fallback;
}

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  formData?: FormData;
  query?: Record<string, string | number | boolean | undefined | null>;
  signal?: AbortSignal;
}

function buildQuery(query?: RequestOptions["query"]): string {
  if (!query) return "";
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === "") continue;
    params.set(key, String(value));
  }
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {};
  if (token) headers.Authorization = `Bearer ${token}`;

  let body: BodyInit | undefined;
  if (options.formData) {
    body = options.formData;
  } else if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(options.body);
  }

  const res = await fetch(`${path}${buildQuery(options.query)}`, {
    method: options.method ?? "GET",
    headers,
    body,
    signal: options.signal,
  });

  const correlationId = res.headers.get("X-Correlation-ID");
  if (correlationId) {
    // eslint-disable-next-line no-console
    console.debug("[correlation-id]", correlationId);
  }

  const contentType = res.headers.get("content-type") ?? "";
  const payload = contentType.includes("application/json") ? await res.json().catch(() => null) : null;

  if (!res.ok) {
    if (res.status === 401) {
      clearToken();
    }
    throw new ApiError(res.status, payload, extractMessage(payload, `İstek başarısız (${res.status})`));
  }

  return payload as T;
}

export const api = {
  get: <T,>(path: string, query?: RequestOptions["query"], signal?: AbortSignal) =>
    request<T>(path, { method: "GET", query, signal }),
  post: <T,>(path: string, body?: unknown) => request<T>(path, { method: "POST", body }),
  postForm: <T,>(path: string, formData: FormData) => request<T>(path, { method: "POST", formData }),
  put: <T,>(path: string, body?: unknown) => request<T>(path, { method: "PUT", body }),
  patch: <T,>(path: string, body?: unknown) => request<T>(path, { method: "PATCH", body }),
  delete: <T,>(path: string, body?: unknown) => request<T>(path, { method: "DELETE", body }),
};

// ---------------------------------------------------------------------------
// Backend zarf şekilleri — OLS.Business.Common.ApiResponse ile birebir.
// ---------------------------------------------------------------------------
export interface DataMessage<T> {
  data: T;
  message: string;
}

export interface Paginated<T> {
  data: T[];
  total: number;
  per_page: number;
  current_page: number;
  last_page: number;
  from: number | null;
  to: number | null;
}
