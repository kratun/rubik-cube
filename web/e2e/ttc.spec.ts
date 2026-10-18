import { readFileSync } from "node:fs";
import { expect, test } from "@playwright/test";

const FIXTURES_DIR = new URL("../../tests/Fixtures/", import.meta.url);
const FILE_ENCODING = "utf8";
const HISTORY_SEPARATOR = " ";
const TTC_BUTTON_NAME = "Apply TTC test sequence";
const EXPLODED_VIEW_DIALOG = "Exploded view";
const SHOW_EXPLODED_VIEW_BUTTON = "Show exploded view";
const HISTORY_REGION = "History";

const EXPLODED_VIEW_FILE = "ttc-exploded-view.txt";
const SEQUENCE_FILE = "ttc-sequence.txt";

function fixture(name: string): string {
  return readFileSync(new URL(name, FIXTURES_DIR), FILE_ENCODING).trimEnd();
}

test("the TTC test sequence button shows the expected exploded view and history", async ({
  page,
}) => {
  const expectedView = fixture(EXPLODED_VIEW_FILE);
  const expectedHistory = fixture(SEQUENCE_FILE);

  await page.goto("/");
  await page.getByRole("button", { name: TTC_BUTTON_NAME }).click();

  await page.getByRole("button", { name: SHOW_EXPLODED_VIEW_BUTTON }).click();
  const view = page.getByRole("dialog", { name: EXPLODED_VIEW_DIALOG }).locator("pre");
  // textContent keeps the leading spaces of the net; toHaveText would collapse whitespace.
  await expect.poll(() => view.textContent()).toBe(expectedView);
  const history = page.getByRole("region", { name: HISTORY_REGION }).getByRole("listitem");
  await expect(history).toHaveText(expectedHistory.split(HISTORY_SEPARATOR));
});
