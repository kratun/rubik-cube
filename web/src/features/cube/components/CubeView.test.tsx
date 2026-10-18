import { render, screen, within } from "@testing-library/react";
import { CubeView, FACE_NAMES, faceLabel } from "./CubeView";
import { SOLVED_CUBE, SOLVED_FACE_COLOURS } from "../fixtures/solvedCubeFixture";

const TILES_PER_FACE = 9;
const FACE_COUNT = 6;

describe("CubeView", () => {
  it("shows six faces of nine tiles each", () => {
    render(<CubeView cube={SOLVED_CUBE} />);

    const faces = screen.getAllByRole("group");
    expect(faces).toHaveLength(FACE_COUNT);
    for (const face of faces) {
      expect(within(face).getAllByRole("img")).toHaveLength(TILES_PER_FACE);
    }
  });

  it.each(FACE_NAMES)("names the colour of every tile of the %s face", (face) => {
    render(<CubeView cube={SOLVED_CUBE} />);

    const tiles = within(screen.getByRole("group", { name: faceLabel(face) })).getAllByRole("img");
    for (const tile of tiles) {
      expect(tile).toHaveAccessibleName(SOLVED_FACE_COLOURS[face]);
    }
  });

  it("shows a visible caption for every face", () => {
    render(<CubeView cube={SOLVED_CUBE} />);

    for (const face of FACE_NAMES) {
      expect(
        within(screen.getByRole("group", { name: faceLabel(face) })).getByText(face),
      ).toBeInTheDocument();
    }
  });
});
