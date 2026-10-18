import type { CubeState } from "../../api/types";

export const SOLVED_CUBE_ID = "0c635560-7b8e-4721-88bb-76d310403744";
export const SOLVED_REVISION = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

const FACE_SIZE = 3;

function uniformFace(colour: string): string[][] {
  return Array.from({ length: FACE_SIZE }, () => Array<string>(FACE_SIZE).fill(colour));
}

export const SOLVED_FACE_COLOURS: Record<string, string> = {
  up: "White",
  left: "Orange",
  front: "Green",
  right: "Red",
  back: "Blue",
  down: "Yellow",
};

export const SOLVED_EXPLODED_VIEW = [
  "      W W W",
  "      W W W",
  "      W W W",
  "O O O G G G R R R B B B",
  "O O O G G G R R R B B B",
  "O O O G G G R R R B B B",
  "      Y Y Y",
  "      Y Y Y",
  "      Y Y Y",
];

export const SOLVED_CUBE: CubeState = {
  cubeId: SOLVED_CUBE_ID,
  revision: SOLVED_REVISION,
  moves: [],
  faces: Object.fromEntries(
    Object.entries(SOLVED_FACE_COLOURS).map(([face, colour]) => [face, uniformFace(colour)]),
  ),
  explodedView: SOLVED_EXPLODED_VIEW,
};
