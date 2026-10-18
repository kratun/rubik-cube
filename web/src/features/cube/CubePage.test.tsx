import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  CUBES_PATH,
  HTTP_GET,
  HTTP_POST,
  MOVES_SEGMENT,
  NETWORK_ERROR_MESSAGE,
  RESET_SEGMENT,
  UNDO_SEGMENT,
} from "../../services/client";
import type { CubeState } from "../../services/types";
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
} from "./fixtures/apiResponseFixture";
import { CubePage, START_NEW_CUBE_LABEL } from "./CubePage";
import { FACE_NAMES, faceLabel } from "./components/CubeView";
import { EXPLODED_VIEW_HEADING } from "./components/ExplodedView";
import { NO_MOVES_MESSAGE } from "./components/MoveHistory";
import { MOVE_PAD_LABEL } from "./components/MovePad";
import {
  BUSY_LABEL,
  RESET_LABEL,
  SESSION_CONTROLS_LABEL,
  SHOW_EXPLODED_VIEW_LABEL,
  TTC_LABEL,
  UNDO_LABEL,
} from "./components/SessionControls";
import { MOVED_CUBE } from "./fixtures/movedCubeFixture";
import { TTC_CUBE, TTC_EXPLODED_VIEW, TTC_HISTORY } from "./fixtures/ttcCubeFixture";
import { MOVE_TOKENS } from "./utils/moves";
import { TTC_TEST_SEQUENCE } from "./utils/ttcSequence";
import { cubePagePath } from "./utils/sessionUrl";
import { SOLVED_CUBE, SOLVED_CUBE_ID, SOLVED_EXPLODED_VIEW } from "./fixtures/solvedCubeFixture";
import { CONFLICT_MESSAGE, CUBE_NOT_FOUND_MESSAGE } from "./hooks/useCubeSession";

const ROOT = "/";
const UNKNOWN_PATH = "/cubes/unknown";
const FRONT_COLOUR = "Green";
const FIRST_TOKEN = "F";
const BUSY_ATTRIBUTE = "aria-busy";
const BUSY_ATTRIBUTE_ON = "true";
const BUSY_ATTRIBUTE_OFF = "false";
const TILES_PER_FACE = 9;
const LINE_BREAK = "\n";
const HISTORY_SEPARATOR = " ";
const BAD_REQUEST_DETAIL = "The revision does not match.";
const CUBE_URL = `${CUBES_PATH}/${SOLVED_CUBE_ID}`;

function openUrl(path: string) {
  window.history.replaceState(null, "", path);
}

// The hook writes the URL in an effect, which may run just after the tiles appear.
async function expectUrlOfSolvedCube() {
  await waitFor(() => {
    expect(window.location.pathname).toBe(cubePagePath(SOLVED_CUBE_ID));
  });
}

// The exploded view starts closed; the first call opens its dialog and it stays open for the rest of the test.
function explodedViewText() {
  const showButton = screen.queryByRole("button", { name: SHOW_EXPLODED_VIEW_LABEL });
  if (showButton !== null) {
    fireEvent.click(showButton);
  }
  return screen.getByRole("dialog", { name: EXPLODED_VIEW_HEADING }).querySelector("pre")
    ?.textContent;
}
async function showCube(cube: CubeState) {
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(jsonResponse(cube));
  openUrl(cubePagePath(SOLVED_CUBE_ID));
  render(<CubePage />);
  await screen.findByRole("group", { name: faceLabel("up") });
}

function historyTexts() {
  return screen.getAllByRole("listitem").map((item) => item.textContent);
}

function callsTo(segment: string) {
  return vi.mocked(globalThis.fetch).mock.calls.filter(([url]) => url === `${CUBE_URL}/${segment}`);
}

afterEach(() => {
  vi.restoreAllMocks();
  openUrl(ROOT);
});

describe("CubePage", () => {
  it("creates a cube at the root, moves to its URL and shows the Solved State", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));
    openUrl(ROOT);

    render(<CubePage />);

    expect(
      await screen.findByRole("group", { name: faceLabel(FACE_NAMES[0]) }),
    ).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledWith(CUBES_PATH, { method: HTTP_POST });
    await expectUrlOfSolvedCube();
  });

  it("shows the faces as tiles and the exploded view as text", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));
    openUrl(ROOT);

    render(<CubePage />);

    const front = await screen.findByRole("group", { name: faceLabel("front") });
    expect(within(front).getAllByRole("img", { name: FRONT_COLOUR })).toHaveLength(TILES_PER_FACE);
    expect(explodedViewText()).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
  });

  it("loads the cube in the URL again on reload", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));
    openUrl(cubePagePath(SOLVED_CUBE_ID));

    render(<CubePage />);

    expect(await screen.findByRole("group", { name: faceLabel("up") })).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledWith(`${CUBES_PATH}/${SOLVED_CUBE_ID}`, { method: HTTP_GET });
    await expectUrlOfSolvedCube();
  });

  it("says cube not found for an unknown id and starts a new cube on request", async () => {
    vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(errorResponse(STATUS_NOT_FOUND, NOT_FOUND_DETAIL))
      .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));
    openUrl(UNKNOWN_PATH);

    render(<CubePage />);

    expect(await screen.findByRole("alert")).toHaveTextContent(CUBE_NOT_FOUND_MESSAGE);
    await userEvent.click(screen.getByRole("button", { name: START_NEW_CUBE_LABEL }));

    expect(await screen.findByRole("group", { name: faceLabel("down") })).toBeInTheDocument();
    await expectUrlOfSolvedCube();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  describe("moves", () => {
    const MOVE_BUTTON = "R";
    const CLOCKWISE_KEY = "L";
    const COUNTER_CLOCKWISE_KEY = "u";
    const COUNTER_CLOCKWISE_TOKEN = "U'";
    const MODIFIERS = ["Control", "Alt", "Meta"];
    const BAD_REQUEST_DETAIL = "Unknown move token.";
    const MOVES_URL = `${CUBE_URL}/${MOVES_SEGMENT}`;
    const MOVE_BODY_ARGUMENT = 1;

    async function showSolvedCube() {
      openUrl(cubePagePath(SOLVED_CUBE_ID));
      render(<CubePage />);
      await screen.findByRole("group", { name: faceLabel("up") });
    }

    function moveCalls(fetchMock: { mock: { calls: unknown[][] } }) {
      return fetchMock.mock.calls.filter(([url]) => url === MOVES_URL);
    }

    it("shows twelve move buttons", async () => {
      vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));

      await showSolvedCube();

      const pad = screen.getByRole("group", { name: MOVE_PAD_LABEL });
      expect(within(pad).getAllByRole("button")).toHaveLength(MOVE_TOKENS.length);
    });

    it("posts the current revision and the token of a clicked button, then shows the response", async () => {
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE));
      await showSolvedCube();

      await userEvent.click(screen.getByRole("button", { name: MOVE_BUTTON }));

      await waitFor(() => {
        expect(explodedViewText()).toBe(MOVED_CUBE.explodedView.join(LINE_BREAK));
      });
      expect(moveCalls(fetchMock)).toHaveLength(1);
      expect(moveCalls(fetchMock)[0]?.[MOVE_BODY_ARGUMENT]).toMatchObject({ method: HTTP_POST });
      expect(requestBodyOf(moveCalls(fetchMock)[0])).toEqual({
        expectedRevision: SOLVED_CUBE.revision,
        moves: [MOVE_BUTTON],
      });
    });

    it("sends the counter-clockwise move for a lower-case key", async () => {
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE));
      await showSolvedCube();

      await userEvent.keyboard(COUNTER_CLOCKWISE_KEY);

      await waitFor(() => {
        expect(moveCalls(fetchMock)).toHaveLength(1);
      });
      expect(requestBodyOf(moveCalls(fetchMock)[0])).toEqual({
        expectedRevision: SOLVED_CUBE.revision,
        moves: [COUNTER_CLOCKWISE_TOKEN],
      });
    });

    it("sends the clockwise move for an upper-case key", async () => {
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE));
      await showSolvedCube();

      await userEvent.keyboard(CLOCKWISE_KEY);

      await waitFor(() => {
        expect(moveCalls(fetchMock)).toHaveLength(1);
      });
      expect(requestBodyOf(moveCalls(fetchMock)[0])).toMatchObject({ moves: [CLOCKWISE_KEY] });
    });

    it.each(MODIFIERS)("sends nothing for a move key pressed with %s", async (modifier) => {
      const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));
      await showSolvedCube();

      await userEvent.keyboard(`{${modifier}>}${COUNTER_CLOCKWISE_KEY}{/${modifier}}`);

      expect(moveCalls(fetchMock)).toHaveLength(0);
    });

    it.each([
      [STATUS_BAD_REQUEST, BAD_REQUEST_DETAIL],
      [STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL],
    ])("shows the message of a %i and keeps the last good cube", async (status, detail) => {
      vi.spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(errorResponse(status, detail));
      await showSolvedCube();

      await userEvent.click(screen.getByRole("button", { name: MOVE_BUTTON }));

      expect(await screen.findByRole("alert")).toHaveTextContent(detail);
      expect(explodedViewText()).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
      const pad = screen.getByRole("group", { name: MOVE_PAD_LABEL });
      expect(within(pad).getAllByRole("button")).toHaveLength(MOVE_TOKENS.length);
    });

    it("shows the network message, stays usable and accepts the next move", async () => {
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockRejectedValueOnce(new TypeError("Failed to fetch"))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE));
      await showSolvedCube();

      await userEvent.click(screen.getByRole("button", { name: MOVE_BUTTON }));
      expect(await screen.findByRole("alert")).toHaveTextContent(NETWORK_ERROR_MESSAGE);
      await userEvent.click(screen.getByRole("button", { name: MOVE_BUTTON }));

      await waitFor(() => {
        expect(explodedViewText()).toBe(MOVED_CUBE.explodedView.join(LINE_BREAK));
      });
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
      expect(moveCalls(fetchMock)).toHaveLength(2);
    });

    it("reloads the session on a 409 and says the cube was changed by someone else", async () => {
      const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL))
        .mockResolvedValueOnce(jsonResponse(MOVED_CUBE));
      await showSolvedCube();

      await userEvent.click(screen.getByRole("button", { name: MOVE_BUTTON }));

      expect(await screen.findByRole("alert")).toHaveTextContent(CONFLICT_MESSAGE);
      expect(explodedViewText()).toBe(MOVED_CUBE.explodedView.join(LINE_BREAK));
      expect(moveCalls(fetchMock)).toHaveLength(1);
      expect(fetchMock).toHaveBeenLastCalledWith(CUBE_URL, { method: HTTP_GET });
    });
  });

  describe("history, undo and reset", () => {
    const HISTORY = ["F", "R'", "U"];
    const CUBE_WITH_HISTORY = { ...MOVED_CUBE, moves: HISTORY };
    const ACTIONS = [
      { label: UNDO_LABEL, segment: UNDO_SEGMENT },
      { label: RESET_LABEL, segment: RESET_SEGMENT },
    ];

    it("lists the moves of the session in order", async () => {
      await showCube(CUBE_WITH_HISTORY);

      expect(historyTexts()).toEqual(HISTORY);
    });

    it("says there are no moves yet and disables Undo for an empty session", async () => {
      await showCube(SOLVED_CUBE);

      expect(screen.getByText(NO_MOVES_MESSAGE)).toBeInTheDocument();
      expect(screen.getByRole("button", { name: UNDO_LABEL })).toBeDisabled();
    });

    it("sends the current revision to undo and shows the state before the last move", async () => {
      await showCube(MOVED_CUBE);
      vi.mocked(globalThis.fetch).mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));

      await userEvent.click(screen.getByRole("button", { name: UNDO_LABEL }));

      await waitFor(() => {
        expect(explodedViewText()).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
      });
      expect(callsTo(UNDO_SEGMENT)).toHaveLength(1);
      expect(requestBodyOf(callsTo(UNDO_SEGMENT)[0])).toEqual({
        expectedRevision: MOVED_CUBE.revision,
      });
      expect(screen.getByText(NO_MOVES_MESSAGE)).toBeInTheDocument();
      expect(screen.getByRole("button", { name: UNDO_LABEL })).toBeDisabled();
    });

    it("moves focus to Reset when the last Undo disables the Undo button", async () => {
      await showCube(MOVED_CUBE);
      vi.mocked(globalThis.fetch).mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));

      await userEvent.click(screen.getByRole("button", { name: UNDO_LABEL }));

      await waitFor(() => {
        expect(screen.getByRole("button", { name: UNDO_LABEL })).toBeDisabled();
      });
      expect(screen.getByRole("button", { name: RESET_LABEL })).toHaveFocus();
    });

    it("calls reset with the current revision and shows an empty history and the Solved State", async () => {
      await showCube(CUBE_WITH_HISTORY);
      vi.mocked(globalThis.fetch).mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));

      await userEvent.click(screen.getByRole("button", { name: RESET_LABEL }));

      await waitFor(() => {
        expect(screen.getByText(NO_MOVES_MESSAGE)).toBeInTheDocument();
      });
      expect(explodedViewText()).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
      expect(requestBodyOf(callsTo(RESET_SEGMENT)[0])).toEqual({
        expectedRevision: CUBE_WITH_HISTORY.revision,
      });
    });

    describe.each(ACTIONS)("$label failures", ({ label, segment }) => {
      it.each([
        [STATUS_BAD_REQUEST, BAD_REQUEST_DETAIL],
        [STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL],
      ])("shows the message of a %i and keeps the last good state", async (status, detail) => {
        await showCube(CUBE_WITH_HISTORY);
        vi.mocked(globalThis.fetch).mockResolvedValueOnce(errorResponse(status, detail));

        await userEvent.click(screen.getByRole("button", { name: label }));

        expect(await screen.findByRole("alert")).toHaveTextContent(detail);
        expect(historyTexts()).toEqual(HISTORY);
        expect(explodedViewText()).toBe(CUBE_WITH_HISTORY.explodedView.join(LINE_BREAK));
        expect(callsTo(segment)).toHaveLength(1);
      });

      it("shows the network message and keeps the last good state", async () => {
        await showCube(CUBE_WITH_HISTORY);
        vi.mocked(globalThis.fetch).mockRejectedValueOnce(new TypeError("Failed to fetch"));

        await userEvent.click(screen.getByRole("button", { name: label }));

        expect(await screen.findByRole("alert")).toHaveTextContent(NETWORK_ERROR_MESSAGE);
        expect(historyTexts()).toEqual(HISTORY);
      });

      it("reloads the session on a 409 and says the cube was changed by someone else", async () => {
        await showCube(CUBE_WITH_HISTORY);
        vi.mocked(globalThis.fetch)
          .mockResolvedValueOnce(errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL))
          .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE));

        await userEvent.click(screen.getByRole("button", { name: label }));

        expect(await screen.findByRole("alert")).toHaveTextContent(CONFLICT_MESSAGE);
        expect(screen.getByText(NO_MOVES_MESSAGE)).toBeInTheDocument();
        expect(callsTo(segment)).toHaveLength(1);
        expect(globalThis.fetch).toHaveBeenLastCalledWith(CUBE_URL, { method: HTTP_GET });
      });
    });
  });

  describe("TTC test sequence", () => {
    const TTC_ERROR_DETAIL = "The sequence could not be applied.";
    const CUBE_WITH_HISTORY = { ...MOVED_CUBE, moves: ["F", "R'", "U"] };

    async function pressTtcButton() {
      await userEvent.click(screen.getByRole("button", { name: TTC_LABEL }));
    }

    async function expectTtcResult() {
      await waitFor(() => {
        expect(explodedViewText()).toBe(TTC_EXPLODED_VIEW.join(LINE_BREAK));
      });
      expect(historyTexts().join(HISTORY_SEPARATOR)).toBe(TTC_HISTORY);
    }

    it("applies the sequence to a solved session with one request and no reset", async () => {
      await showCube(SOLVED_CUBE);
      vi.mocked(globalThis.fetch).mockResolvedValueOnce(jsonResponse(TTC_CUBE));

      await pressTtcButton();

      await expectTtcResult();
      expect(callsTo(RESET_SEGMENT)).toHaveLength(0);
      expect(callsTo(MOVES_SEGMENT)).toHaveLength(1);
      expect(requestBodyOf(callsTo(MOVES_SEGMENT)[0])).toEqual({
        expectedRevision: SOLVED_CUBE.revision,
        moves: TTC_TEST_SEQUENCE,
      });
    });

    it("resets a session with moves first, then applies the sequence on the reset revision", async () => {
      await showCube(CUBE_WITH_HISTORY);
      vi.mocked(globalThis.fetch)
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(jsonResponse(TTC_CUBE));

      await pressTtcButton();

      await expectTtcResult();
      expect(requestBodyOf(callsTo(RESET_SEGMENT)[0])).toEqual({
        expectedRevision: CUBE_WITH_HISTORY.revision,
      });
      expect(requestBodyOf(callsTo(MOVES_SEGMENT)[0])).toEqual({
        expectedRevision: SOLVED_CUBE.revision,
        moves: TTC_TEST_SEQUENCE,
      });
      const urls = vi.mocked(globalThis.fetch).mock.calls.map(([url]) => url);
      expect(urls.slice(-2)).toEqual([
        `${CUBE_URL}/${RESET_SEGMENT}`,
        `${CUBE_URL}/${MOVES_SEGMENT}`,
      ]);
    });

    it.each([
      [STATUS_BAD_REQUEST, BAD_REQUEST_DETAIL],
      [STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL],
    ])("shows the message of a %i and keeps the last good state", async (status, detail) => {
      await showCube(SOLVED_CUBE);
      vi.mocked(globalThis.fetch).mockResolvedValueOnce(errorResponse(status, detail));

      await pressTtcButton();

      expect(await screen.findByRole("alert")).toHaveTextContent(detail);
      expect(explodedViewText()).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
    });

    it("shows the network message and keeps the last good state", async () => {
      await showCube(SOLVED_CUBE);
      vi.mocked(globalThis.fetch).mockRejectedValueOnce(new TypeError("Failed to fetch"));

      await pressTtcButton();

      expect(await screen.findByRole("alert")).toHaveTextContent(NETWORK_ERROR_MESSAGE);
      expect(explodedViewText()).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
    });

    it("keeps the old cube when the reset step fails", async () => {
      await showCube(CUBE_WITH_HISTORY);
      vi.mocked(globalThis.fetch).mockResolvedValueOnce(
        errorResponse(STATUS_SERVER_ERROR, SERVER_ERROR_DETAIL),
      );

      await pressTtcButton();

      expect(await screen.findByRole("alert")).toHaveTextContent(SERVER_ERROR_DETAIL);
      expect(historyTexts()).toEqual(CUBE_WITH_HISTORY.moves);
      expect(callsTo(MOVES_SEGMENT)).toHaveLength(0);
    });

    it("shows the reset state with the message when reset worked but the sequence failed", async () => {
      await showCube(CUBE_WITH_HISTORY);
      vi.mocked(globalThis.fetch)
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockResolvedValueOnce(errorResponse(STATUS_SERVER_ERROR, TTC_ERROR_DETAIL));

      await pressTtcButton();

      expect(await screen.findByRole("alert")).toHaveTextContent(TTC_ERROR_DETAIL);
      expect(screen.getByText(NO_MOVES_MESSAGE)).toBeInTheDocument();
      expect(explodedViewText()).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
    });

    it("shows the reset state with the network message when the network fails after the reset", async () => {
      await showCube(CUBE_WITH_HISTORY);
      vi.mocked(globalThis.fetch)
        .mockResolvedValueOnce(jsonResponse(SOLVED_CUBE))
        .mockRejectedValueOnce(new TypeError("Failed to fetch"));

      await pressTtcButton();

      expect(await screen.findByRole("alert")).toHaveTextContent(NETWORK_ERROR_MESSAGE);
      expect(screen.getByText(NO_MOVES_MESSAGE)).toBeInTheDocument();
    });

    it.each([
      ["the reset", [errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL)]],
      [
        "the sequence",
        [jsonResponse(SOLVED_CUBE), errorResponse(STATUS_CONFLICT, SERVER_ERROR_DETAIL)],
      ],
    ])("reloads the session on a 409 from %s and says it was changed", async (_step, answers) => {
      await showCube(CUBE_WITH_HISTORY);
      for (const answer of [...answers, jsonResponse(MOVED_CUBE)]) {
        vi.mocked(globalThis.fetch).mockResolvedValueOnce(answer);
      }

      await pressTtcButton();

      expect(await screen.findByRole("alert")).toHaveTextContent(CONFLICT_MESSAGE);
      expect(explodedViewText()).toBe(MOVED_CUBE.explodedView.join(LINE_BREAK));
      expect(globalThis.fetch).toHaveBeenLastCalledWith(CUBE_URL, { method: HTTP_GET });
    });

    it("ignores a second click while the first sequence is in flight", async () => {
      await showCube(CUBE_WITH_HISTORY);
      let answerReset: (response: Response) => void = () => undefined;
      vi.mocked(globalThis.fetch)
        .mockReturnValueOnce(
          new Promise<Response>((resolve) => {
            answerReset = resolve;
          }),
        )
        .mockResolvedValueOnce(jsonResponse(TTC_CUBE));

      await pressTtcButton();
      await pressTtcButton();
      answerReset(jsonResponse(SOLVED_CUBE));

      await expectTtcResult();
      expect(callsTo(RESET_SEGMENT)).toHaveLength(1);
      expect(callsTo(MOVES_SEGMENT)).toHaveLength(1);
    });
  });

  describe("busy state", () => {
    it("shows a busy cue and keeps focus on the clicked button until the answer arrives", async () => {
      await showCube(SOLVED_CUBE);
      let answerMove: (response: Response) => void = () => undefined;
      vi.mocked(globalThis.fetch).mockReturnValueOnce(
        new Promise<Response>((resolve) => {
          answerMove = resolve;
        }),
      );
      const moveButton = screen.getByRole("button", { name: FIRST_TOKEN });
      expect(screen.queryByText(BUSY_LABEL)).not.toBeInTheDocument();

      await userEvent.click(moveButton);
      await userEvent.click(moveButton);

      expect(screen.getByText(BUSY_LABEL)).toBeInTheDocument();
      expect(screen.getByRole("group", { name: MOVE_PAD_LABEL })).toHaveAttribute(
        BUSY_ATTRIBUTE,
        BUSY_ATTRIBUTE_ON,
      );
      expect(screen.getByRole("group", { name: SESSION_CONTROLS_LABEL })).toHaveAttribute(
        BUSY_ATTRIBUTE,
        BUSY_ATTRIBUTE_ON,
      );
      expect(moveButton).toHaveFocus();
      expect(callsTo(MOVES_SEGMENT)).toHaveLength(1);

      answerMove(jsonResponse(MOVED_CUBE));

      await waitFor(() => {
        expect(screen.queryByText(BUSY_LABEL)).not.toBeInTheDocument();
      });
      expect(screen.getByRole("group", { name: MOVE_PAD_LABEL })).toHaveAttribute(
        BUSY_ATTRIBUTE,
        BUSY_ATTRIBUTE_OFF,
      );
    });
  });
});
