import type { CubeState } from "../../api/types";
import "./CubeView.css";

// Face keys of the API contract. The CSS places them in the exploded layout (AD-3).
export const FACE_NAMES = ["up", "left", "front", "right", "back", "down"] as const;
export const EXPLODED_VIEW_HEADING = "Exploded view";

const EXPLODED_VIEW_HEADING_ID = "exploded-view-heading";
const EXPLODED_VIEW_LINE_BREAK = "\n";

interface CubeViewProps {
  cube: CubeState;
}

export function faceLabel(face: string): string {
  return `${face} face`;
}

/** Renders `faces` as tiles and `explodedView` as text, exactly as the API sent them (AD-3). */
export function CubeView({ cube }: CubeViewProps) {
  return (
    <>
      <div className="cube-net">
        {FACE_NAMES.map((face) => (
          <div key={face} role="group" aria-label={faceLabel(face)} className={`face face-${face}`}>
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
        ))}
      </div>
      <section aria-labelledby={EXPLODED_VIEW_HEADING_ID}>
        <h2 id={EXPLODED_VIEW_HEADING_ID}>{EXPLODED_VIEW_HEADING}</h2>
        <pre>{cube.explodedView.join(EXPLODED_VIEW_LINE_BREAK)}</pre>
      </section>
    </>
  );
}
