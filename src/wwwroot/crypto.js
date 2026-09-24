(() => {
  "use strict";

  const price = (value) => `$${Number(value).toFixed(5)}`;
  const signed = (value, money) => `${value >= 0 ? "+" : "−"}${money(Math.abs(value))}`;
  const svg = (tag, attributes = {}, text = null) => {
    const node = document.createElementNS("http://www.w3.org/2000/svg", tag);
    for (const [key, value] of Object.entries(attributes)) node.setAttribute(key, String(value));
    if (text !== null) node.textContent = text;
    return node;
  };

  function chart(data, ui) {
    const { element: el, append, integer } = ui;
    const market = data.market;
    const candles = data.candles;
    const figure = el("figure", "crypto-chart");
    const change = (market.price / .01 - 1) * 100;
    append(figure, append(el("div", "crypto-chart__heading"),
      append(el("div"), el("span", "eyebrow", `${data.symbol} / USD · SIMULATED MARKET`),
        append(el("div", "crypto-price-row"), el("strong", "crypto-price", price(market.price)),
          el("span", change >= 0 ? "crypto-positive" : "crypto-negative", `${change >= 0 ? "+" : ""}${change.toFixed(1)}%`))),
      el("span", `crypto-live ${data.phase === "live" ? "is-live" : ""}`, ({ live: "● LIVE", exiting: "◌ EXIT PENDING", result: "■ CLOSED" })[data.phase])));

    const plot = el("div", "crypto-plot");
    const graph = svg("svg", { viewBox: "0 0 720 282", role: "img",
      "aria-label": `${data.name} price chart. ${candles.length} observations. Current price ${price(market.price)}, ${change.toFixed(1)} percent from launch.` });
    const defs = svg("defs");
    const gradient = svg("linearGradient", { id: "crypto-fill", x1: "0", y1: "0", x2: "0", y2: "1" });
    gradient.append(svg("stop", { offset: "0%", "stop-color": "#c4ff73", "stop-opacity": ".3" }),
      svg("stop", { offset: "100%", "stop-color": "#c4ff73", "stop-opacity": "0" }));
    defs.append(gradient);
    graph.append(defs);
    const low = Math.min(.01, ...candles.map(c => c.low)) * .87;
    const high = Math.max(.01, ...candles.map(c => c.high)) * 1.13;
    const x = (tick) => 66 + 635 * tick / data.duration;
    const y = (value) => 204 - 181 * (value - low) / (high - low);
    for (let i = 0; i <= 4; i++) {
      const value = low + (high - low) * i / 4;
      graph.append(svg("line", { x1: 66, x2: 701, y1: y(value), y2: y(value), class: "crypto-gridline" }),
        svg("text", { x: 57, y: y(value) + 4, "text-anchor": "end", class: "crypto-axis" }, price(value)));
    }
    for (const tick of [0, 30, 60, 90, 120]) graph.append(
      svg("line", { x1: x(tick), x2: x(tick), y1: 23, y2: 251, class: "crypto-gridline" }),
      svg("text", { x: x(tick), y: 271, "text-anchor": "middle", class: "crypto-axis" }, `${tick}s`));
    graph.append(svg("line", { x1: 66, x2: 701, y1: y(.01), y2: y(.01), class: "crypto-launch-line" }));
    const points = candles.map(c => `${x(c.tick).toFixed(2)},${y(c.close).toFixed(2)}`).join(" ");
    const last = candles.at(-1);
    if (last) {
      const area = `M66,204 L${points.replaceAll(" ", " L")} L${x(last.tick)},204 Z`;
      graph.append(svg("path", { d: area, fill: "url(#crypto-fill)" }));
      const maxVolume = Math.max(1, ...candles.map(c => c.volume));
      for (const candle of candles) {
        const rising = candle.close >= candle.open;
        const color = rising ? "#b6f776" : "#ff8b9e";
        graph.append(svg("line", { x1: x(candle.tick), x2: x(candle.tick), y1: y(candle.high), y2: y(candle.low), stroke: color, "stroke-width": 1 }),
          svg("rect", { x: x(candle.tick) - 1.5, y: Math.min(y(candle.open), y(candle.close)), width: 3,
            height: Math.max(1, Math.abs(y(candle.open) - y(candle.close))), fill: color }),
          svg("rect", { x: x(candle.tick) - 1.5, y: 250 - candle.volume / maxVolume * 28, width: 3,
            height: Math.max(1, candle.volume / maxVolume * 28), fill: color, opacity: .45 }));
      }
      graph.append(svg("polyline", { points, class: "crypto-price-line" }),
        svg("circle", { cx: x(last.tick), cy: y(last.close), r: 4, class: "crypto-price-dot" }));
    }
    const sparks = el("canvas", "crypto-sparks");
    sparks.setAttribute("aria-hidden", "true");
    sparks.dataset.nodeKey = "crypto-sparks";
    append(plot, graph, sparks);
    append(figure, plot, append(el("figcaption", "crypto-chart__caption"),
      el("span", "", `1s candles · ${integer(market.holders)} holders`),
      el("span", "", "Dotted line = launch price · Bars = volume")));
    return figure;
  }

  function meter(label, value, tone, ui) {
    const { element: el, append } = ui;
    const item = el("div", `crypto-meter crypto-meter--${tone}`);
    append(item, append(el("div", "crypto-meter__label"), el("span", "", label), el("strong", "", `${Math.round(value)} / 100`)));
    const track = el("div", "crypto-meter__track");
    track.setAttribute("role", "meter");
    track.setAttribute("aria-label", label);
    track.setAttribute("aria-valuenow", String(Math.round(value)));
    track.setAttribute("aria-valuemin", "0");
    track.setAttribute("aria-valuemax", "100");
    const fill = el("span");
    fill.style.width = `${Math.max(0, Math.min(100, value))}%`;
    append(item, append(track, fill));
    return item;
  }

  function lobby(data, ui) {
    const { element: el, append, actionButton, findAction, sendAction, money, balance, busy } = ui;
    const body = el("div", "crypto-lobby");
    append(body, append(el("div", "crypto-banner"), el("span", "crypto-banner__logo", "↗"),
      append(el("div"), el("span", "eyebrow", "THE PARKING-LOT LAUNCHPAD"),
        el("h2", "", "Your next terrible idea."),
        el("p", "", "120 seconds. One founder wallet. A community of future bagholders."))));
    const form = el("form", "crypto-name");
    const label = el("label", "eyebrow", "01 / Name your coin");
    label.htmlFor = "crypto-name";
    const input = el("input");
    input.id = "crypto-name";
    input.name = "coin";
    input.value = data.name;
    input.required = true;
    input.maxLength = 24;
    input.pattern = "[A-Za-z0-9 \\-]*[A-Za-z0-9][A-Za-z0-9 \\-]*";
    input.title = "1–24 letters, numbers, spaces or hyphens";
    input.autocomplete = "off";
    input.dataset.focusKey = "crypto-coin-name";
    input.disabled = busy;
    input.oninput = event => { event.currentTarget.dataset.editing = "true"; };
    const save = el("button", "game-button", "Set name");
    save.type = "submit";
    save.dataset.focusKey = "crypto-save-name";
    save.disabled = busy;
    form.onsubmit = event => {
      event.preventDefault();
      const field = event.currentTarget.elements.coin;
      if (event.currentTarget.reportValidity()) {
        delete field.dataset.editing;
        sendAction("crypto.rename", field.value);
      }
    };
    append(body, append(form, label, append(el("div", "crypto-name__row"), input, save)),
      el("h3", "eyebrow", "02 / Pick your narrative"));
    const narratives = el("div", "crypto-narratives");
    for (const narrative of data.narratives) {
      const button = actionButton(findAction(narrative.actionId), { className: "crypto-choice" });
      button.setAttribute("aria-pressed", String(data.narrativeId === narrative.id));
      append(button, el("small", "", narrative.detail));
      narratives.append(button);
    }
    append(body, narratives, el("h3", "eyebrow", "03 / Put road money on the line"));
    const stakes = el("div", "crypto-stakes");
    for (const funding of data.funding) {
      const button = actionButton(findAction(funding.actionId), { className: "crypto-stake", label: money(funding.amount) });
      button.setAttribute("aria-pressed", String(data.stake === funding.amount));
      stakes.append(button);
    }
    append(body, stakes, append(el("div", "crypto-budget"),
      ui.labeledValue("Seed capital", money(data.stake)), ui.labeledValue("Launch fee", money(data.launchFee)),
      ui.labeledValue("Road cash after launch", money(balance - data.stake - data.launchFee))));
    append(body, el("p", "crypto-warning", "Most launches lose money. Hype costs extra. Every launched coin costs one trail day when you return; browsing is free."),
      actionButton(findAction("crypto.launch"), { className: "game-button--solid game-button--wide crypto-launch" }),
      el("p", "card-note", "Your coin starts at $0.0100. You own the seed allocation; the pool determines what you can actually withdraw. Choose a campaign, watch it land, then time your exit."));
    if (!findAction("crypto.launch").enabled) body.append(el("p", "crypto-negative", `You need ${money(data.stake + data.launchFee)} to launch. Choose a smaller stake or earn more cash.`));
    body.append(actionButton(findAction("crypto.leave")));
    return body;
  }

  function receipt(data, ui) {
    const { element: el, append, money, labeledValue, actionButton, findAction } = ui;
    const record = data.receipt;
    const won = record.net > 0;
    const result = el("section", `crypto-result ${won ? "is-profit" : "is-loss"}`);
    result.setAttribute("aria-labelledby", "crypto-result-title");
    const title = el("h2", "", won ? "The rug came with a receipt." : "Congratulations. You funded the ecosystem.");
    title.id = "crypto-result-title";
    title.tabIndex = -1;
    append(result, el("span", "eyebrow", "FINAL SETTLEMENT"), title,
      el("strong", "crypto-result__net", signed(record.net, money)),
      el("p", "card-note", ({ pulled: "Your withdrawal cleared after three market ticks.",
        collapsed: "Liquidity vanished before you could get out.", expired: "The 120-second listing expired. The remaining position sold at a steep discount." })[record.reason]));
    const ledger = el("dl", "crypto-ledger");
    for (const [label, value] of [["Seed capital", -record.stake], ["Launch fee", -record.launchFee],
      ["Hype spending", -record.marketing], ["Cash returned (after exit costs)", record.returned]])
      append(ledger, el("dt", "", label), el("dd", "", signed(value, money)));
    append(result, ledger, append(el("div", "crypto-budget"),
      labeledValue("Best quoted exit", money(record.bestExit)),
      labeledValue("Quote when you clicked", record.reason === "pulled" ? money(record.requestedExit) : "No completed pull"),
      labeledValue("Time in the market", `${record.duration}s`)),
      el("p", "card-note", "Best quote is hindsight, not a guaranteed fill. Fees, campaigns and the settlement delay all count against your net."),
      actionButton(findAction("crypto.leave"), { className: "game-button--solid game-button--wide" }));
    return result;
  }

  function body(data, ui) {
    const { element: el, append, labeledValue, money, actionButton, findAction } = ui;
    const page = el("div", "crypto-desk");
    page.dataset.phase = data.phase;
    page.dataset.motion = ui.motion ? "on" : "off";
    page.dataset.heat = data.market.hype > 60 ? "hot" : "warm";
    if (data.phase === "lobby") return append(page, lobby(data, ui));
    const m = data.market;
    const banner = el("div", `crypto-signal crypto-signal--${data.effect.kind}`);
    banner.dataset.nodeKey = `effect-${data.effect.id}`;
    append(banner, el("span", "crypto-signal__icon", data.effect.kind === "dump" || data.effect.kind === "loss" ? "↘" : "↗"),
      el("strong", "", data.effect.text), el("span", "crypto-countdown", `${m.remaining}s`));
    append(page, append(el("div", "crypto-session-head"), el("h2", "", data.name),
      el("span", "eyebrow", `${m.elapsed} / ${data.duration} SECONDS`)), banner);
    if (data.receipt) page.append(receipt(data, ui));
    page.append(chart(data, ui));
    if (!data.receipt) {
      const exit = el("section", "crypto-exit");
      const net = m.exitQuote - m.totalSpent;
      append(exit, append(el("div", "crypto-exit__quote"), labeledValue("Estimated cash back", money(m.exitQuote)),
        labeledValue("Net after all spending", signed(net, money), net >= 0 ? "crypto-positive" : "crypto-negative")));
      if (data.phase === "live") exit.append(actionButton(findAction("crypto.pull-out"), { className: "crypto-rug-button" }));
      else exit.append(el("strong", "crypto-settling", `WITHDRAWAL PENDING · ${m.exitRemaining}s`));
      append(exit, el("p", "", data.phase === "live"
        ? "3-second settlement. The market keeps moving. This quote includes price impact and the $3 exit fee."
        : "Your sell is public. Other holders can get out first. The final payout can still fall."));
      page.append(exit);
    }
    append(page, append(el("div", "crypto-stats"), labeledValue("On-paper bag", money(m.paperValue)),
      labeledValue("Pool liquidity", money(m.liquidity)), labeledValue("Total spent", money(m.totalSpent))),
      append(el("div", "crypto-meters"), meter("Hype", m.hype, "hype", ui),
        meter("Credibility", m.trust, "trust", ui), meter("Wallet suspicion", m.heat, "heat", ui)));
    if (data.phase === "live" || data.phase === "exiting") {
      const campaign = el("section", "crypto-campaigns");
      append(campaign, append(el("div", "section-header"), el("h2", "", "Manufacture the hype"), el("span", "", "One campaign at a time")));
      const work = el("div", "crypto-work");
      if (data.work) {
        append(work, el("strong", "", `${data.work.name} · lands in ${data.work.remaining}s`));
        const fill = el("span", "crypto-work__fill");
        fill.style.width = `${100 * (1 - data.work.remaining / data.work.duration)}%`;
        work.append(append(el("div", "crypto-work__track"), fill));
      } else work.append(el("span", "", data.phase === "exiting" ? "Campaign desk closed. Waiting for settlement." : "Attention is decaying. Pick a move—or take the exit."));
      campaign.append(work);
      const grid = el("div", "crypto-campaign-grid");
      for (const [index, c] of data.campaigns.entries()) {
        const action = findAction(c.actionId);
        const card = el("article", "crypto-campaign");
        card.dataset.nodeKey = c.id;
        card.dataset.active = String(data.work?.id === c.id);
        append(card, append(el("div", "crypto-campaign__top"), el("span", "crypto-campaign__icon", ["✦", "▥", "↗", "◉", "≡", "♨"][index]),
          el("span", "", `${money(c.cost)} / ${c.duration}s`)), el("h3", "", c.name), el("p", "", c.detail));
        if (action) {
          const button = actionButton(action, { label: action.enabled ? "Run campaign →" : c.status, className: "crypto-campaign__button" });
          button.setAttribute("aria-label", `${c.name}: ${action.enabled ? "Run campaign" : c.status}`);
          card.append(button);
        }
        else card.append(el("span", "card-note", "Exchange settling"));
        grid.append(card);
      }
      append(page, append(campaign, grid));
    }
    const feed = el("section", "crypto-feed");
    append(feed, el("h2", "section-title", "The extremely normal market feed"));
    const tape = el("ol");
    for (const news of data.news) {
      const row = el("li", `crypto-news crypto-news--${news.tone}`);
      row.dataset.nodeKey = `news-${news.id}`;
      append(row, el("time", "", `T+${String(news.tick).padStart(3, "0")}`), el("span", "", news.text));
      tape.append(row);
    }
    append(page, append(feed, tape));
    return page;
  }

  function rail(data, ui) {
    const { element: el, append, labeledValue, money } = ui;
    const card = el("section", "rail-card crypto-career");
    append(card, ui.panelHeading("Founder dossier / This trip", "Serial entrepreneur"),
      append(el("div", "crypto-career__stats"), labeledValue("Coins launched", data.career.launches),
        labeledValue("Profitable exits", data.career.wins), labeledValue("Lifetime net", signed(data.career.net, money)),
        labeledValue("Notoriety", `${data.career.notoriety} / 40`)),
      el("p", "card-note", "The internet remembers. Each finished launch raises suspicion at your next launch."));
    if (data.career.history.length) {
      const history = el("ul", "crypto-history");
      for (const [index, record] of data.career.history.entries()) {
        const row = el("li");
        row.dataset.nodeKey = `history-${index}`;
        append(row, el("span", "", record.name), el("strong", record.net > 0 ? "crypto-positive" : "crypto-negative", signed(record.net, money)));
        history.append(row);
      }
      card.append(history);
    }
    const guide = el("div", "crypto-primer");
    append(guide, el("h3", "", "Read the room"));
    for (const [heading, copy] of [
      ["Price ≠ payout", "Your entire bag cannot sell at the last traded price. Watch the cash-back estimate."],
      ["Timing has a cost", "Hype takes seconds to land. Repeat campaigns lose novelty. Every dollar comes out of road cash."],
      ["The exit is public", "Pulling out takes three seconds. Whales, bots and panicking holders may sell first."],
      ["There is a closing bell", "At 120 seconds the listing expires and your bag is sold at a steep discount."],
    ]) append(guide, el("strong", "", heading), el("p", "card-note", copy));
    append(card, guide, el("p", "crypto-fiction", "Fictional exchange. Real consequences for your imaginary road trip. Records last for this journey."));
    return card;
  }

  // Decorative particles stay local; prices and payouts always come from the server.
  let particles = [], frame = 0, previous = 0, lastEmission = 0, effectKey = "", config = null, canvas = null;
  function emit(count, kind, width, height) {
    const negative = ["dump", "loss"].includes(kind);
    const palette = negative ? ["#ff7896", "#ffb46e", "#e75172"] : ["#c4ff73", "#f7e98a", "#6fffe9", "#ffffff"];
    const origin = Math.min(.93, .1 + (config?.market.elapsed || 0) / 120 * .84) * width;
    for (let i = 0; i < count && particles.length < 200; i++) {
      const angle = Math.random() * Math.PI * 2;
      const speed = 25 + Math.random() * 155;
      particles.push({ x: origin, y: height * .35, vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed - (negative ? -25 : 55), life: 1, decay: .45 + Math.random() * .65,
        color: palette[i % palette.length], size: 1 + Math.random() * 2.5, gravity: negative ? 70 : 30 });
    }
  }
  function animate(now) {
    frame = 0;
    if (!config || !canvas?.isConnected || document.hidden || !config.motion) { particles = []; return; }
    const context = canvas.getContext("2d");
    if (!context) return;
    const bounds = canvas.getBoundingClientRect();
    const scale = Math.min(window.devicePixelRatio || 1, 2);
    if (canvas.width !== Math.round(bounds.width * scale) || canvas.height !== Math.round(bounds.height * scale)) {
      canvas.width = Math.round(bounds.width * scale);
      canvas.height = Math.round(bounds.height * scale);
    }
    const delta = Math.min(.05, (now - (previous || now)) / 1000);
    previous = now;
    context.setTransform(scale, 0, 0, scale, 0, 0);
    context.clearRect(0, 0, bounds.width, bounds.height);
    if (config.phase !== "result" && now - lastEmission > 160 - config.market.hype) {
      emit(config.market.hype > 55 ? 3 : 1, config.phase === "exiting" ? "dump" : "pump", bounds.width, bounds.height);
      lastEmission = now;
    }
    context.globalCompositeOperation = "lighter";
    for (const particle of particles) {
      const oldX = particle.x, oldY = particle.y;
      particle.life -= particle.decay * delta;
      particle.vy += particle.gravity * delta;
      particle.x += particle.vx * delta;
      particle.y += particle.vy * delta;
      context.globalAlpha = Math.max(0, particle.life);
      context.strokeStyle = particle.color;
      context.lineWidth = particle.size;
      context.beginPath();
      context.moveTo(oldX - particle.vx * .025, oldY - particle.vy * .025);
      context.lineTo(particle.x, particle.y);
      context.stroke();
    }
    particles = particles.filter(p => p.life > 0);
    context.globalAlpha = 1;
    context.globalCompositeOperation = "source-over";
    if (config.phase !== "result" || particles.length) frame = requestAnimationFrame(animate);
  }
  function sync(data, motion, journeyId) {
    config = data ? { ...data, motion } : null;
    canvas = document.querySelector(".crypto-sparks");
    if (!canvas || !motion || !data || data.phase === "lobby") {
      cancelAnimationFrame(frame); frame = 0; previous = 0; particles = [];
      canvas?.getContext("2d")?.clearRect(0, 0, canvas.width, canvas.height);
      if (!data) effectKey = "";
      return;
    }
    const launch = data.career.launches + (data.phase === "result" ? 0 : 1);
    const key = `${journeyId}:${launch}:${data.effect.id}`;
    if (effectKey !== key) {
      effectKey = key;
      const bounds = canvas.getBoundingClientRect();
      emit(data.effect.kind === "work" ? 18 : data.effect.kind === "win" ? 150 : 90,
        data.effect.kind, bounds.width, bounds.height);
    }
    if (!frame) { previous = 0; frame = requestAnimationFrame(animate); }
  }
  document.addEventListener("visibilitychange", () => {
    if (document.hidden) { cancelAnimationFrame(frame); frame = 0; particles = []; canvas?.getContext("2d")?.clearRect(0, 0, canvas.width, canvas.height); }
    else if (config?.motion && !frame && canvas?.isConnected) { previous = 0; frame = requestAnimationFrame(animate); }
  });
  window.TrailCrypto = { body, rail, sync };
})();
