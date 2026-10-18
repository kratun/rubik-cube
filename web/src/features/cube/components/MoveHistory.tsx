import { useEffect, useRef } from "react";
import "./MoveHistory.css";

export const HISTORY_HEADING = "History";
export const NO_MOVES_MESSAGE = "No moves yet";

const HISTORY_HEADING_ID = "move-history-heading";

interface MoveHistoryProps {
  moves: readonly string[];
}

/** The moves of the session in the order the API reports them; nothing is derived here (AD-9). */
export function MoveHistory({ moves }: MoveHistoryProps) {
  const list = useRef<HTMLOListElement>(null);

  // The list scrolls when it is long; keep the newest move in view.
  useEffect(() => {
    if (list.current !== null) {
      list.current.scrollTop = list.current.scrollHeight;
    }
  }, [moves]);

  return (
    <section aria-labelledby={HISTORY_HEADING_ID} className="move-history-section">
      <h2 id={HISTORY_HEADING_ID} className="group-label">
        {HISTORY_HEADING}
      </h2>
      {moves.length === 0 ? (
        <p>{NO_MOVES_MESSAGE}</p>
      ) : (
        // role="list" keeps the list semantics that some browsers drop for list-style: none.
        <ol
          ref={list}
          role="list"
          tabIndex={0}
          aria-labelledby={HISTORY_HEADING_ID}
          className="move-history"
        >
          {moves.map((move, index) => (
            // A move can repeat (F F), so the position is part of the key; the list only changes at its end (append, undo) or empties.
            <li key={`${String(index)}-${move}`} className="move-chip">
              {move}
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
