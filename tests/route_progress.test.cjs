// Runs the browser's routeProgress renderer against a small DOM stub: node tests/route_progress.test.cjs
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const source = fs.readFileSync(path.join(__dirname, "../src/wwwroot/app.js"), "utf8");
const start = source.indexOf("  function routeProgress(");
const end = source.indexOf("\n  function routeStops(", start);
assert.ok(start >= 0 && end > start, "routeProgress was not found in app.js");

function element(tag, className, text) {
  return { tag, className, text, style: {}, attrs: {}, children: [], setAttribute(key, value) { this.attrs[key] = value; } };
}

function append(parent, ...children) {
  parent.children.push(...children);
  return parent;
}

const routeProgress = vm.runInNewContext(`(${source.slice(start, end).trim()})`, {
  element,
  append,
  clamp: (value, min, max) => Math.max(min, Math.min(max, value)),
  integer: (value) => String(value),
});

function render(currentStopIndex, stopCount, milesTraveled) {
  const out = routeProgress({ currentStopIndex, stops: Array(stopCount).fill({}), milesTraveled, totalMiles: 100 });
  const [labels, track, note] = out.children;
  return { labels, track, fill: track.children[0], note };
}

assert.equal(render(0, 5, 0).fill.style.width, "0%");

// An inflated odometer must not push progress to 100% while stops remain.
const midway = render(2, 5, 28039);
assert.equal(midway.fill.style.width, "50%");
assert.equal(midway.labels.children[0].text, "2 / 4 legs complete");
assert.equal(midway.track.attrs["aria-valuenow"], "2");
assert.equal(midway.track.attrs["aria-valuemax"], "4");
assert.match(midway.note.text, /Odometer 28039 mi/);

// Only the final stop reports 100%.
assert.equal(render(3, 5, 28039).fill.style.width, "75%");
assert.equal(render(4, 5, 28039).fill.style.width, "100%");

// A fork that inserts a stop lengthens the route.
assert.equal(render(2, 6, 28039).fill.style.width, "40%");

// Empty route state.
const empty = render(0, 0, 0);
assert.equal(empty.fill.style.width, "0%");
assert.equal(empty.track.attrs["aria-valuemax"], "1");

console.log("PASS route progress follows stops, not the odometer");
