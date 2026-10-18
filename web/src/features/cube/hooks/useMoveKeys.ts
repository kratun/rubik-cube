import { useEffect } from "react";
import { moveForKey } from "../utils/moves";

const TEXT_ENTRY_TAGS: readonly string[] = ["INPUT", "TEXTAREA", "SELECT"];
// Inputs that hold no text: a letter pressed while one has focus is still a shortcut (the switch that turns the shortcuts on again is one).
const NON_TEXT_INPUT_TYPES: readonly string[] = ["checkbox", "radio", "button", "submit", "reset"];
const MODAL_SELECTOR = '[aria-modal="true"]';

// Letters typed into a text field are text, not moves. A focused button is different: its own keys
// are Space and Enter, which are not move letters.
function isTextEntry(target: EventTarget | null): boolean {
  if (target instanceof HTMLInputElement && NON_TEXT_INPUT_TYPES.includes(target.type)) {
    return false;
  }
  return (
    target instanceof HTMLElement &&
    (TEXT_ENTRY_TAGS.includes(target.tagName) || target.isContentEditable)
  );
}

// While a modal dialog is open the page behind it is not the target of the keys.
function isInsideModal(target: EventTarget | null): boolean {
  return target instanceof HTMLElement && target.closest(MODAL_SELECTOR) !== null;
}

/** Sends the move of each pressed face letter to `onMove` while `enabled` (WCAG 2.1.4); keys with Ctrl, Alt or Meta and held-key repeats are ignored. */
export function useMoveKeys(onMove: (token: string) => void, enabled: boolean): void {
  useEffect(() => {
    if (!enabled) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (
        event.repeat ||
        event.ctrlKey ||
        event.altKey ||
        event.metaKey ||
        isTextEntry(event.target) ||
        isInsideModal(event.target)
      ) {
        return;
      }
      const token = moveForKey(event.key);
      if (token !== null) {
        onMove(token);
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [onMove, enabled]);
}
