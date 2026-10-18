import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CUBES_PATH, HTTP_GET, HTTP_POST } from "../../api/client";
import {
  NOT_FOUND_DETAIL,
  STATUS_NOT_FOUND,
  errorResponse,
  jsonResponse,
} from "./apiResponseFixture";
import { CubePage, START_NEW_CUBE_LABEL } from "./CubePage";
import { EXPLODED_VIEW_HEADING, FACE_NAMES, faceLabel } from "./CubeView";
import { cubePagePath } from "./sessionUrl";
import { SOLVED_CUBE, SOLVED_CUBE_ID, SOLVED_EXPLODED_VIEW } from "./solvedCubeFixture";
import { CUBE_NOT_FOUND_MESSAGE } from "./useCubeSession";

const ROOT = "/";
const UNKNOWN_PATH = "/cubes/unknown";
const FRONT_COLOUR = "Green";
const TILES_PER_FACE = 9;
const LINE_BREAK = "\n";

function openUrl(path: string) {
  window.history.replaceState(null, "", path);
}

// The hook writes the URL in an effect, which may run just after the tiles appear.
async function expectUrlOfSolvedCube() {
  await waitFor(() => {
    expect(window.location.pathname).toBe(cubePagePath(SOLVED_CUBE_ID));
  });
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
    const view = screen.getByRole("region", { name: EXPLODED_VIEW_HEADING });
    expect(view.querySelector("pre")?.textContent).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
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
});
