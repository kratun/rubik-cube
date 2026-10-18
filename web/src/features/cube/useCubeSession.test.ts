import { act, renderHook, waitFor } from "@testing-library/react";
import {
  CUBES_PATH,
  HTTP_GET,
  HTTP_POST,
  MOVES_SEGMENT,
  NETWORK_ERROR_MESSAGE,
} from "../../api/client";
import {
  NOT_FOUND_DETAIL,
  SERVER_ERROR_DETAIL,
  STATUS_BAD_REQUEST,
  STATUS_CONFLICT,
  STATUS_NOT_FOUND,
  STATUS_SERVER_ERROR,
  errorResponse,
  jsonResponse,
  requestBodyOf,
} from "./apiResponseFixture";
import { MOVED_CUBE, MOVED_REVISION, SECOND_MOVED_REVISION } from "./movedCubeFixture";
import { SOLVED_CUBE, SOLVED_CUBE_ID } from "./solvedCubeFixture";
import { cubePagePath } from "./sessionUrl";
import { CUBE_NOT_FOUND_MESSAGE, useCubeSession } from "./useCubeSession";

const UNKNOWN_ID = "unknown";
const ROOT = "/";

afterEach(() => {
  vi.restoreAllMocks();
  window.history.replaceState(null, "", ROOT);
});

describe("useCubeSession", () => {
  it("creates a cube without an id and moves the URL to it", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));

    const { result } = renderHook(() => useCubeSession(null));

    await waitFor(() => {
      expect(result.current.state).toEqual({ status: "ready", cube: SOLVED_CUBE, notice: null });
    });
    expect(fetchMock).toHaveBeenCalledWith(CUBES_PATH, { method: HTTP_POST });
    expect(window.location.pathname).toBe(cubePagePath(SOLVED_CUBE_ID));
  });

  it("loads the cube of the id", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));

    const { result } = renderHook(() => useCubeSession(SOLVED_CUBE_ID));

    await waitFor(() => {
      expect(result.current.state).toEqual({ status: "ready", cube: SOLVED_CUBE, notice: null });
    });
    expect(fetchMock).toHaveBeenCalledWith(`${CUBES_PATH}/${SOLVED_CUBE_ID}`, { method: HTTP_GET });
  });

  it.each([STATUS_NOT_FOUND, STATUS_BAD_REQUEST])(
    "reports cube not found when loading answers %i",
    async (status) => {
      vi.spyOn(globalThis, "fetch").mockResolvedValue(errorResponse(status, NOT_FOUND_DETAIL));

      const { result } = renderHook(() => useCubeSession(UNKNOWN_ID));

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "error",
          message: CUBE_NOT_FOUND_MESSAGE,
        });
      });
    },
  );

  it("reports the server message when loading fails otherwise", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      errorResponse(STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL),
    );

    const { result } = renderHook(() => useCubeSession(SOLVED_CUBE_ID));

    await waitFor(() => {
      expect(result.current.state).toEqual({ status: "error", message: SERVER_ERROR_DETAIL });
    });
  });

  it("reports the server message, not cube not found, when creating fails with 404", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      errorResponse(STATUS_NOT_FOUND, SERVER_ERROR_DETAIL),
    );

    const { result } = renderHook(() => useCubeSession(null));

    await waitFor(() => {
      expect(result.current.state).toEqual({ status: "error", message: SERVER_ERROR_DETAIL });
    });
  });

  it("reports the network error message when the server cannot be reached", async () => {
    vi.spyOn(globalThis, "fetch").mockRejectedValue(new TypeError("Failed to fetch"));

    const { result } = renderHook(() => useCubeSession(SOLVED_CUBE_ID));

    await waitFor(() => {
      expect(result.current.state).toEqual({ status: "error", message: NETWORK_ERROR_MESSAGE });
    });
  });

  it("ignores a response that arrives after the hook was unmounted", async () => {
    let resolveFetch: (response: Response) => void = () => undefined;
    vi.spyOn(globalThis, "fetch").mockReturnValue(
      new Promise<Response>((resolve) => {
        resolveFetch = resolve;
      }),
    );
    const { result, unmount } = renderHook(() => useCubeSession(null));
    unmount();

    await act(async () => {
      resolveFetch(jsonResponse(SOLVED_CUBE));
      await Promise.resolve();
    });

    expect(result.current.state).toEqual({ status: "loading" });
    expect(window.location.pathname).toBe(ROOT);
  });

  it("starts a new cube after an error", async () => {
    vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(errorResponse(STATUS_NOT_FOUND, NOT_FOUND_DETAIL))
      .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));
    const { result } = renderHook(() => useCubeSession(UNKNOWN_ID));
    await waitFor(() => {
      expect(result.current.state.status).toBe("error");
    });

    act(() => {
      result.current.startNewCube();
    });

    await waitFor(() => {
      expect(result.current.state).toEqual({ status: "ready", cube: SOLVED_CUBE, notice: null });
    });
    expect(window.location.pathname).toBe(cubePagePath(SOLVED_CUBE_ID));
  });

  describe("applyMove", () => {
    const TOKEN = "F";
    const SECOND_TOKEN = "R'";
    const MOVES_URL = `${CUBES_PATH}/${SOLVED_CUBE_ID}/${MOVES_SEGMENT}`;

    async function readySession() {
      const view = renderHook(() => useCubeSession(SOLVED_CUBE_ID));
      await waitFor(() => {
        expect(view.result.current.state.status).toBe("ready");
      });
      return view;
    }

    it("does nothing before the cube is loaded", () => {
      const fetchMock = vi.spyOn(globalThis, "fetch").mockReturnValue(new Promise(() => undefined));
      const { result } = renderHook(() => useCubeSession(SOLVED_CUBE_ID));

      act(() => {
        result.current.applyMove(TOKEN);
      });

      expect(fetchMock).toHaveBeenCalledOnce();
    });

    it("sends each move with the revision of the last response", async () => {
      const secondCube = { ...MOVED_CUBE, revision: SECOND_MOVED_REVISION };
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(secondCube));
      const { result } = await readySession();

      act(() => {
        result.current.applyMove(TOKEN);
      });
      await waitFor(() => {
        expect(result.current.state).toEqual({ status: "ready", cube: MOVED_CUBE, notice: null });
      });
      act(() => {
        result.current.applyMove(SECOND_TOKEN);
      });
      await waitFor(() => {
        expect(result.current.state).toEqual({ status: "ready", cube: secondCube, notice: null });
      });

      expect(requestBodyOf(fetchMock.mock.calls[1])).toEqual({
        expectedRevision: SOLVED_CUBE.revision,
        moves: [TOKEN],
      });
      expect(requestBodyOf(fetchMock.mock.calls[2])).toEqual({
        expectedRevision: MOVED_REVISION,
        moves: [SECOND_TOKEN],
      });
    });

    it("ignores a move while the previous one is still in flight", async () => {
      let resolveMove: (response: Response) => void = () => undefined;
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockReturnValueOnce(
          new Promise<Response>((resolve) => {
            resolveMove = resolve;
          }),
        );
      const { result } = await readySession();

      act(() => {
        result.current.applyMove(TOKEN);
        result.current.applyMove(SECOND_TOKEN);
      });
      await act(async () => {
        resolveMove(jsonResponse(MOVED_CUBE));
        await Promise.resolve();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({ status: "ready", cube: MOVED_CUBE, notice: null });
      });
      expect(fetchMock.mock.calls.filter(([url]) => url === MOVES_URL)).toHaveLength(1);
    });

    it("keeps the cube and shows the reload error when the reload after a 409 fails", async () => {
      vi.spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL))
        .mockResolvedValueOnce(errorResponse(STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL));
      const { result } = await readySession();

      act(() => {
        result.current.applyMove(TOKEN);
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "ready",
          cube: SOLVED_CUBE,
          notice: SERVER_ERROR_DETAIL,
        });
      });
    });
  });
});
