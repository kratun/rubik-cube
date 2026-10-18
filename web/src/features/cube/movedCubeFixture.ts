import type { CubeState } from "../../api/types";
import { SOLVED_CUBE } from "./solvedCubeFixture";

export const MOVED_REVISION = "a1b2c3d4e5f60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8f9";
export const SECOND_MOVED_REVISION =
  "0f9e8d7c6b5a49382716f5e4d3c2b1a00f9e8d7c6b5a49382716f5e4d3c2b1a0";
export const MOVED_FIRST_LINE = "      G G G";

/** A cube after one move, distinct from the Solved State in revision, history and text view. */
export const MOVED_CUBE: CubeState = {
  ...SOLVED_CUBE,
  revision: MOVED_REVISION,
  moves: ["F"],
  explodedView: [MOVED_FIRST_LINE, ...SOLVED_CUBE.explodedView.slice(1)],
};
