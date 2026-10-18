import { act, renderHook, waitFor } from "@testing-library/react";
import {
  CUBES_PATH,
  HTTP_GET,
  HTTP_POST,
  MOVES_SEGMENT,
  NETWORK_ERROR_MESSAGE,
  RESET_SEGMENT,
  UNDO_SEGMENT,
} from "../../../services/client";
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
} from "../fixtures/apiResponseFixture";
import { MOVED_CUBE, MOVED_REVISION, SECOND_MOVED_REVISION } from "../fixtures/movedCubeFixture";
import { SOLVED_CUBE, SOLVED_CUBE_ID } from "../fixtures/solvedCubeFixture";
import { TTC_CUBE } from "../fixtures/ttcCubeFixture";
import { TTC_TEST_SEQUENCE } from "../utils/ttcSequence";
import type { CubeState } from "../../../services/types";
import { cubePagePath } from "../utils/sessionUrl";
import { renderReadySession } from "../fixtures/readySessionFixture";
import { CONFLICT_MESSAGE, CUBE_NOT_FOUND_MESSAGE, useCubeSession } from "./useCubeSession";

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

  describe.each([
    { name: "undo", segment: UNDO_SEGMENT },
    { name: "reset", segment: RESET_SEGMENT },
  ] as const)("$name", ({ name, segment }) => {
    const URL = `${CUBES_PATH}/${SOLVED_CUBE_ID}/${segment}`;

    it("posts the current revision and shows the response", async () => {
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));
      const { result } = await renderReadySession();

      act(() => {
        result.current[name]();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({ status: "ready", cube: SOLVED_CUBE, notice: null });
      });
      expect(fetchMock).toHaveBeenLastCalledWith(
        URL,
        expect.objectContaining({ method: HTTP_POST }),
      );
      expect(requestBodyOf(fetchMock.mock.calls[1])).toEqual({
        expectedRevision: MOVED_REVISION,
      });
    });

    it("keeps the cube and shows the message when it fails", async () => {
      vi.spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL));
      const { result } = await renderReadySession();

      act(() => {
        result.current[name]();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "ready",
          cube: MOVED_CUBE,
          notice: SERVER_ERROR_DETAIL,
        });
      });
    });

    it("reloads the session on a 409 and reports the conflict", async () => {
      vi.spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL))
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));
      const { result } = await renderReadySession();

      act(() => {
        result.current[name]();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "ready",
          cube: SOLVED_CUBE,
          notice: CONFLICT_MESSAGE,
        });
      });
    });

    it("keeps the cube and shows the reload error when the reload after a 409 fails", async () => {
      vi.spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL))
        .mockResolvedValueOnce(errorResponse(STATUS_SERVER_ERROR, NOT_FOUND_DETAIL));
      const { result } = await renderReadySession();

      act(() => {
        result.current[name]();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "ready",
          cube: MOVED_CUBE,
          notice: NOT_FOUND_DETAIL,
        });
      });
    });

    it("is ignored while another change is in flight", async () => {
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE))
        .mockReturnValueOnce(new Promise(() => undefined));
      const { result } = await renderReadySession();

      act(() => {
        result.current.applyMove("F");
        result.current[name]();
      });

      expect(fetchMock).toHaveBeenCalledTimes(2);
    });
  });

  describe("applyMove", () => {
    const TOKEN = "F";
    const SECOND_TOKEN = "R'";
    const MOVES_URL = `${CUBES_PATH}/${SOLVED_CUBE_ID}/${MOVES_SEGMENT}`;

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
      const { result } = await renderReadySession();

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
      const { result } = await renderReadySession();

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

    it("is busy from the start of a move until the answer arrives", async () => {
      let resolveMove: (response: Response) => void = () => undefined;
      vi.spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockReturnValueOnce(
          new Promise<Response>((resolve) => {
            resolveMove = resolve;
          }),
        );
      const { result } = await renderReadySession();
      expect(result.current.busy).toBe(false);

      act(() => {
        result.current.applyMove(TOKEN);
      });
      expect(result.current.busy).toBe(true);
      await act(async () => {
        resolveMove(jsonResponse(MOVED_CUBE));
        await Promise.resolve();
      });

      await waitFor(() => {
        expect(result.current.busy).toBe(false);
      });
    });

    it.each([
      ["a server error", errorResponse(STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL)],
      ["a conflict", errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL)],
    ])("stops being busy after %s", async (_name, failure) => {
      vi.spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(failure)
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));
      const { result } = await renderReadySession();

      act(() => {
        result.current.applyMove(TOKEN);
      });

      await waitFor(() => {
        expect(result.current.busy).toBe(false);
      });
      expect(result.current.state.status).toBe("ready");
    });

    it("accepts the next move once the answer arrived", async () => {
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE));
      const { result } = await renderReadySession();

      act(() => {
        result.current.applyMove(TOKEN);
      });
      await waitFor(() => {
        expect(result.current.busy).toBe(false);
      });
      act(() => {
        result.current.applyMove(SECOND_TOKEN);
      });

      expect(fetchMock.mock.calls.filter(([url]) => url === MOVES_URL)).toHaveLength(2);
    });

    it("keeps the cube and shows the reload error when the reload after a 409 fails", async () => {
      vi.spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL))
        .mockResolvedValueOnce(errorResponse(STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL));
      const { result } = await renderReadySession();

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

  describe("applyTtcSequence", () => {
    it("shows the confirmed reset cube when the reload after a 409 on the sequence fails", async () => {
      vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(jsonResponse(MOVED_CUBE));
      const { result } = renderHook(() => useCubeSession(SOLVED_CUBE_ID));
      await waitFor(() => {
        expect(result.current.state.status).toBe("ready");
      });
      vi.mocked(globalThis.fetch)
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL))
        .mockResolvedValueOnce(errorResponse(STATUS_SERVER_ERROR, NOT_FOUND_DETAIL));

      act(() => {
        result.current.applyTtcSequence();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "ready",
          cube: SOLVED_CUBE,
          notice: NOT_FOUND_DETAIL,
        });
      });
    });

    const RESET_URL = `${CUBES_PATH}/${SOLVED_CUBE_ID}/${RESET_SEGMENT}`;
    const MOVES_URL = `${CUBES_PATH}/${SOLVED_CUBE_ID}/${MOVES_SEGMENT}`;

    async function readySession(cube: CubeState) {
      vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(jsonResponse(cube));
      return renderReadySession();
    }

    it("resets a cube with moves, then applies the sequence on the revision the reset answered", async () => {
      const { result } = await readySession(MOVED_CUBE);
      const fetchMock = vi.mocked(globalThis.fetch);
      fetchMock
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(TTC_CUBE));

      act(() => {
        result.current.applyTtcSequence();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({ status: "ready", cube: TTC_CUBE, notice: null });
      });
      expect(fetchMock).toHaveBeenNthCalledWith(2, RESET_URL, expect.anything());
      expect(requestBodyOf(fetchMock.mock.calls[1])).toEqual({ expectedRevision: MOVED_REVISION });
      expect(fetchMock).toHaveBeenNthCalledWith(3, MOVES_URL, expect.anything());
      expect(requestBodyOf(fetchMock.mock.calls[2])).toEqual({
        expectedRevision: SOLVED_CUBE.revision,
        moves: TTC_TEST_SEQUENCE,
      });
    });

    it("skips the reset for a cube without moves", async () => {
      const { result } = await readySession(SOLVED_CUBE);
      const fetchMock = vi.mocked(globalThis.fetch);
      fetchMock.mockResolvedValueOnce(jsonResponse(TTC_CUBE));

      act(() => {
        result.current.applyTtcSequence();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({ status: "ready", cube: TTC_CUBE, notice: null });
      });
      expect(fetchMock).toHaveBeenCalledTimes(2);
      expect(fetchMock).toHaveBeenLastCalledWith(MOVES_URL, expect.anything());
    });

    it("shows the confirmed reset cube with the message when the sequence fails after the reset", async () => {
      const { result } = await readySession(MOVED_CUBE);
      vi.mocked(globalThis.fetch)
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL));

      act(() => {
        result.current.applyTtcSequence();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "ready",
          cube: SOLVED_CUBE,
          notice: SERVER_ERROR_DETAIL,
        });
      });
    });

    it("keeps the old cube when the reset fails", async () => {
      const { result } = await readySession(MOVED_CUBE);
      const fetchMock = vi.mocked(globalThis.fetch);
      fetchMock.mockResolvedValueOnce(errorResponse(STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL));

      act(() => {
        result.current.applyTtcSequence();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "ready",
          cube: MOVED_CUBE,
          notice: SERVER_ERROR_DETAIL,
        });
      });
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });

    it("reloads the session when the sequence answers 409 after the reset", async () => {
      const { result } = await readySession(MOVED_CUBE);
      vi.mocked(globalThis.fetch)
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE));

      act(() => {
        result.current.applyTtcSequence();
      });

      await waitFor(() => {
        expect(result.current.state).toEqual({
          status: "ready",
          cube: MOVED_CUBE,
          notice: CONFLICT_MESSAGE,
        });
      });
    });
  });
});
