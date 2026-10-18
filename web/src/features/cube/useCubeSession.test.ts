import { act, renderHook, waitFor } from "@testing-library/react";
import { CUBES_PATH, HTTP_GET, HTTP_POST, NETWORK_ERROR_MESSAGE } from "../../api/client";
import {
  NOT_FOUND_DETAIL,
  SERVER_ERROR_DETAIL,
  STATUS_BAD_REQUEST,
  STATUS_NOT_FOUND,
  STATUS_SERVER_ERROR,
  errorResponse,
  jsonResponse,
} from "./apiResponseFixture";
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
      expect(result.current.state).toEqual({ status: "ready", cube: SOLVED_CUBE });
    });
    expect(fetchMock).toHaveBeenCalledWith(CUBES_PATH, { method: HTTP_POST });
    expect(window.location.pathname).toBe(cubePagePath(SOLVED_CUBE_ID));
  });

  it("loads the cube of the id", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));

    const { result } = renderHook(() => useCubeSession(SOLVED_CUBE_ID));

    await waitFor(() => {
      expect(result.current.state).toEqual({ status: "ready", cube: SOLVED_CUBE });
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
      expect(result.current.state).toEqual({ status: "ready", cube: SOLVED_CUBE });
    });
    expect(window.location.pathname).toBe(cubePagePath(SOLVED_CUBE_ID));
  });
});
