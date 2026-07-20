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

describe("macOS release packaging", () => {
  it("writes a checksum that verifies beside a downloaded archive", () => {
    const packagingScript = readFileSync(
      resolve("scripts/package-macos-release.sh"),
      "utf8",
    );

    expect(packagingScript).toContain(
      '$(basename -- "$archive_path")',
    );
    expect(packagingScript).not.toContain(
      'shasum -a 256 "$archive_path" > "$checksum_path"',
    );
  });
});
