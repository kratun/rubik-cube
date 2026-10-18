import { render, screen } from "@testing-library/react";
import { AlertBanner } from "./AlertBanner";

const MESSAGE = "Something failed";

describe("AlertBanner", () => {
  it("shows the message as an alert", () => {
    render(<AlertBanner message={MESSAGE} />);

    expect(screen.getByRole("alert")).toHaveTextContent(MESSAGE);
  });
});
