import {
  CUBES_PATH,
  HTTP_GET,
  HTTP_POST,
  MOVES_SEGMENT,
  NETWORK_ERROR_MESSAGE,
  NETWORK_ERROR_STATUS,
  RESET_SEGMENT,
  UNDO_SEGMENT,
  UNEXPECTED_RESPONSE_MESSAGE,
  applyMoves,
  createCube,
  getCube,
  request,
  resetCube,
  undoMove,
} from "./client";
import type { CubeState, ErrorDetails } from "./types";

const STATUS_OK = 200;
const STATUS_CREATED = 201;
const STATUS_NOT_FOUND = 404;
const STATUS_CONFLICT = 409;
const STATUS_BAD_GATEWAY = 502;

const CUBE_ID = "3f2c1b9e-0a53-4a43-b0a4-7f2f0f7e6c11";
const REVISION = "e3b0c442";
const MOVE_TOKEN = "F";
const ERROR_TYPE = "cube-not-found";
const ERROR_TITLE = "Cube not found";
const ERROR_DETAIL = "No cube has this id.";
const TRACE_ID = "00-abc";
const NETWORK_FAILURE = new TypeError("Failed to fetch");
const HTML_BODY = "<html>Bad Gateway</html>";
const JSON_HEADERS = { "Content-Type": "application/json" };

const cubeState: CubeState = {
  cubeId: CUBE_ID,
  revision: REVISION,
  moves: [],
  faces: { up: [["White"]] },
  explodedView: ["W"],
};

const notFound: ErrorDetails = {
  type: ERROR_TYPE,
  title: ERROR_TITLE,
  status: STATUS_NOT_FOUND,
  detail: ERROR_DETAIL,
  traceId: TRACE_ID,
};

function respondWith(body: unknown, status: number) {
  return vi
    .spyOn(globalThis, "fetch")
    .mockResolvedValue(new Response(JSON.stringify(body), { status, headers: JSON_HEADERS }));
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe("request", () => {
  it("returns ok with the parsed body for a 2xx response", async () => {
    respondWith(cubeState, STATUS_OK);

    const result = await request<CubeState>(HTTP_GET, CUBES_PATH);

    expect(result).toEqual({ ok: true, data: cubeState });
  });

  it("returns the status and the ErrorDetails for a non-2xx response", async () => {
    respondWith(notFound, STATUS_NOT_FOUND);

    const result = await request<CubeState>(HTTP_GET, CUBES_PATH);

    expect(result).toEqual({ ok: false, status: STATUS_NOT_FOUND, error: notFound });
  });

  it("returns status 0 and a generic message when the network fails", async () => {
    vi.spyOn(globalThis, "fetch").mockRejectedValue(NETWORK_FAILURE);

    const result = await request<CubeState>(HTTP_GET, CUBES_PATH);

    expect(result).toMatchObject({
      ok: false,
      status: NETWORK_ERROR_STATUS,
      error: { detail: NETWORK_ERROR_MESSAGE },
    });
  });

  it("returns a generic error with the real status when a non-2xx body is not ErrorDetails", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(HTML_BODY, { status: STATUS_BAD_GATEWAY }),
    );

    const result = await request<CubeState>(HTTP_GET, CUBES_PATH);

    expect(result).toMatchObject({
      ok: false,
      status: STATUS_BAD_GATEWAY,
      error: { detail: UNEXPECTED_RESPONSE_MESSAGE },
    });
  });

  it("returns a generic error when a 2xx body is not JSON", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response("", { status: STATUS_OK }));

    const result = await request<CubeState>(HTTP_GET, CUBES_PATH);

    expect(result).toMatchObject({
      ok: false,
      status: STATUS_OK,
      error: { detail: UNEXPECTED_RESPONSE_MESSAGE },
    });
  });

  it("sends a JSON body with a content type only when it has a body", async () => {
    const spy = respondWith(cubeState, STATUS_OK);
    const body = { expectedRevision: REVISION };

    await request<CubeState>(HTTP_POST, CUBES_PATH, body);
    await request<CubeState>(HTTP_GET, CUBES_PATH);

    expect(spy).toHaveBeenNthCalledWith(1, CUBES_PATH, {
      method: HTTP_POST,
      headers: JSON_HEADERS,
      body: JSON.stringify(body),
    });
    expect(spy).toHaveBeenNthCalledWith(2, CUBES_PATH, { method: HTTP_GET });
  });
});

describe("cube operations", () => {
  const cubePath = `${CUBES_PATH}/${CUBE_ID}`;

  it("createCube posts to the cubes path", async () => {
    const spy = respondWith(cubeState, STATUS_CREATED);

    const result = await createCube();

    expect(spy).toHaveBeenCalledWith(CUBES_PATH, { method: HTTP_POST });
    expect(result).toEqual({ ok: true, data: cubeState });
  });

  it("getCube reads the cube by its id", async () => {
    const spy = respondWith(cubeState, STATUS_OK);

    await getCube(CUBE_ID);

    expect(spy).toHaveBeenCalledWith(cubePath, { method: HTTP_GET });
  });

  it("applyMoves posts the revision and the moves", async () => {
    const spy = respondWith(cubeState, STATUS_OK);

    await applyMoves(CUBE_ID, REVISION, [MOVE_TOKEN]);

    expect(spy).toHaveBeenCalledWith(`${cubePath}/${MOVES_SEGMENT}`, {
      method: HTTP_POST,
      headers: JSON_HEADERS,
      body: JSON.stringify({ expectedRevision: REVISION, moves: [MOVE_TOKEN] }),
    });
  });

  it.each([
    ["undoMove", undoMove, UNDO_SEGMENT],
    ["resetCube", resetCube, RESET_SEGMENT],
  ])("%s posts the revision to its own path", async (_name, operation, segment) => {
    const spy = respondWith(cubeState, STATUS_OK);

    await operation(CUBE_ID, REVISION);

    expect(spy).toHaveBeenCalledWith(`${cubePath}/${segment}`, {
      method: HTTP_POST,
      headers: JSON_HEADERS,
      body: JSON.stringify({ expectedRevision: REVISION }),
    });
  });

  it("passes a 409 conflict through as an error result", async () => {
    respondWith({ ...notFound, status: STATUS_CONFLICT }, STATUS_CONFLICT);

    const result = await undoMove(CUBE_ID, REVISION);

    expect(result).toMatchObject({ ok: false, status: STATUS_CONFLICT });
  });
});
