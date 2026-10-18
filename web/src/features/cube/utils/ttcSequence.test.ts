import ttcSequenceFile from "../../../../../tests/Fixtures/ttc-sequence.txt?raw";
import { TTC_TEST_SEQUENCE } from "./ttcSequence";

const TOKEN_SEPARATOR = " ";

describe("TTC_TEST_SEQUENCE", () => {
  it("matches the TTC Test Sequence fixture shared with the backend tests", () => {
    expect(TTC_TEST_SEQUENCE.join(TOKEN_SEPARATOR)).toBe(ttcSequenceFile.trim());
  });
});
