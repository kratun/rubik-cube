import type { ErrorDetails } from "../../api/types";

export const STATUS_OK = 200;
export const STATUS_BAD_REQUEST = 400;
export const STATUS_NOT_FOUND = 404;
export const STATUS_SERVER_ERROR = 500;

export const NOT_FOUND_DETAIL = "No cube exists with this id.";
export const SERVER_ERROR_DETAIL = "The server failed.";

const ERROR_TYPE = "error";
const ERROR_TITLE = "Error";
const TRACE_ID = "trace";
const JSON_HEADERS = { "Content-Type": "application/json" };

export function jsonResponse(body: unknown, status = STATUS_OK): Response {
  return new Response(JSON.stringify(body), { status, headers: JSON_HEADERS });
}

export function errorResponse(status: number, detail: string): Response {
  const body: ErrorDetails = {
    type: ERROR_TYPE,
    title: ERROR_TITLE,
    status,
    detail,
    traceId: TRACE_ID,
  };
  return jsonResponse(body, status);
}
