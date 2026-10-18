import { useEffect, useRef } from "react";
import "./ExplodedView.css";

export const EXPLODED_VIEW_HEADING = "Exploded view";
export const CLOSE_EXPLODED_VIEW_LABEL = "Close";

const EXPLODED_VIEW_HEADING_ID = "exploded-view-heading";
const EXPLODED_VIEW_LINE_BREAK = "\n";
const ESCAPE_KEY = "Escape";
const TAB_KEY = "Tab";

interface ExplodedViewProps {
  lines: readonly string[];
  open: boolean;
  onClose: () => void;
}

/**
 * The `explodedView` lines of the API as text, unchanged (AD-3), in a modal dialog so the page does not move.
 * Escape, the Close button or a click outside closes it; focus returns to the control that opened it.
 */
export function ExplodedView({ lines, open, onClose }: ExplodedViewProps) {
  const closeButton = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }
    const opener = document.activeElement;
    closeButton.current?.focus();
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === ESCAPE_KEY) {
        onClose();
      } else if (event.key === TAB_KEY) {
        // Close is the only control in the dialog, so Tab keeps the focus there instead of leaving it.
        event.preventDefault();
        closeButton.current?.focus();
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
      if (opener instanceof HTMLElement) {
        opener.focus();
      }
    };
  }, [open, onClose]);

  if (!open) {
    return null;
  }
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={EXPLODED_VIEW_HEADING_ID}
        className="modal"
        onClick={(event) => {
          event.stopPropagation();
        }}
      >
        <h2 id={EXPLODED_VIEW_HEADING_ID}>{EXPLODED_VIEW_HEADING}</h2>
        <pre>{lines.join(EXPLODED_VIEW_LINE_BREAK)}</pre>
        <button ref={closeButton} type="button" onClick={onClose}>
          {CLOSE_EXPLODED_VIEW_LABEL}
        </button>
      </div>
    </div>
  );
}
