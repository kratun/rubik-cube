import js from "@eslint/js";
import prettier from "eslint-config-prettier";
import reactHooks from "eslint-plugin-react-hooks";
import globals from "globals";
import tseslint from "typescript-eslint";

// Where a feature file may import from (architecture spine, "Frontend layout rules"): utils are plain functions
// without React, hooks do not know components, components reach the server only through hooks.
const fixturesOnly = {
  group: ["**/fixtures/**"],
  message: "Fixtures in fixtures/ are for tests only.",
};
const productionFiles = ["src/**/*.{ts,tsx}"];
const testFiles = ["src/**/*.test.{ts,tsx}", "src/**/fixtures/**", "src/setupTests.ts"];
const restrict = (files, ...patterns) => ({
  files,
  ignores: testFiles,
  rules: { "no-restricted-imports": ["error", { patterns: [fixturesOnly, ...patterns] }] },
});
export default tseslint.config(
  { ignores: ["dist", "src/services/types/generated"] },
  js.configs.recommended,
  ...tseslint.configs.strictTypeChecked,
  reactHooks.configs.flat.recommended,
  {
    languageOptions: {
      globals: globals.browser,
      parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname },
    },
    rules: {
      "@typescript-eslint/no-explicit-any": "error",
    },
  },
  { files: ["eslint.config.js"], extends: [tseslint.configs.disableTypeChecked] },
  { files: ["*.config.{js,ts}"], languageOptions: { globals: globals.node } },
  restrict(productionFiles),
  restrict(["src/features/*/utils/**"], {
    group: [
      "react",
      "react-dom",
      "react/*",
      "react-dom/*",
      "**/hooks/**",
      "**/components/**",
      "**/services/**",
    ],
    message: "utils/ holds plain functions: no React, hooks, components or server access.",
  }),
  restrict(["src/features/*/hooks/**"], {
    group: ["**/components/**"],
    message: "hooks/ must not depend on components/.",
  }),
  restrict(["src/features/*/components/**"], {
    group: ["**/services/**", "!**/services/types", "!**/services/types/**"],
    message:
      "components/ reach the server only through hooks/ (AD-9); only services/types is allowed.",
  }),
  prettier,
);
