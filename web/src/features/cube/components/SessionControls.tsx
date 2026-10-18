import { useEffect, useRef } from "react";
import "./SessionControls.css";

export const SESSION_CONTROLS_LABEL = "Session";
export const UNDO_LABEL = "Undo";
export const RESET_LABEL = "Reset";
export const TTC_LABEL = "Apply TTC test sequence";
export const SHOW_EXPLODED_VIEW_LABEL = "Show exploded view";
export const BUSY_LABEL = "Working…";

const SESSION_CONTROLS_LABEL_ID = "session-controls-label";

interface SessionControlsProps {
  canUndo: boolean;
  busy: boolean;
  onUndo: () => void;
  onReset: () => void;
  onApplyTtc: () => void;
  onShowExplodedView: () => void;
}

/**
 * Undo is disabled when there is nothing to undo; Reset and the TTC preset stay available.
 * While a change is running the buttons stay enabled (a second click is ignored by the hook), so focus is kept.
 */
export function SessionControls({
  canUndo,
  busy,
  onUndo,
  onReset,
  onApplyTtc,
  onShowExplodedView,
}: SessionControlsProps) {
  const undoButton = useRef<HTMLButtonElement>(null);
  const resetButton = useRef<HTMLButtonElement>(null);
  const couldUndo = useRef(canUndo);

  // A button that becomes disabled loses keyboard focus. When the last Undo empties the history,
  // focus moves to Reset, its neighbour, so a keyboard user keeps their place.
  useEffect(() => {
    const undoLostFocus = couldUndo.current && !canUndo;
    const focusIsLost =
      document.activeElement === undoButton.current || document.activeElement === document.body;
    if (undoLostFocus && focusIsLost) {
      resetButton.current?.focus();
    }
    couldUndo.current = canUndo;
  }, [canUndo]);

  return (
    <>
      <div
        role="group"
        aria-labelledby={SESSION_CONTROLS_LABEL_ID}
        aria-busy={busy}
        className="session-controls"
      >
        <h2 id={SESSION_CONTROLS_LABEL_ID} className="group-label">
          {SESSION_CONTROLS_LABEL}
        </h2>
        <button ref={undoButton} type="button" disabled={!canUndo} onClick={onUndo}>
          {UNDO_LABEL}
        </button>
        <button ref={resetButton} type="button" className="button-danger" onClick={onReset}>
          {RESET_LABEL}
        </button>
        <button type="button" className="exploded-view-opener" onClick={onShowExplodedView}>
          {SHOW_EXPLODED_VIEW_LABEL}
        </button>
        <button type="button" className="button-primary" onClick={onApplyTtc}>
          {TTC_LABEL}
        </button>
      </div>
      {/* Outside the aria-busy group, which would hold back announcements inside it; always rendered so the row keeps its height (4.1.3). */}
      <span role="status" className="busy-cue">
        {busy && BUSY_LABEL}
      </span>
    </>
  );
}
