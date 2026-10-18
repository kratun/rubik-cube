import { useEffect } from "react";
import { moveForKey } from "./moves";

const TEXT_ENTRY_TAGS: readonly string[] = ["INPUT", "TEXTAREA", "SELECT"];

// Letters typed into a text field are text, not moves. A focused button is different: its own keys
// are Space and Enter, which are not move letters.
function isTextEntry(target: EventTarget | null): boolean {
  return (
    target instanceof HTMLElement &&
    (TEXT_ENTRY_TAGS.includes(target.tagName) || target.isContentEditable)
  );
}

/** Sends the move of each pressed face letter to `onMove`; keys with Ctrl, Alt or Meta and held-key repeats are ignored. */
export function useMoveKeys(onMove: (token: string) => void): void {
  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (
        event.repeat ||
        event.ctrlKey ||
        event.altKey ||
        event.metaKey ||
        isTextEntry(event.target)
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
  }, [onMove]);
}
