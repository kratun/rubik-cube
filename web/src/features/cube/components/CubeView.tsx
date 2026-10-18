import type { CubeState } from "../../../services/types";
import "./CubeView.css";

// Face keys of the API contract. The CSS places them in the exploded layout (AD-3).
export const FACE_NAMES = ["up", "left", "front", "right", "back", "down"] as const;

interface CubeViewProps {
  cube: CubeState;
}

export function faceLabel(face: string): string {
  return `${face} face`;
}

/** Renders `faces` as tiles, exactly as the API sent them (AD-3). */
export function CubeView({ cube }: CubeViewProps) {
  return (
    <div className="cube-net">
      {FACE_NAMES.map((face) => (
        <div key={face} role="group" aria-label={faceLabel(face)} className={`face face-${face}`}>
          {/* The group already has the accessible name; the caption is for sighted users. */}
          <span className="face-label" aria-hidden="true">
            {face}
          </span>
          <div className="face-tiles">
            {(cube.faces[face] ?? []).flatMap((row, rowIndex) =>
              row.map((colour, columnIndex) => (
                <div
                  key={`${String(rowIndex)}-${String(columnIndex)}`}
                  role="img"
                  aria-label={colour}
                  className="tile"
                  data-colour={colour}
                />
              )),
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
