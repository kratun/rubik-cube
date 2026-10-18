import { useEffect, useRef } from "react";

export const SESSION_CONTROLS_LABEL = "Session";
export const UNDO_LABEL = "Undo";
export const RESET_LABEL = "Reset";

interface SessionControlsProps {
  canUndo: boolean;
  onUndo: () => void;
  onReset: () => void;
}

/** Undo is disabled when there is nothing to undo; Reset stays available. */
export function SessionControls({ canUndo, onUndo, onReset }: SessionControlsProps) {
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
    <div role="group" aria-label={SESSION_CONTROLS_LABEL}>
      <button ref={undoButton} type="button" disabled={!canUndo} onClick={onUndo}>
        {UNDO_LABEL}
      </button>
      <button ref={resetButton} type="button" onClick={onReset}>
        {RESET_LABEL}
      </button>
    </div>
  );
}
