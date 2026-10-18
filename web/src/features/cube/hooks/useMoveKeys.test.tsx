import { fireEvent, render, renderHook, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useMoveKeys } from "./useMoveKeys";

const CLOCKWISE_KEY = "F";
const COUNTER_CLOCKWISE_KEY = "f";
const COUNTER_CLOCKWISE_TOKEN = "F'";
const MODIFIERS = ["Control", "Alt", "Meta"];
const NOT_MOVE_KEY = "x";
const FIELD_LABEL = "Notes";
const BUTTON_LABEL = "Focus";

function withModifier(modifier: string, key: string): string {
  return `{${modifier}>}${key}{/${modifier}}`;
}

function renderKeys() {
  const onMove = vi.fn();
  const view = renderHook(() => {
    useMoveKeys(onMove);
  });
  return { onMove, ...view };
}

describe("useMoveKeys", () => {
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
});
