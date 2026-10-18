import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SHORTCUTS_LABEL, ShortcutsToggle } from "./ShortcutsToggle";

describe("ShortcutsToggle", () => {
  it("shows the state it is given", () => {
    render(<ShortcutsToggle enabled onChange={vi.fn()} />);

    expect(screen.getByRole("checkbox", { name: SHORTCUTS_LABEL })).toBeChecked();
  });

  it("reports true when an unchecked switch is clicked", async () => {
    const onChange = vi.fn();
    render(<ShortcutsToggle enabled={false} onChange={onChange} />);

    await userEvent.click(screen.getByRole("checkbox", { name: SHORTCUTS_LABEL }));

    expect(onChange).toHaveBeenCalledExactlyOnceWith(true);
  });

  it("reports the new state when it is clicked", async () => {
    const onChange = vi.fn();
    render(<ShortcutsToggle enabled onChange={onChange} />);

    await userEvent.click(screen.getByRole("checkbox", { name: SHORTCUTS_LABEL }));

    expect(onChange).toHaveBeenCalledExactlyOnceWith(false);
  });
});
