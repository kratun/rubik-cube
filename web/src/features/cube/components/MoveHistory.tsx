export const HISTORY_HEADING = "History";
export const NO_MOVES_MESSAGE = "No moves yet";

const HISTORY_HEADING_ID = "move-history-heading";

interface MoveHistoryProps {
  moves: readonly string[];
}

/** The moves of the session in the order the API reports them; nothing is derived here (AD-9). */
export function MoveHistory({ moves }: MoveHistoryProps) {
  return (
    <section aria-labelledby={HISTORY_HEADING_ID}>
      <h2 id={HISTORY_HEADING_ID}>{HISTORY_HEADING}</h2>
      {moves.length === 0 ? (
        <p>{NO_MOVES_MESSAGE}</p>
      ) : (
        <ol>
          {moves.map((move, index) => (
            // A move can repeat (F F), so the position is part of the key; the list only changes at its end (append, undo) or empties.
            <li key={`${String(index)}-${move}`}>{move}</li>
          ))}
        </ol>
      )}
    </section>
  );
}
