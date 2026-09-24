(() => {
  "use strict";

  const root = document.getElementById("app");
  const api = window.OregonTrailApi;
  let state = null;
  let busy = false;
  let online = true;
  let draftScreen = "";
  let draftValue = "";
  let pendingFocusKey = null;
  let storyPage = 0;
  let helpOpen = false;
  let helpReturnKey = null;
  let effects = true;
  let sceneStep = 0;
  let live = null;
  let queuedState = null;
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  try { effects = localStorage.getItem("asphalt-effects") !== "off"; } catch { /* Storage is optional. */ }
  document.documentElement.dataset.effects = effects ? "on" : "off";
  const announcement = element("div", "sr-only");
  announcement.setAttribute("role", "status");
  announcement.setAttribute("aria-live", "polite");
  document.body.append(announcement);

  const money = (value) =>
    new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", minimumFractionDigits: Number(value) % 1 ? 2 : 0, maximumFractionDigits: 2 })
      .format(Number(value) || 0);
  const integer = (value) => new Intl.NumberFormat("en-US").format(Number(value) || 0);
  const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

  function element(tag, className, content) {
    const result = document.createElement(tag);
    if (className) result.className = className;
    if (content !== undefined && content !== null) result.textContent = String(content);
    return result;
  }

  function append(parent, ...children) {
    for (const child of children) if (child) parent.append(child);
    return parent;
  }

  function labeledValue(label, value, className = "") {
    return append(element("div", `labeled-value ${className}`),
      element("span", "labeled-value__label", label),
      element("strong", "labeled-value__value", value));
  }

  function panelHeading(kicker, title) {
    return append(element("div", "panel-heading"),
      element("span", "eyebrow", kicker), element("h2", "panel-title", title));
  }

  function findAction(actionId) {
    return state?.screen.actions.find((action) => action.actionId === actionId);
  }

  function isHome() {
    return state.screen.id.endsWith("mainmenuscreen") || state.screen.id.endsWith("mainmenu");
  }

  function scene(name, caption = "") {
    const figure = element("figure", `scene scene--${name}`);
    const art = name === "vehicle"
      ? vehicleArt(state.hud.vehicleId, false, online && state.driving?.isDriving === true)
      : element("pre", "ascii-art", (window.TrailScenes[name] || window.TrailScenes.road).trimEnd());
    art.setAttribute("aria-hidden", "true");
    append(figure, art, caption ? element("figcaption", "scene-caption", caption) : null);
    return figure;
  }

  function drivingScene() {
    const driving = state.driving;
    const moving = online && driving?.isDriving;
    const figure = scene("vehicle");
    figure.classList.add("driving-scene");
    figure.dataset.driving = String(Boolean(moving));
    const status = !online ? "Reconnecting · motion paused" : moving ? "On the road"
      : driving?.status === "disabled" ? "Vehicle needs attention"
      : driving?.status === "resting" ? "Resting" : "Parked";
    const telemetry = append(element("figcaption", "driving-telemetry"),
      element("strong", "driving-status", status),
      element("span", "", state.hud.vehicleName),
      element("span", "", `${integer(driving?.milesRemaining ?? state.progress.milesToNextLocation)} mi to ${driving?.destination || state.progress.nextLocation || "Seattle"}`),
      element("span", "driving-pace", `${state.hud.pace} pace · ${state.hud.weather}`));
    figure.append(telemetry);
    return figure;
  }

  function vehicleArt(vehicleId, compact = false, moving = false) {
    const art = element(compact ? "span" : "pre", "ascii-art");
    art.setAttribute("aria-hidden", "true");
    art.dataset.vehicleId = vehicleId || "minivan";
    art.dataset.compact = String(compact);
    art.dataset.moving = String(moving);
    updateVehicleArt(art);
    return art;
  }

  function updateVehicleArt(art) {
    const animate = effects && !reducedMotion.matches && online;
    const preview = art.closest(".choice-card:not(:disabled):is(:hover, :focus-visible)");
    const frame = window.TrailScenes.vehicleFrame(art.dataset.vehicleId,
      animate ? sceneStep : 0,
      animate && (art.dataset.moving === "true" || Boolean(preview)),
      art.dataset.compact === "true");
    if (art.textContent !== frame) art.textContent = frame;
  }

  // One clock for decorative characters only; game state and focused controls stay untouched.
  let lastFrame = 0;
  function animateScene(timestamp) {
    if (!document.hidden && online && !reducedMotion.matches && effects && timestamp - lastFrame >= 100) {
      sceneStep++;
      lastFrame = timestamp;
      root.querySelectorAll('.ascii-art[data-moving="true"], .choice-card:is(:hover, :focus-visible) .ascii-art').forEach(updateVehicleArt);
    }
    requestAnimationFrame(animateScene);
  }
  requestAnimationFrame(animateScene);
  reducedMotion.addEventListener("change", () => {
    root.querySelectorAll(".ascii-art[data-vehicle-id]").forEach(updateVehicleArt);
    if (state?.crypto) render(false);
  });

  function localButton(label, key, callback, className = "") {
    const button = element("button", `local-button ${className}`, label);
    button.type = "button";
    button.dataset.focusKey = key;
    button.onclick = callback;
    return button;
  }

  function description(copy) {
    const block = element("div", "play-description");
    for (const paragraph of (copy || "").split(/\n\s*\n/)) {
      if (paragraph.trim()) block.append(element("p", "", paragraph.replace(/\s*\n\s*/g, " ").trim()));
    }
    return block;
  }

  function setupSteps() {
    const steps = ["Story", "Background", "Vehicle", "Party", "Departure"];
    const id = state.screen.id;
    const current = id.includes("profession") ? 1 : id.includes("vehicle") ? 2
      : /names/.test(id) ? 3 : /month/.test(id) ? 4 : 0;
    const list = element("ol", "setup-steps");
    list.setAttribute("aria-label", "Journey setup");
    steps.forEach((label, index) => {
      const item = element("li", index === current ? "is-current" : index < current ? "is-done" : "",
        `${index < current ? "+" : String(index + 1).padStart(2, "0")} ${label}`);
      if (index === current) item.setAttribute("aria-current", "step");
      list.append(item);
    });
    return list;
  }

  function sendAction(actionId, text = null, value = null) {
    const action = findAction(actionId);
    if (!state || busy || !action?.enabled) return;
    pendingFocusKey = document.activeElement?.dataset?.focusKey || null;
    busy = true;
    render(false);
    api.action(actionId, state.revision, state.journeyId, text, value,
      (json) => {
        busy = false;
        online = true;
        const incoming = parseState(json);
        if (!incoming) { applyQueuedState(); return; }
        if (incoming.actionAccepted) {
          draftScreen = incoming.screen.id;
          draftValue = "";
        }
        acceptState(incoming, true);
        applyQueuedState();
      },
      (message) => {
        busy = false;
        online = false;
        showError(message);
        // The server may have applied the command even if its HTTP acknowledgement was lost.
        // Keep the newest streamed state, and refresh without ever replaying the mutation.
        applyQueuedState();
        live?.refresh();
      });
  }

  function applyQueuedState() {
    if (!queuedState) return;
    const queued = queuedState;
    queuedState = null;
    acceptState(queued);
  }

  function actionButton(action, options = {}) {
    const button = element("button", `game-button ${options.className || ""}`, options.label || action.label);
    button.type = "button";
    button.dataset.actionId = action.actionId;
    button.dataset.focusKey = action.actionId;
    if (options.className === "game-button--step") button.setAttribute("aria-label", action.label);
    button.disabled = busy || !action.enabled;
    button.onclick = () => sendAction(action.actionId);
    return button;
  }

  function parseState(json) {
    try {
      const incoming = typeof json === "string" ? JSON.parse(json) : json;
      if (!incoming?.screen || !Number.isSafeInteger(incoming.revision)) throw new Error();
      return incoming;
    } catch {
      showError("The game sent a state this browser could not read.");
      return null;
    }
  }

  function acceptState(incoming, fromAction = false) {
    if (busy && !fromAction) { queuedState = incoming; return; }
    const sameJourney = state?.journeyId === incoming.journeyId;
    if (sameJourney && state && incoming.revision < state.revision) return;
    if (sameJourney && state && incoming.revision === state.revision && !fromAction) {
      if (!online) { state = incoming; online = true; render(false); }
      return;
    }
    const previousScreen = state?.screen.id;
    const previousCryptoPhase = state?.crypto?.phase;
    const previousCreator = state?.creator;
    state = incoming;
    online = true;
    if (!sameJourney || previousScreen !== state.screen.id) {
      draftScreen = state.screen.id;
      draftValue = "";
      storyPage = 0;
    }
    const cryptoTransition = previousCryptoPhase !== state.crypto?.phase &&
      (state.crypto?.phase === "live" || state.crypto?.phase === "result");
    let creatorFocus = null;
    const creator = state.creator;
    if (sameJourney && previousCreator && creator) {
      if (!previousCreator.started && creator.started) creatorFocus = "#creator-channel-title";
      else if (!previousCreator.draft && creator.draft) creatorFocus = "#creator-draft-title";
      else if (creator.stats.uploads > previousCreator.stats.uploads)
        creatorFocus = `#creator-video-${creator.videos[0].id}`;
      else if (creator.stats.reuploads > previousCreator.stats.reuploads) {
        const repaired = creator.videos.find(video => video.reedited &&
          previousCreator.videos.some(old => old.id === video.id && !old.reedited));
        if (repaired) creatorFocus = `#creator-video-${repaired.id}`;
      }
    }
    render(previousScreen !== state.screen.id || cryptoTransition || Boolean(creatorFocus), creatorFocus);
  }

  function showError(message) {
    if (!state) {
      root.replaceChildren(append(element("main", "load-state load-state--error"),
        element("span", "eyebrow", "Connection problem"),
        element("h1", "", "The road is out of reach"),
        element("p", "", message),
        append(element("button", "game-button game-button--solid", "Try again"))));
      root.querySelector("button").addEventListener("click", requestState);
      return;
    }
    state = { ...state, responseMessage: message };
    render(false);
  }

  function requestState() {
    live?.refresh();
  }

  function routeProgress(progress) {
    const percent = progress.totalMiles > 0
      ? clamp(Math.round(100 * progress.milesTraveled / progress.totalMiles), 0, 100)
      : 0;
    const track = element("div", "route-progress__track");
    track.setAttribute("role", "progressbar");
    track.setAttribute("aria-label", "Journey mileage");
    track.setAttribute("aria-valuemin", "0");
    track.setAttribute("aria-valuemax", String(progress.totalMiles));
    track.setAttribute("aria-valuenow", String(clamp(progress.milesTraveled, 0, progress.totalMiles)));
    const fill = element("span", "route-progress__fill");
    fill.style.width = `${percent}%`;
    return append(element("div", "route-progress"),
      append(element("div", "route-progress__labels"),
        element("strong", "", `${integer(progress.milesTraveled)} / ${integer(progress.totalMiles)} mi`),
        element("span", "", `${percent}% of route`)),
      append(track, fill));
  }

  function routeStops(progress, full = false) {
    const stops = progress.stops || [];
    const current = clamp(progress.currentStopIndex || 0, 0, Math.max(stops.length - 1, 0));
    const start = full ? 0 : Math.max(0, current - 1);
    const end = full ? stops.length : Math.min(stops.length, current + 3);
    const list = element("ol", "route-stops");
    for (let index = start; index < end; index++) {
      const stop = stops[index];
      const phase = index < current ? "passed" : index === current ? "current" : "ahead";
      const item = element("li", `route-stop route-stop--${phase}`);
      append(item,
        element("span", "route-stop__marker", index === current ? ">" : index < current ? "+" : "."),
        append(element("span", "route-stop__copy"),
          element("strong", "", stop.name),
          element("small", "", index === current ? "Current stop" : index < current ? "Passed" : stop.kind.replaceAll("-", " "))));
      list.append(item);
    }
    return list;
  }

  function routePanel(progress, full = false) {
    const panel = element("section", `rail-card route-card ${full ? "route-card--full" : ""}`);
    append(panel, panelHeading("01 / Navigation", full ? "Route manifest" : "The road ahead"));
    if (progress.stops?.length) {
      panel.append(routeProgress(progress));
      panel.append(routeStops(progress, full));
      if (!full && progress.currentStopIndex + 3 < progress.stops.length) {
        panel.append(element("p", "card-note", `${progress.stops.length - progress.currentStopIndex - 3} more stops after these`));
      }
    } else {
      panel.append(element("p", "card-note", "Route details will appear when the journey begins."));
    }
    return panel;
  }

  function inventoryCount(itemId) {
    return state.inventory.find((item) => item.itemId === itemId)?.quantity || 0;
  }

  function signal(label, value, detail, level = "normal") {
    return append(element("div", `signal signal--${level}`),
      element("span", "signal__label", label),
      element("strong", "signal__value", value),
      element("span", "signal__detail", detail));
  }

  function readinessPanel() {
    const hud = state.hud;
    const gas = inventoryCount("gas");
    const snacks = hud.foodPoundsRemaining ?? inventoryCount("snacks");
    const foodDays = hud.foodDaysRemaining || 0;
    const hasParty = hud.livingPartyCount > 0;
    const card = element("section", "rail-card readiness-card");
    append(card, panelHeading("02 / Vehicle telemetry", "Vital signs"));
    const grid = element("div", "signal-grid");
    append(grid,
      signal("Fuel", `${integer(gas)} cans`, "Needed to keep moving", gas < 6 ? "low" : "normal"),
      signal("Food", `${integer(snacks)} lb`, hasParty
        ? `${integer(foodDays)} days at ${hud.foodPerDay} lb/day` : "Party not assembled",
      hasParty && foodDays < 3 ? "low" : "normal"),
      signal("Party", `${hud.livingPartyCount} alive`, hud.health || "Health unknown",
        hud.livingPartyCount && hud.health?.toLowerCase() !== "good" ? "watch" : "normal"),
      signal("Cargo", `${integer(hud.cargoWeight)} / ${integer(hud.cargoCapacity)} lb`,
        "Loaded in vehicle", hud.cargoCapacity && hud.cargoWeight >= hud.cargoCapacity ? "watch" : "normal"));
    card.append(grid);
    if (hud.vehicleIssue) card.append(element("p", "issue", `${hud.vehicleIssue} needs attention before driving.`));
    const modes = element("div", "mode-pair");
    append(modes, labeledValue("Pace", hud.pace || "—"), labeledValue("Rations", hud.rations || "—"));
    card.append(modes);
    return card;
  }

  function foldPanel(card, title, folded) {
    if (!folded) return card;
    const details = element("details", `${card.className} rail-fold`);
    const summary = append(element("summary", ""), element("h2", "panel-title", title));
    card.querySelector(".panel-heading")?.remove();
    const body = element("div", "rail-fold__body");
    body.append(...card.childNodes);
    return append(details, summary, body);
  }

  function partyPanel(folded = false) {
    const card = element("section", "rail-card party-card");
    append(card, panelHeading("03 / Passenger manifest", "The people you carry"));
    if (!state.party.length) {
      card.append(element("p", "card-note", "Name your party to see everyone riding along."));
      return foldPanel(card, "Passenger manifest", folded);
    }
    const list = element("ul", "party-list");
    for (const person of state.party) {
      append(list, append(element("li", "party-member"),
        append(element("span", "party-member__avatar"), document.createTextNode(person.name.slice(0, 1).toUpperCase())),
        append(element("span", "party-member__identity"),
          element("strong", "", person.name),
          element("small", "", person.isLeader ? `${person.profession} · Driver` : person.profession)),
        element("span", `health health--${person.health.toLowerCase()}`, person.health)));
    }
    card.append(list);
    return foldPanel(card, "Passenger manifest", folded);
  }

  function cargoPanel(folded = false) {
    const card = element("section", "rail-card cargo-card");
    append(card, panelHeading("04 / Cargo manifest", "Supplies on board"));
    const stocked = state.inventory.filter((item) => item.quantity > 0);
    if (!stocked.length) {
      card.append(element("p", "card-note", "No supplies packed yet."));
      return foldPanel(card, "Cargo manifest", folded);
    }
    const list = element("dl", "cargo-list");
    for (const item of stocked) {
      append(list, element("dt", "", item.name),
        element("dd", "", `${integer(item.quantity)}${item.itemId === "snacks" ? " lb" : ""}`));
    }
    card.append(list);
    return foldPanel(card, "Cargo manifest", folded);
  }

  function setupPanel() {
    const panel = element("section", "rail-card setup-card");
    append(panel, panelHeading("Travel advisory / 2028", "A one-way proposition."), scene("vehicle"),
      element("p", "card-note", "A paid-off car. A folder of important documents. A country that treats catastrophe as a weather report."));
    const points = element("div", "setup-points");
    append(points,
      labeledValue("Origin", "Cape Coral, FL"),
      labeledValue("Destination", "Seattle, WA"),
      labeledValue("Distance", `~${integer(state.progress.totalMiles)} miles`));
    panel.append(points);
    panel.append(element("p", "advisory-note", "Keep fuel in the tank, food in the cooler, and someone alive at the wheel."));
    return panel;
  }

  function actionCard(action) {
    const card = element("button", `choice-card ${action.selected ? "is-selected" : ""}`);
    card.type = "button";
    card.disabled = busy || !action.enabled;
    card.dataset.actionId = action.actionId;
    card.dataset.focusKey = action.actionId;
    append(card,
      element("span", "choice-card__title", action.label),
      action.detail ? element("span", "choice-card__detail", action.detail) : null);
    if (action.vehicleId) {
      card.append(append(element("span", "choice-card__art"), vehicleArt(action.vehicleId, true)));
    }
    if (action.facts?.length) {
      const facts = element("span", "choice-card__facts");
      for (const fact of action.facts) {
        append(facts, append(element("span", "choice-fact"),
          element("small", "", fact.label), element("strong", "", fact.value)));
      }
      card.append(facts);
    }
    card.append(element("span", "choice-card__cta", action.enabled ? "[ select ]" : "[ unavailable ]"));
    card.onclick = () => sendAction(action.actionId);
    return card;
  }

  function choiceActions(actions) {
    const group = element("div", "choice-grid");
    for (const action of actions) group.append(actionCard(action));
    return group;
  }

  function simpleActions(actions, className = "") {
    const group = element("div", `action-list ${className}`);
    for (const [index, action] of actions.entries()) {
      group.append(actionButton(action, {
        className: action.kind === "continue" || action.kind === "primary" || action.kind === "restart"
          || (state.screen.kind === "setup" && index === 0 && !action.facts?.length)
          ? "game-button--solid" : "",
      }));
    }
    return group;
  }

  function inputPanel(input) {
    const panel = element("form", "input-card");
    const id = "game-input";
    const label = element("label", "input-card__label", input.label);
    label.htmlFor = id;
    const field = element("input", "game-input");
    field.id = id;
    field.dataset.focusKey = "game-input";
    field.type = input.kind === "number" ? "number" : "text";
    field.placeholder = input.placeholder || "";
    field.autocomplete = "off";
    field.value = draftScreen === state.screen.id ? draftValue : "";
    field.disabled = busy;
    field.required = !!input.required;
    if (input.min != null) field.min = String(input.min);
    if (input.max != null) field.max = input.max;
    if (input.maxLength != null) field.maxLength = input.maxLength;
    const submit = element("button", "game-button game-button--solid", "Confirm [↵]");
    submit.type = "submit";
    submit.dataset.focusKey = input.actionId;
    function valid(field) {
      const value = field.value.trim();
      if (input.required && !value) return false;
      if (input.kind === "number") {
        const parsed = Number(value);
        return value !== "" && Number.isInteger(parsed)
          && (input.min === null || parsed >= input.min)
          && (input.max === null || parsed <= input.max);
      }
      return input.maxLength === null || field.value.length <= input.maxLength;
    }
    const update = (field, submit) => {
      draftScreen = state.screen.id;
      draftValue = field.value;
      submit.disabled = busy || !valid(field);
    };
    field.oninput = (event) => update(event.currentTarget, event.currentTarget.form.querySelector('button[type="submit"]'));
    panel.onsubmit = (event) => {
      event.preventDefault();
      const field = event.currentTarget.querySelector("input");
      if (!valid(field)) return;
      const parsed = input.kind === "number" ? Number(field.value) : null;
      sendAction(input.actionId, field.value, parsed);
    };
    const hint = element("p", "input-hint", input.kind === "number"
      ? `Enter a whole number from ${input.min} to ${input.max}.`
      : `${input.required ? "" : state.screen.id.endsWith("epitapheditor") ? "Leave blank to finish the epitaph. " : "A blank response is allowed. "}${input.maxLength ? `${input.maxLength} characters maximum.` : ""}`);
    hint.id = "input-hint";
    field.setAttribute("aria-describedby", hint.id);
    append(panel, label, field, submit, hint);
    update(field, submit);
    return panel;
  }

  function activityMeter(progress) {
    if (!Number.isFinite(progress.activityTotal) || progress.activityTotal <= 0) return null;
    const current = clamp(progress.activityCurrent || 0, 0, progress.activityTotal);
    const percent = 100 * current / progress.activityTotal;
    const panel = element("div", "activity-meter");
    append(panel, append(element("div", "activity-meter__top"),
      element("strong", "", progress.activityLabel || "In progress"),
      element("span", "", `${integer(current)} / ${integer(progress.activityTotal)}`)));
    const track = element("div", "activity-meter__track");
    track.setAttribute("role", "progressbar");
    track.setAttribute("aria-label", progress.activityLabel || "Activity progress");
    track.setAttribute("aria-valuemin", "0");
    track.setAttribute("aria-valuemax", String(progress.activityTotal));
    track.setAttribute("aria-valuenow", String(current));
    const fill = element("span", "activity-meter__fill");
    fill.style.width = `${percent}%`;
    panel.append(append(track, fill));
    return panel;
  }

  function insuranceNotice() {
    const notice = scene("smilingSun");
    notice.classList.add("insurance-notice");
    append(notice, append(element("figcaption", "insurance-notice__copy"),
      element("p", "insurance-notice__company", "~ SUNSHINE STATE MUTUAL ~"),
      element("p", "", "* UNINSURABLE AT ANY PREMIUM. *"),
      element("p", "insurance-notice__signoff", '"HAVE A SUNNY DAY!"')));
    return notice;
  }

  function storyPanel(pages) {
    const titles = ["Notice of non-renewal", "What fits in the car", "The country in between", "Uphill from the sea", "History is graded on a curve"];
    const panel = element("section", "story-panel");
    const heading = element("h2", "story-title", titles[storyPage] || "The road ahead");
    heading.id = "story-title";
    heading.tabIndex = -1;
    append(panel, element("span", "eyebrow", `Chapter ${String(storyPage + 1).padStart(2, "0")} / ${String(pages.length).padStart(2, "0")}`), heading);
    if (storyPage === 0) panel.append(insuranceNotice());
    const copy = description(pages[storyPage]);
    copy.className = "story-copy";
    panel.append(copy);
    const controls = element("nav", "story-controls");
    controls.setAttribute("aria-label", "Opening story chapters");
    const change = (page) => {
      storyPage = page;
      render(false);
      root.querySelector("#story-title")?.focus();
      announcement.textContent = `Chapter ${page + 1} of ${pages.length}: ${titles[page]}`;
    };
    const prev = localButton("← Previous", "story-prev", () => change(storyPage - 1));
    prev.disabled = storyPage === 0;
    const lastChapter = storyPage === pages.length - 1;
    const next = lastChapter
      ? actionButton(state.screen.actions.find((action) => action.kind === "primary"), {
        className: "game-button--solid story-advance", label: "Choose your background →",
      })
      : localButton("Next chapter →", "story-next", () => change(storyPage + 1), "game-button game-button--solid story-advance");
    append(controls, prev, element("span", "story-position", `${storyPage + 1} / ${pages.length}`), next);
    panel.append(controls);
    return panel;
  }

  function scorePanel(score) {
    const panel = element("section", "score-panel");
    append(panel,
      element("span", "eyebrow", "Final clout"),
      element("strong", "score-panel__value", integer(score.finalPoints)),
      element("span", "score-panel__rank", score.rating),
      element("p", "card-note", `${integer(score.basePoints)} base points × ${score.multiplier} background multiplier`));
    const table = element("table", "score-table");
    const head = element("thead", "");
    append(head, append(element("tr", ""), element("th", "", "What made it"), element("th", "", "Points")));
    const rows = element("tbody", "");
    for (const line of score.lines || []) {
      append(rows, append(element("tr", ""),
        element("td", "", `${integer(line.quantity)} ${line.description}`),
        element("td", "", integer(line.points))));
    }
    if (score.choiceScoreDelta) append(rows, append(element("tr", ""),
      element("td", "", "Road decisions"),
      element("td", "", `${score.choiceScoreDelta > 0 ? "+" : ""}${integer(score.choiceScoreDelta)}`)));
    append(table, head, rows);
    panel.append(table);
    if (score.epilogue?.length) {
      const list = element("ul", "score-epilogue");
      for (const line of score.epilogue) list.append(element("li", "", line));
      append(panel, element("h3", "", "Along the way"), list);
    }
    return panel;
  }

  function leaderboardPanel(entries) {
    const table = element("table", "score-table leaderboard-table");
    const head = element("thead", "");
    append(head, append(element("tr", ""),
      element("th", "", "Rank"), element("th", "", "Traveler"),
      element("th", "", "Clout"), element("th", "", "Rating")));
    const rows = element("tbody", "");
    for (const [index, entry] of entries.entries()) {
      append(rows, append(element("tr", ""),
        element("td", "", `#${index + 1}`),
        element("td", "", entry.name),
        element("td", "", integer(entry.points)),
        element("td", "", entry.rating)));
    }
    return append(table, head, rows);
  }

  function storeRow(row) {
    const card = element("article", "shop-row");
    card.dataset.nodeKey = row.itemId;
    const header = append(element("div", "shop-row__head"),
      append(element("div", ""), element("h3", "", row.name),
        element("span", "shop-row__price", `${money(row.unitPrice)} each`)),
      element("strong", "shop-row__total", money(row.totalPrice)));
    const controls = element("div", "shop-row__controls");
    const decrease = findAction(row.decreaseActionId);
    const increase = findAction(row.increaseActionId);
    if (decrease) controls.append(actionButton(decrease, { className: "game-button--step", label: "−" }));
    const quantity = element("input", "quantity-input");
    quantity.type = "number";
    quantity.required = true;
    quantity.min = String(row.minQuantity);
    quantity.max = String(row.maxQuantity);
    quantity.value = String(row.quantity);
    quantity.dataset.focusKey = row.setActionId;
    quantity.disabled = busy || !findAction(row.setActionId)?.enabled;
    quantity.setAttribute("aria-label", `${row.name} quantity`);
    const setQuantity = (quantity) => {
      const value = Number(quantity.value);
      if (!quantity.reportValidity() || !Number.isInteger(value) || value < row.minQuantity || value > row.maxQuantity) {
        return;
      }
      delete quantity.dataset.editing;
      if (value !== row.quantity) sendAction(row.setActionId, null, value);
    };
    quantity.oninput = (event) => { event.currentTarget.dataset.editing = "true"; };
    quantity.onchange = (event) => setQuantity(event.currentTarget);
    quantity.onkeydown = (event) => {
      if (event.key === "Enter") { event.preventDefault(); setQuantity(event.currentTarget); }
    };
    controls.append(quantity);
    if (increase) controls.append(actionButton(increase, { className: "game-button--step", label: "+" }));
    const bulk = state.screen.actions.find((action) =>
      action.actionId.startsWith(`store.${row.itemId}.add-`));
    if (bulk) controls.append(actionButton(bulk, {
      className: "game-button--bulk", label: `+${bulk.actionId.split("add-")[1]}`,
    }));
    append(card, header, controls,
      element("p", "shop-row__limit", `Up to ${integer(row.maxQuantity)} within your current budget and cargo space`));
    return card;
  }

  function storeBody(store) {
    const body = element("div", "store-body");
    const heading = append(element("div", "section-header"),
      element("h2", "", "Stock the vehicle"),
      element("span", "", `${store.rows.length} supplies available`));
    const budget = element("div", "shop-budget");
    append(budget,
      labeledValue("Pending", money(store.pendingTotal)),
      labeledValue("Cash left", money(store.balance - store.pendingTotal)),
      labeledValue("Cargo", `${integer(store.cargoWeight)} / ${integer(store.cargoCapacity)} lb`));
    const list = element("div", "shop-list");
    for (const row of store.rows) list.append(storeRow(row));
    append(body, heading, budget, list);
    return body;
  }

  function receiptPanel(store) {
    const card = element("section", "rail-card receipt-card");
    append(card, panelHeading("Travel center / Receipt", "Your loadout"), scene("store"));
    const chosen = store.rows.filter((row) => row.quantity > 0);
    const lines = element("dl", "receipt-lines");
    if (!chosen.length) card.append(element("p", "card-note", "Nothing on the receipt yet."));
    for (const row of chosen) {
      append(lines, element("dt", "", `${row.name} × ${integer(row.quantity)}`),
        element("dd", "", money(row.totalPrice)));
    }
    card.append(lines);
    const totals = element("div", "receipt-totals");
    append(totals,
      labeledValue("Total", money(store.pendingTotal)),
      labeledValue("Cash left", money(store.balance - store.pendingTotal)),
      labeledValue("Cargo", `${integer(store.cargoWeight)} / ${integer(store.cargoCapacity)} lb`));
    card.append(totals);
    const projectedSnacks = (state.hud.foodPoundsRemaining ?? inventoryCount("snacks")) +
      (store.rows.find((row) => row.itemId === "snacks")?.quantity || 0);
    if (state.hud.foodPerDay > 0) card.append(element("p", "card-note",
      `${Math.floor(projectedSnacks / state.hud.foodPerDay)} days of food after checkout at current rations.`));
    const checkout = findAction("store.checkout");
    if (checkout) card.append(actionButton(checkout, { className: "game-button--solid game-button--wide", label: "Buy supplies & leave →" }));
    card.append(element("p", "card-note", "Supplies are loaded when you leave the store."));
    return card;
  }

  function travelBody() {
    const body = element("div", "travel-body");
    body.append(drivingScene());
    append(body, append(element("div", "destination-card"),
      element("span", "eyebrow", "Next leg"),
      element("strong", "destination-card__name", state.progress.nextLocation || "Seattle"),
      element("span", "destination-card__distance", `${integer(state.progress.milesToNextLocation)} miles away`),
      routeProgress(state.progress)));
    const drive = state.screen.actions.find((action) => action.group === "drive") || state.screen.actions[0];
    if (drive) body.append(actionButton(drive, { className: "game-button--drive", label: `${drive.label} →` }));
    for (const [group, title] of [
      ["review", "Check the trip"],
      ["manage", "Set the pace"],
      ["local", "At this stop"],
    ]) {
      const actions = state.screen.actions.filter((action) => action !== drive && action.group === group);
      if (actions.length) append(body,
        element("h2", "section-title", title), simpleActions(actions, "action-list--travel"));
    }
    return body;
  }

  function screenBody() {
    const screen = state.screen;
    if (screen.kind === "crypto" && state.crypto) return window.TrailCrypto.body(state.crypto, cryptoUi());
    if (screen.kind === "creator" && state.creator) return window.TrailCreator.body(state.creator, cryptoUi());
    if (screen.kind === "store" && state.store) return storeBody(state.store);
    if (screen.kind === "travel") return travelBody();
    const body = element("div", `screen-body screen-body--${screen.kind}`);
    if (screen.kind === "river") body.append(scene("river"));
    else if (screen.kind === "event") body.append(scene("event"));
    else if (screen.id.endsWith("gamewin")) body.append(insuranceNotice());
    else if (screen.kind === "game-over") body.append(scene(state.score && state.hud.livingPartyCount > 0 ? "seattle" : "end"));
    else if (screen.id.endsWith("tombstoneview")) body.append(scene("end"));
    else if (screen.kind === "activity") body.append(screen.id.endsWith("crossingtick") ? scene("river") : drivingScene());
    if (screen.story?.length) {
      body.append(storyPanel(screen.story));
      return body;
    }
    if (screen.kind === "activity" && !state.driving?.isDriving) body.append(activityMeter(state.progress) || element("div", "activity-pulse", "In progress"));
    if (screen.kind === "status") {
      if (screen.leaderboard?.length) body.append(leaderboardPanel(screen.leaderboard));
      else if (screen.id.endsWith("lookatmap")) body.append(routePanel(state.progress, true));
      else body.append(cargoPanel());
    }
    if (screen.kind === "game-over") {
      if (state.score) body.append(scorePanel(state.score));
      append(body, append(element("div", "end-stats"),
        labeledValue("Miles traveled", integer(state.progress.milesTraveled)),
        labeledValue("Days on the road", integer(state.hud.turns))));
    }
    if (screen.input) body.append(inputPanel(screen.input));
    const actions = screen.actions.filter((action) =>
      action.actionId !== screen.input?.actionId && action.kind !== "adjust" && action.kind !== "set-value");
    if (actions.length) {
      if (screen.kind === "choice" || (screen.kind === "setup" && actions.some((action) => action.facts?.length))) {
        const choices = actions.filter((action) => action.kind !== "help");
        const guides = actions.filter((action) => action.kind === "help");
        if (choices.length) body.append(choiceActions(choices));
        if (guides.length) body.append(simpleActions(guides, "action-list--helper"));
      } else {
        body.append(simpleActions(actions));
      }
    }
    if (!actions.length && !screen.input && screen.kind === "activity") {
      body.append(element("p", "waiting-note", "The road is moving. The next decision will appear here."));
    }
    return body;
  }

  function cryptoUi() {
    return { element, append, actionButton, findAction, sendAction, money, integer, labeledValue, panelHeading,
      balance: state.hud.balance, busy: busy || !online, motion: effects && !reducedMotion.matches && online,
      journeyId: state.journeyId, refresh: () => render(false) };
  }

  function homeScreen() {
    const main = element("main", "home-screen");
    main.id = "main-content";
    const intro = element("section", "home-intro");
    intro.setAttribute("aria-labelledby", "screen-title");
    append(intro, element("p", "eyebrow", "An American survival story / Est. 2028"));
    const title = element("h1", "home-title");
    title.id = "screen-title";
    title.tabIndex = -1;
    title.dataset.screenFocus = "";
    append(title, element("span", "home-title__the", "THE"), element("span", "", "ASPHALT"),
      append(element("span", ""), document.createTextNode("TRAIL"), element("span", "cursor", "_")));
    append(intro, title,
      element("p", "home-tagline", "The American dream has a check-engine light."),
      element("p", "home-copy", "Florida is uninsurable. Seattle is uphill from the sea. Load the car, bring your people, and try to make it across the country in one piece."));
    const choices = simpleActions(state.screen.actions, "action-list--home");
    choices.setAttribute("aria-label", "Main menu");
    intro.append(choices);
    const vista = element("section", "home-vista");
    vista.setAttribute("aria-label", "Cape Coral to Seattle, a one-way journey");
    append(vista,
      append(element("div", "window-bar"), element("span", "", "WINDSHIELD / LOOKING NORTHWEST"), element("span", "", "[ 01 ]")),
      scene("road", "NO FAST TRAVEL. NO GUARANTEES."),
      append(element("div", "route-ticket"), labeledValue("Depart", "Cape Coral, FL"),
        element("span", "route-ticket__arrow", "- - - >"), labeledValue("Arrive", "Seattle, WA")),
      append(element("div", "home-vista__meta"), element("span", "", `~${integer(state.progress.totalMiles)} MILES`), element("span", "", "ONE WAY / 2028")));
    append(main, intro, vista);
    return main;
  }

  function controlsDialog() {
    const dialog = element("dialog", "controls-dialog");
    dialog.setAttribute("aria-labelledby", "controls-title");
    append(dialog, element("span", "eyebrow", "Operator's field guide"), element("h2", "", "Keep your eyes on the road."));
    dialog.querySelector("h2").id = "controls-title";
    const controls = element("dl", "controls-list");
    for (const [key, label] of [["1–9", "Choose a numbered command"], ["↑ / ↓", "Move between available commands"], ["Enter", "Activate a focused command or submit a form"], ["Tab", "Move through every control"], ["?", "Open this guide"], ["Esc", "Close this guide"]]) {
      append(controls, element("dt", "", key), element("dd", "", label));
    }
    append(dialog, controls,
      element("p", "card-note", "Fuel keeps you moving. Snacks keep your party alive. Watch the cargo limit, rest when you need to, and read the consequences before choosing."),
      element("p", "card-note", "Your journey stays with this browser while the server session lasts. There is no permanent save; a service restart ends the journey."));
    const close = () => { helpOpen = false; pendingFocusKey = helpReturnKey || "controls"; render(false); };
    dialog.oncancel = (event) => { event.preventDefault(); close(); };
    dialog.append(localButton("[ Return to the road ]", "close-controls", close, "local-button--accent"));
    return dialog;
  }

  function numberCommands(app) {
    const buttons = [...app.querySelectorAll("[data-action-id]")].filter((button) => {
      const action = findAction(button.dataset.actionId);
      return action && !button.dataset.noShortcut && !["adjust", "set-value", "bulk"].includes(action.kind);
    });
    buttons.slice(0, 9).forEach((button, index) => {
      button.dataset.shortcut = String(index + 1);
      button.setAttribute("aria-keyshortcuts", String(index + 1));
      const marker = element("span", "command-key", `[${index + 1}]`);
      marker.setAttribute("aria-hidden", "true");
      button.prepend(marker);
    });
  }

  // Keep live controls and their focus stable as the simulation ticks. Event properties are
  // refreshed with each snapshot so row controls always use the latest legal quantities.
  function reconcile(current, next) {
    const key = (node) => node.nodeType === Node.ELEMENT_NODE
      ? node.dataset.nodeKey || node.dataset.focusKey || "" : "";
    const compatible = (a, b) => a && a.nodeType === b.nodeType && a.nodeName === b.nodeName && key(a) === key(b);
    if (!compatible(current, next)) { current.replaceWith(next); return; }
    if (next.nodeType === Node.TEXT_NODE) {
      if (current.textContent !== next.textContent) current.textContent = next.textContent;
      return;
    }
    const editing = current instanceof HTMLInputElement && current.dataset.editing && current === document.activeElement;
    for (const name of current.getAttributeNames()) {
      if (!next.hasAttribute(name) && name !== "open" && !(editing && name === "data-editing")) current.removeAttribute(name);
    }
    for (const name of next.getAttributeNames()) {
      if (current.getAttribute(name) !== next.getAttribute(name)) current.setAttribute(name, next.getAttribute(name));
    }
    for (const name of ["onclick", "oninput", "onchange", "onkeydown", "onsubmit", "oncancel"]) current[name] = next[name];
    if (current instanceof HTMLInputElement && !editing && current.value !== next.value) current.value = next.value;
    const children = [...next.childNodes];
    children.forEach((child, index) => {
      let existing = current.childNodes[index];
      if (key(child) && !compatible(existing, child)) {
        const keyed = [...current.childNodes].find((candidate) => compatible(candidate, child));
        if (keyed) { current.insertBefore(keyed, existing || null); existing = keyed; }
      }
      if (existing) reconcile(existing, child);
      else current.append(child);
    });
    while (current.childNodes.length > children.length) current.lastChild.remove();
  }

  function render(focusHeading, creatorFocus = null) {
    if (!state) return;
    const active = document.activeElement;
    const focusKey = pendingFocusKey || active?.dataset?.focusKey;
    const selection = active instanceof HTMLInputElement
      ? { start: active.selectionStart, end: active.selectionEnd } : null;
    const screen = state.screen;
    const home = isHome();
    const app = element("div", `game game--${screen.kind}${home ? " game--home" : ""}`);
    const header = element("header", "topbar");
    const logo = element("span", "brand-mark", ">_");
    logo.setAttribute("aria-hidden", "true");
    append(header,
      append(element("div", "topbar__brand"), logo,
        append(element("div", ""),
          element("span", "topbar__title", "ASPHALT TRAIL"),
          element("span", "topbar__kicker", "Roadside survival terminal"))),
      append(element("div", "topbar__right"),
        element("span", `connection ${online ? "" : "connection--offline"}`, online ? "CONNECTED" : "RECONNECTING"),
        localButton("CRT", "effects", () => {
          effects = !effects;
          document.documentElement.dataset.effects = effects ? "on" : "off";
          try { localStorage.setItem("asphalt-effects", effects ? "on" : "off"); } catch { /* Optional preference. */ }
          render(false);
        }, "effects-toggle"),
        localButton("[ ? ]", "controls", () => { helpReturnKey = "controls"; helpOpen = true; render(false); })));
    header.querySelector(".effects-toggle").setAttribute("aria-label", "CRT scanlines and ASCII animation");
    header.querySelector(".effects-toggle").setAttribute("aria-pressed", String(effects));
    header.querySelector('[data-focus-key="controls"]').setAttribute("aria-label", "Keyboard controls and field guide");
    app.append(header);
    const tripStarted = screen.kind !== "setup" && !screen.id.includes("mainmenu");
    if (tripStarted && !screen.leaderboard) {
      const strip = element("div", "status-strip");
      append(strip,
        labeledValue("Current location", state.hud.locationName || "On the road"),
        labeledValue("Vehicle", state.hud.vehicleName || "—"),
        labeledValue("Date / weather", `${state.hud.date} / ${state.hud.weather || "—"}`),
        labeledValue("Cash", money(state.hud.balance)));
      app.append(strip);
    }
    if (screen.kind === "setup" && !home) app.append(setupSteps());
    const layout = element("main", "game-layout");
    layout.id = "main-content";
    const play = element("section", "play-panel");
    play.dataset.nodeKey = screen.id;
    play.setAttribute("aria-busy", String(busy));
    play.setAttribute("aria-labelledby", "screen-title");
    const intro = element("div", "play-intro");
    append(intro,
      element("span", "eyebrow", ({ setup: "Departure", travel: "On the road", store: "Travel center", event: "Road event", choice: "Decision", river: "Crossing", activity: "In motion", status: "Trip record", crypto: "Side hustle / Financial ruin simulator", creator: "Side hustle / A travel-channel career", "game-over": "Journey end" })[screen.kind] || "On the trail"),
      element("h1", "play-title", screen.title),
      description(screen.description));
    intro.querySelector("h1").id = "screen-title";
    intro.querySelector("h1").tabIndex = -1;
    intro.querySelector("h1").dataset.screenFocus = "";
    play.append(intro);
    if (state.responseMessage) {
      const alert = element("p", "response-message", state.responseMessage);
      alert.setAttribute("role", "alert");
      play.append(alert);
    }
    play.append(screenBody());
    const rail = element("aside", "game-rail");
    rail.setAttribute("aria-label", "Journey details");
    if (!tripStarted || screen.leaderboard) {
      append(rail, setupPanel(), state.party.length ? partyPanel() : null);
    } else if (screen.kind === "store" && state.store) {
      append(rail, receiptPanel(state.store), readinessPanel(), partyPanel(true));
    } else if (screen.kind === "crypto" && state.crypto) {
      append(rail, window.TrailCrypto.rail(state.crypto, cryptoUi()), readinessPanel());
    } else if (screen.kind === "creator" && state.creator) {
      append(rail, window.TrailCreator.rail(state.creator, cryptoUi()), readinessPanel());
    } else {
      append(rail, routePanel(state.progress), readinessPanel(), partyPanel(true), cargoPanel(true));
    }
    append(layout, play, rail);
    app.append(home ? homeScreen() : layout);
    if (home && state.responseMessage) {
      const alert = element("p", "response-message", state.responseMessage);
      alert.setAttribute("role", "alert");
      app.append(alert);
    }
    numberCommands(app);
    const footer = element("footer", "game-footer");
    append(footer, element("span", "", "FL → WA / THE LONG WAY HOME"),
      element("span", "keyboard-hint", "[1–9] select   [↑↓] navigate   [?] controls"),
      element("span", `terminal-status ${busy ? "is-busy" : ""}`, busy ? "PROCESSING..."
        : screen.kind === "activity" && !screen.input ? "JOURNEY IN PROGRESS_" : "AWAITING INPUT_"));
    app.append(footer);
    if (helpOpen) app.append(controlsDialog());
    if (root.firstElementChild?.classList.contains("game")) reconcile(root.firstElementChild, app);
    else root.replaceChildren(app);
    window.TrailCrypto.sync(state.crypto, effects && !reducedMotion.matches && online, state.journeyId);
    if (helpOpen) {
      const dialog = root.querySelector("dialog");
      if (!dialog.open) dialog.showModal();
    } else if (focusHeading) {
      const cryptoResult = state.crypto?.phase === "result";
      const creatorHeading = creatorFocus ? root.querySelector(creatorFocus) : null;
      announcement.textContent = creatorHeading ? creatorHeading.textContent : cryptoResult
        ? `Coin settled. ${state.crypto.receipt.net >= 0 ? "Profit" : "Loss"}: ${money(Math.abs(state.crypto.receipt.net))}.`
        : screen.title;
      requestAnimationFrame(() => {
        const heading = creatorHeading || (cryptoResult ? root.querySelector("#crypto-result-title") : root.querySelector("[data-screen-focus]"));
        heading?.focus({ preventScroll: true });
        if (heading && heading.getBoundingClientRect().top < 0) heading.scrollIntoView({ block: "start" });
        else if (creatorHeading && creatorHeading.getBoundingClientRect().bottom > innerHeight)
          creatorHeading.scrollIntoView({ block: "nearest" });
      });
    } else if (focusKey) {
      const match = [...root.querySelectorAll("[data-focus-key]")]
        .find((candidate) => candidate.dataset.focusKey === focusKey);
      if (match && !match.disabled) {
        match.focus({ preventScroll: true });
        if (selection && match instanceof HTMLInputElement && match.type !== "number") {
          match.setSelectionRange(selection.start, selection.end);
        }
      }
    }
    if (!busy) pendingFocusKey = null;
  }

  document.addEventListener("keydown", (event) => {
    if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || event.repeat || !state) return;
    const active = document.activeElement;
    if (active?.matches("input, textarea, select, [contenteditable=true]") || helpOpen) return;
    if (event.key === "?") {
      event.preventDefault();
      helpReturnKey = active?.dataset?.focusKey;
      helpOpen = true;
      render(false);
      return;
    }
    if (busy) return;
    const commands = [...root.querySelectorAll("[data-shortcut]:not(:disabled)")];
    const chosen = commands.find((button) => button.dataset.shortcut === event.key);
    if (chosen) { event.preventDefault(); chosen.click(); }
    else if (["ArrowDown", "ArrowUp"].includes(event.key) && commands.length) {
      event.preventDefault();
      const index = commands.indexOf(active);
      const next = index < 0 ? (event.key === "ArrowDown" ? 0 : commands.length - 1)
        : (index + (event.key === "ArrowDown" ? 1 : -1) + commands.length) % commands.length;
      commands[next].focus();
    }
  });

  live = api.watch((incoming) => {
    const next = parseState(incoming);
    if (next) acceptState(next);
  }, showError, (connected) => {
    if (online === connected) return;
    online = connected;
    render(false);
  });
  window.addEventListener("pagehide", () => live.stop());
  window.addEventListener("pageshow", (event) => { if (event.persisted) window.location.reload(); });
})();
