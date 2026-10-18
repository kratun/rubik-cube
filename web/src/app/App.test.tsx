import { render, screen } from "@testing-library/react";
import { APP_TITLE, App } from "./App";

describe("App", () => {
  it("shows the application title as the main heading", () => {
    render(<App />);

    expect(screen.getByRole("heading", { level: 1, name: APP_TITLE })).toBeInTheDocument();
  });
});
