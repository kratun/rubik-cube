import type { ApiResult } from "./apiResult";
import type { ApplyMovesRequest, CubeState, ErrorDetails, ExpectedRevisionRequest } from "./types";

export const CUBES_PATH = "/api/cubes";
export const MOVES_SEGMENT = "moves";
export const UNDO_SEGMENT = "undo";
export const RESET_SEGMENT = "reset";

export const NETWORK_ERROR_STATUS = 0;
export const NETWORK_ERROR_MESSAGE =
  "Could not reach the server. Check your connection and try again.";
export const UNEXPECTED_RESPONSE_MESSAGE = "The server sent a response the app could not read.";

const JSON_CONTENT_TYPE = "application/json";
const CONTENT_TYPE_HEADER = "Content-Type";
const NETWORK_ERROR_TYPE = "network-error";
const NETWORK_ERROR_TITLE = "Network error";
const UNEXPECTED_RESPONSE_TYPE = "unexpected-response";
const UNEXPECTED_RESPONSE_TITLE = "Unexpected response";
const NO_TRACE_ID = "";

export const HTTP_GET = "GET";
export const HTTP_POST = "POST";

type HttpMethod = typeof HTTP_GET | typeof HTTP_POST;

function isErrorDetails(value: unknown): value is ErrorDetails {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.type === "string" &&
    typeof candidate.title === "string" &&
    typeof candidate.status === "number" &&
    typeof candidate.detail === "string" &&
    typeof candidate.traceId === "string"
  );
}

function localError(status: number, type: string, title: string, detail: string): ApiResult<never> {
  return { ok: false, status, error: { type, title, status, detail, traceId: NO_TRACE_ID } };
}

function unexpectedResponse(status: number): ApiResult<never> {
  return localError(
    status,
    UNEXPECTED_RESPONSE_TYPE,
    UNEXPECTED_RESPONSE_TITLE,
    UNEXPECTED_RESPONSE_MESSAGE,
  );
}

async function readJson(response: Response): Promise<unknown> {
  try {
    return (await response.json()) as unknown;
  } catch {
    return undefined;
  }
}

/** The one fetch wrapper (AD-7): it never throws, and never lets a caller see a raw Response. */
export async function request<T>(
  method: HttpMethod,
  path: string,
  body?: unknown,
): Promise<ApiResult<T>> {
  let response: Response;
  try {
    response = await fetch(path, {
      method,
      ...(body === undefined
        ? {}
        : { headers: { [CONTENT_TYPE_HEADER]: JSON_CONTENT_TYPE }, body: JSON.stringify(body) }),
    });
  } catch {
    return localError(
      NETWORK_ERROR_STATUS,
      NETWORK_ERROR_TYPE,
      NETWORK_ERROR_TITLE,
      NETWORK_ERROR_MESSAGE,
    );
  }

  const payload = await readJson(response);
  if (response.ok) {
    return payload === undefined
      ? unexpectedResponse(response.status)
      : { ok: true, data: payload as T };
  }
  return isErrorDetails(payload)
    ? { ok: false, status: response.status, error: payload }
    : unexpectedResponse(response.status);
}

function cubePath(cubeId: string, segment?: string): string {
  const base = `${CUBES_PATH}/${encodeURIComponent(cubeId)}`;
  return segment === undefined ? base : `${base}/${segment}`;
}

export function createCube(): Promise<ApiResult<CubeState>> {
  return request<CubeState>(HTTP_POST, CUBES_PATH);
}

export function getCube(cubeId: string): Promise<ApiResult<CubeState>> {
  return request<CubeState>(HTTP_GET, cubePath(cubeId));
}

export function applyMoves(
  cubeId: string,
  expectedRevision: string,
  moves: string[],
): Promise<ApiResult<CubeState>> {
  const body: ApplyMovesRequest = { expectedRevision, moves };
  return request<CubeState>(HTTP_POST, cubePath(cubeId, MOVES_SEGMENT), body);
}

export function undoMove(cubeId: string, expectedRevision: string): Promise<ApiResult<CubeState>> {
  const body: ExpectedRevisionRequest = { expectedRevision };
  return request<CubeState>(HTTP_POST, cubePath(cubeId, UNDO_SEGMENT), body);
}

export function resetCube(cubeId: string, expectedRevision: string): Promise<ApiResult<CubeState>> {
  const body: ExpectedRevisionRequest = { expectedRevision };
  return request<CubeState>(HTTP_POST, cubePath(cubeId, RESET_SEGMENT), body);
}
