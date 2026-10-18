import ttcExplodedViewFile from "../../../../../tests/Fixtures/ttc-exploded-view.txt?raw";
import type { CubeState } from "../../../services/types";
import { SOLVED_CUBE } from "./solvedCubeFixture";
import { TTC_TEST_SEQUENCE } from "../utils/ttcSequence";

export const TTC_REVISION = "5c1d0f7e9b2a4386d1e0f9a8b7c6d5e4f3a2b1c0d9e8f7a6b5c4d3e2f1a0b9c8";
export const TTC_HISTORY = TTC_TEST_SEQUENCE.join(" ");

const LINE_BREAK = "\n";

/** The nine expected lines, read from the file the backend tests use. */
export const TTC_EXPLODED_VIEW = ttcExplodedViewFile.trimEnd().split(LINE_BREAK);

/** The cube after the TTC Test Sequence on a solved cube. */
export const TTC_CUBE: CubeState = {
  ...SOLVED_CUBE,
  revision: TTC_REVISION,
  moves: [...TTC_TEST_SEQUENCE],
  explodedView: TTC_EXPLODED_VIEW,
};
