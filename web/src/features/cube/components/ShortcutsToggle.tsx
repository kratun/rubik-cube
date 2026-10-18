import "./ShortcutsToggle.css";

export const SHORTCUTS_LABEL = "Keyboard shortcuts";
export const SHORTCUTS_HINT = "Letters F R U B L D turn the cube; lower case turns it back.";

const SHORTCUTS_HINT_ID = "shortcuts-hint";

interface ShortcutsToggleProps {
  enabled: boolean;
  onChange: (enabled: boolean) => void;
}

/** Lets the user turn the single-letter shortcuts off, so speech input and assistive technology cannot trigger them (WCAG 2.1.4). */
export function ShortcutsToggle({ enabled, onChange }: ShortcutsToggleProps) {
  return (
    <div className="shortcuts-toggle">
      <label>
        <input
          type="checkbox"
          checked={enabled}
          aria-describedby={SHORTCUTS_HINT_ID}
          onChange={(event) => {
            onChange(event.target.checked);
          }}
        />
        {SHORTCUTS_LABEL}
      </label>
      <small id={SHORTCUTS_HINT_ID} className="shortcuts-hint">
        {SHORTCUTS_HINT}
      </small>
    </div>
  );
}
