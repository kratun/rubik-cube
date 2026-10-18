import { MOVE_TOKENS } from "./moves";

export const MOVE_PAD_LABEL = "Moves";

interface MovePadProps {
  onMove: (token: string) => void;
}

/** One real button per move token, so Tab and Enter or Space work without extra code. */
export function MovePad({ onMove }: MovePadProps) {
  return (
    <div role="group" aria-label={MOVE_PAD_LABEL}>
      {MOVE_TOKENS.map((token) => (
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
  );
}
