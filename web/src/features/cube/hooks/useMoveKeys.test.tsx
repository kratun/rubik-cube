import { fireEvent, render, renderHook, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useMoveKeys } from "./useMoveKeys";

const CLOCKWISE_KEY = "F";
const COUNTER_CLOCKWISE_KEY = "f";
const COUNTER_CLOCKWISE_TOKEN = "F'";
const MODIFIERS = ["Control", "Alt", "Meta"];
const NOT_MOVE_KEY = "x";
const FIELD_LABEL = "Notes";
const CHECKBOX_LABEL = "Switch";
const BUTTON_LABEL = "Focus";
const DIALOG_BUTTON_LABEL = "Close";

function withModifier(modifier: string, key: string): string {
  return `{${modifier}>}${key}{/${modifier}}`;
}

function renderKeys(enabled = true) {
  const onMove = vi.fn();
  const view = renderHook(() => {
    useMoveKeys(onMove, enabled);
  });
  return { onMove, ...view };
}

describe("useMoveKeys", () => {
  it("still sends the move while a checkbox has focus", async () => {
    const { onMove } = renderKeys();
    render(<input type="checkbox" aria-label={CHECKBOX_LABEL} />);
    screen.getByRole("checkbox", { name: CHECKBOX_LABEL }).focus();

    await userEvent.keyboard(CLOCKWISE_KEY);

    expect(onMove).toHaveBeenCalledExactlyOnceWith(CLOCKWISE_KEY);
  });

  it("sends no move while the shortcuts are switched off", async () => {
    const { onMove } = renderKeys(false);

    await userEvent.keyboard(CLOCKWISE_KEY);

    expect(onMove).not.toHaveBeenCalled();
  });

  it("sends the clockwise move for an upper-case letter", async () => {
    const { onMove } = renderKeys();

    await userEvent.keyboard(CLOCKWISE_KEY);

    expect(onMove).toHaveBeenCalledExactlyOnceWith(CLOCKWISE_KEY);
  });

  it("sends the counter-clockwise move for a lower-case letter", async () => {
    const { onMove } = renderKeys();

    await userEvent.keyboard(COUNTER_CLOCKWISE_KEY);

    expect(onMove).toHaveBeenCalledExactlyOnceWith(COUNTER_CLOCKWISE_TOKEN);
  });

  it("ignores other keys", async () => {
    const { onMove } = renderKeys();

    await userEvent.keyboard(NOT_MOVE_KEY);

    expect(onMove).not.toHaveBeenCalled();
  });

  it.each(MODIFIERS)("ignores a move key pressed with %s", async (modifier) => {
    const { onMove } = renderKeys();

    await userEvent.keyboard(withModifier(modifier, COUNTER_CLOCKWISE_KEY));

    expect(onMove).not.toHaveBeenCalled();
  });

  it("ignores letters typed into a text field", async () => {
    const { onMove } = renderKeys();
    render(<input type="text" aria-label={FIELD_LABEL} />);

    await userEvent.type(screen.getByRole("textbox", { name: FIELD_LABEL }), CLOCKWISE_KEY);

    expect(onMove).not.toHaveBeenCalled();
  });

  it("still sends the move when a button has focus", async () => {
    const { onMove } = renderKeys();
    render(<button type="button">{BUTTON_LABEL}</button>);
    await userEvent.click(screen.getByRole("button", { name: BUTTON_LABEL }));

    await userEvent.keyboard(CLOCKWISE_KEY);

    expect(onMove).toHaveBeenCalledExactlyOnceWith(CLOCKWISE_KEY);
  });

  it("ignores the repeats of a held key", () => {
    const { onMove } = renderKeys();

    fireEvent.keyDown(window, { key: CLOCKWISE_KEY, repeat: true });

    expect(onMove).not.toHaveBeenCalled();
  });

  it("removes the listener on unmount", async () => {
    const { onMove, unmount } = renderKeys();
    unmount();

    await userEvent.keyboard(CLOCKWISE_KEY);

    expect(onMove).not.toHaveBeenCalled();
  });

  it("ignores keys pressed inside a modal dialog", async () => {
    const onMove = vi.fn();
    function Page() {
      useMoveKeys(onMove, true);
      return (
        <div role="dialog" aria-modal="true" aria-label="Dialog">
          <button type="button">{DIALOG_BUTTON_LABEL}</button>
        </div>
      );
    }
    render(<Page />);
    screen.getByRole("button", { name: DIALOG_BUTTON_LABEL }).focus();

    await userEvent.keyboard(CLOCKWISE_KEY);

    expect(onMove).not.toHaveBeenCalled();
  });
});
