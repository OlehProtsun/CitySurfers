import { expect, test } from "@playwright/test";
test("real MVP: login, four overtakes, finish, rival and every screen", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  await page.goto("/");
  await page.getByRole("button", { name: "Enter the city" }).click();
  await expect(page.getByRole("heading", { name: "Kraków." })).toBeVisible();
  await expect(page.locator(".map-frame")).toHaveAttribute(
    "data-ready",
    "true",
    { timeout: 30000 },
  );
  expect(
    (await page.locator(".map-canvas").boundingBox())!.height,
  ).toBeGreaterThan(100);
  await page.screenshot({ path: "test-results/idle-map.png", fullPage: true });
  await page.getByRole("button", { name: "Start run" }).click();
  await expect(page.locator(".live-target")).toBeVisible();
  await expect(page.locator(".live-target")).toHaveCSS("opacity", "1");
  expect(
    (await page.locator(".live-target").boundingBox())!.y +
      (await page.locator(".live-target").boundingBox())!.height,
  ).toBeLessThan((await page.getByRole("navigation").boundingBox())!.y);
  await page.screenshot({ path: "test-results/live-run.png", fullPage: true });
  await expect(page.getByRole("heading", { name: "OVERTAKE!" })).toBeVisible();
  await expect(page.locator(".overtake")).toHaveCSS("opacity", "1");
  await page.screenshot({ path: "test-results/overtake.png" });
  await expect(page.getByText("6.80 km", { exact: true })).toBeVisible({
    timeout: 60000,
  });
  await expect(page.getByText("59 pts earned", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Finish run" }).click();
  await expect(
    page.getByRole("heading", { name: "Run complete." }),
  ).toBeVisible();
  await page.screenshot({ path: "test-results/summary.png", fullPage: true });
  await expect(page.getByText("+59 season points")).toBeVisible();
  await expect(page.getByText("6 pts to pass", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Back to City" }).click();
  await expect(page.getByText("6 pts to pass", { exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Ranking", exact: true }).click();
  await expect(page.getByLabel("Your ranking")).toContainText("59");
  await page.getByRole("button", { name: "month", exact: true }).click();
  await expect(page.getByText("6 pts to pass", { exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Map", exact: true }).click();
  await page.getByRole("button", { name: "today", exact: true }).click();
  await expect(
    page.getByRole("button", { name: /runners in area/ }).first(),
  ).toBeVisible();
  await page
    .getByRole("button", { name: /runners in area/ })
    .first()
    .click();
  await expect(page.getByText("ACTIVITY ZONE", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "month", exact: true }).click();
  await expect(
    page.getByRole("button", { name: "month", exact: true }),
  ).toHaveAttribute("aria-pressed", "true");
  await expect(page.locator(".map-credit")).toBeVisible();
  await page.getByRole("link", { name: "Progress", exact: true }).click();
  await expect(page.getByText("1 completed runs · 4 overtakes")).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Recent runs", exact: true }),
  ).toBeVisible();
  expect(errors).toEqual([]);
});
test("all presentation sizes preserve layout, controls and attribution", async ({
  page,
}) => {
  await page.goto("/login");
  await page.getByRole("button", { name: "Enter the city" }).click();
  for (const [width, height] of [
    [375, 812],
    [390, 844],
    [430, 932],
    [1440, 1000],
  ]) {
    await page.setViewportSize({ width: width!, height: height! });
    for (const path of ["/", "/ranking", "/map", "/progress"]) {
      await page.goto(path);
      await expect(page.getByRole("navigation")).toBeVisible();
      await expect(page.locator("h1")).toBeVisible();
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
      if (path === "/" || path === "/map")
        await expect(page.locator(".map-credit")).toBeVisible();
      await page.screenshot({
        path: `test-results/${width}-${path.replace("/", "") || "home"}.png`,
        fullPage: true,
      });
    }
  }
});

test("network recovery pauses writes and recovers an acknowledged but lost finish", async ({
  page,
}) => {
  await page.goto("/");
  await page.getByRole("button", { name: "Enter the city" }).click();
  let patches = 0;
  await page.route("**/api/runs/*/progress", async (route) => {
    patches++;
    await route.abort("failed");
  });
  await page.getByRole("button", { name: "Start run" }).click();
  await expect(
    page.getByRole("heading", { name: "Run paused. Let’s reconnect." }),
  ).toBeVisible();
  await page.waitForTimeout(2700);
  expect(patches).toBe(1);
  await expect(
    page.getByRole("button", { name: "Resume demo" }),
  ).toBeDisabled();
  await page.unroute("**/api/runs/*/progress");
  await page.getByRole("button", { name: "Retry", exact: true }).click();
  await expect(page.getByRole("button", { name: "Resume demo" })).toBeEnabled();
  await page.getByRole("button", { name: "Resume demo" }).click();
  await expect(page.getByText("0.60 km", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Pause demo" }).click();
  await page.route("**/api/runs/*/finish", async (route) => {
    await route.fetch();
    await route.abort("failed");
  });
  await page.getByRole("button", { name: "Finish run" }).click();
  await expect(
    page.getByRole("heading", { name: "Run complete." }),
  ).toBeVisible();
});
test("map provider failure keeps goal and start controls available", async ({
  page,
}) => {
  await page.route("https://tiles.openfreemap.org/**", (route) =>
    route.abort(),
  );
  await page.goto("/");
  await page.getByRole("button", { name: "Enter the city" }).click();
  await expect(page.getByText("The city map is taking a break.")).toBeVisible();
  await expect(page.getByRole("button", { name: "Start run" })).toBeEnabled();
  await expect(page.locator(".map-credit")).toBeVisible();
});
