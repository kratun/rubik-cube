import type { ErrorDetails } from "./index";

/** Outcome of an API call (AD-7): data on 2xx, the status and ErrorDetails otherwise. */
export type ApiResult<T> =
  { ok: true; data: T } | { ok: false; status: number; error: ErrorDetails };
