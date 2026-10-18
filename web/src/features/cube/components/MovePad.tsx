import { MOVE_TOKENS_BY_FACE } from "../utils/moves";
import "./MovePad.css";

export const MOVE_PAD_LABEL = "Moves";

const MOVE_PAD_LABEL_ID = "move-pad-label";

interface MovePadProps {
  onMove: (token: string) => void;
  busy: boolean;
}

/** One real button per move token, so Tab and Enter or Space work without extra code. */
export function MovePad({ onMove, busy }: MovePadProps) {
  return (
    <div role="group" aria-labelledby={MOVE_PAD_LABEL_ID} aria-busy={busy} className="move-pad">
      <h2 id={MOVE_PAD_LABEL_ID} className="group-label">
        {MOVE_PAD_LABEL}
      </h2>
      <div className="move-pad-faces">
        {MOVE_TOKENS_BY_FACE.map((pair) => (
          <div key={pair[0]} className="move-pair">
            {pair.map((token) => (
              <button
                key={token}
                type="button"
                onClick={() => {
                  onMove(token);
                }}
              >
                {token}
              </button>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}
