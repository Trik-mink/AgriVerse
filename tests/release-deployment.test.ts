import { readFileSync } from "node:fs";
import { resolve } from "node:path";

import { describe, expect, it } from "vitest";

describe("Render release deployment", () => {
  it("installs build-only TypeScript dependencies before compiling", () => {
    const blueprint = readFileSync(resolve("render.yaml"), "utf8");

    expect(blueprint).toContain(
      "buildCommand: npm ci --include=dev && npm run build:server && npm prune --omit=dev",
    );
    expect(blueprint).toContain("startCommand: npm start");
    expect(blueprint).toContain("value: production");
  });
});
