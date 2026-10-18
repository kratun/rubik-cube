// @vitest-environment node
import { ESLint } from "eslint";
import tseslint from "typescript-eslint";

const LAYER_RULE = "no-restricted-imports";
const NO_ERRORS = 0;
const LINT_TIMEOUT_MS = 30_000;
const IGNORED_FILE = "File ignored";
const ONE_ERROR = 1;
const FEATURE_DIR = "src/features/cube";
const COMPONENT_FILE = `${FEATURE_DIR}/components/Sample.tsx`;
const UTIL_FILE = `${FEATURE_DIR}/utils/sample.ts`;
const HOOK_FILE = `${FEATURE_DIR}/hooks/useSample.ts`;
const PAGE_FILE = `${FEATURE_DIR}/Sample.tsx`;
const TEST_FILE = `${FEATURE_DIR}/components/Sample.test.tsx`;

// Only the layer rule is under test, so type-aware linting is switched off: the virtual files are not on disk.
const eslint = new ESLint({
  overrideConfig: [{ files: ["**/*.{ts,tsx}"], ...tseslint.configs.disableTypeChecked }],
});

async function layerErrors(filePath: string, code: string): Promise<number> {
  const [result] = await eslint.lintText(code, { filePath });
  // A parse failure or an ignored file (ESLint warns "File ignored") would make the "passes" cases pass for the wrong reason.
  expect(
    result?.messages.filter(
      (message) => message.fatal === true || message.message.startsWith(IGNORED_FILE),
    ),
  ).toEqual([]);
  return result?.messages.filter((message) => message.ruleId === LAYER_RULE).length ?? NO_ERRORS;
}

describe("ESLint layer rules (architecture spine, frontend layout rules)", () => {
  it.each([
    [
      "a component importing the server client",
      COMPONENT_FILE,
      'import "../../../services/client";',
    ],
    ["a util importing react", UTIL_FILE, 'import "react";'],
    ["a util importing a hook", UTIL_FILE, 'import "../hooks/useSample";'],
    ["a hook importing a component", HOOK_FILE, 'import "../components/Sample";'],
    ["production code importing a fixture", PAGE_FILE, 'import "./fixtures/solvedCubeFixture";'],
  ])(
    "fails for %s",
    async (_name, filePath, code) => {
      expect(await layerErrors(filePath, code)).toBe(ONE_ERROR);
    },
    LINT_TIMEOUT_MS,
  );

  it.each([
    ["a component importing generated types", COMPONENT_FILE, 'import "../../../services/types";'],
    ["a hook importing the server client", HOOK_FILE, 'import "../../../services/client";'],
    ["a test importing a fixture", TEST_FILE, 'import "../fixtures/solvedCubeFixture";'],
  ])(
    "passes for %s",
    async (_name, filePath, code) => {
      expect(await layerErrors(filePath, code)).toBe(NO_ERRORS);
    },
    LINT_TIMEOUT_MS,
  );
});
