(() => {
  "use strict";

  const categories = [
    ["all", "Everything"], ["camera", "Cameras"], ["light", "Lighting"],
    ["storage", "Cards & storage"], ["power", "Batteries"], ["monitor", "Monitors"],
    ["lens", "Lenses"], ["filter", "ND filters"], ["audio", "Audio"], ["support", "Support"],
  ];
  let category = "all", journey = null, nameDraft = null;
  const signed = (value, money) => `${value < 0 ? "−" : "+"}${money(Math.abs(value))}`;
  const svg = (tag, attributes = {}, text = null) => {
    const node = document.createElementNS("http://www.w3.org/2000/svg", tag);
    for (const [key, value] of Object.entries(attributes)) node.setAttribute(key, String(value));
    if (text !== null) node.textContent = text;
    return node;
  };

  function command(id, ui, options = {}) {
    const action = ui.findAction(id);
    if (!action) return null;
    const button = ui.actionButton(action, options);
    if (options.noShortcut) button.dataset.noShortcut = "true";
    button.disabled ||= ui.busy;
    return button;
  }

  function section(title, kicker, ui, className = "") {
    const card = ui.element("section", `creator-section ${className}`);
    ui.append(card, ui.element("span", "eyebrow", kicker), ui.element("h2", "", title));
    return card;
  }

  function cameraMark(ui) {
    const frame = ui.element("div", "creator-viewfinder");
    frame.setAttribute("aria-hidden", "true");
    const road = svg("svg", { viewBox: "0 0 300 140", fill: "none" });
    road.append(svg("path", { d: "M0 88H300 M0 82L45 45L75 72L128 24L187 73L229 34L300 78", stroke: "currentColor", "stroke-width": 1.5 }),
      svg("path", { d: "M109 140L147 88H153L201 140 M152 140L150 129 M150 119V111 M150 104V99", stroke: "currentColor", "stroke-width": 2 }),
      svg("circle", { cx: 220, cy: 27, r: 12, stroke: "currentColor" }));
    ui.append(frame, ui.element("span", "creator-rec", "● REC"), road,
      ui.element("span", "creator-viewfinder__time", "00:00:00"), ui.element("span", "creator-viewfinder__label", "A ROAD. A PHONE. A BUSINESS EXPENSE."));
    return frame;
  }

  function onboarding(data, ui) {
    const { element: el, append, money } = ui;
    const page = el("div", "creator-onboarding");
    append(page, cameraMark(ui), append(el("div", "creator-manifesto"),
      el("span", "eyebrow", "THE NEXT BIG THING / PROBABLY NOT"),
      el("h2", "", "Turn your escape into content."),
      el("p", "", "Florida is uninsurable. Seattle is still miles away. Surely strangers will pay to watch your family get there.")));
    const form = el("form", "creator-name");
    form.dataset.nodeKey = "creator-onboarding-form";
    const label = el("label", "eyebrow", "Your YouTube travel channel");
    label.htmlFor = "creator-channel-name";
    const input = el("input", "game-input");
    input.id = "creator-channel-name";
    input.name = "channel";
    input.value = nameDraft ?? data.name;
    input.required = true;
    input.minLength = 1;
    input.maxLength = 32;
    input.autocomplete = "off";
    input.disabled = ui.busy;
    input.dataset.focusKey = "creator-channel-name";
    input.setAttribute("aria-describedby", "creator-name-hint");
    input.oninput = event => {
      const field = event.currentTarget;
      field.dataset.editing = "true";
      nameDraft = field.value;
      field.setCustomValidity("");
    };
    const submit = el("button", "game-button game-button--solid", "Create channel · free");
    submit.type = "submit";
    submit.disabled = ui.busy;
    submit.dataset.focusKey = "creator.start";
    const hint = el("p", "card-note", "1–32 characters. Letters, numbers, spaces, apostrophes, &, periods, ! and hyphens.");
    hint.id = "creator-name-hint";
    form.onsubmit = event => {
      event.preventDefault();
      const field = event.currentTarget.elements.channel;
      const valid = /^[\p{L}\p{N} '&.!-]+$/u.test(field.value.trim()) && /[\p{L}\p{N}]/u.test(field.value);
      field.setCustomValidity(valid ? "" : "Give your channel a name using the characters listed below.");
      if (event.currentTarget.reportValidity()) {
        nameDraft = field.value;
        delete field.dataset.editing;
        ui.sendAction("creator.start", field.value.trim());
      }
    };
    append(page, append(form, label, input, hint, submit));
    const steps = el("ol", "creator-start-steps");
    for (const [title, copy] of [
      ["Shoot whatever happens", `One trail day + ${money(data.productionCost)}. The road supplies the idea; you find out what it is after filming.`],
      ["Publish. Refresh. Cope.", "Subscribers and passing strangers might watch. Most videos go nowhere. Ad income comes much later, if at all."],
      ["Visit Victor & Horoshilov", "Cameras, lights, batteries, lenses. More equipment raises the audience ceiling. It cannot make anyone care."],
    ]) append(steps, append(el("li"), el("strong", "", title), el("p", "card-note", copy)));
    append(page, steps, el("p", "creator-fineprint", "Start at any point on the road. Keep your channel, gear and footage throughout this journey. Filming and re-editing spend real trail days; browsing and publishing do not."));
    return page;
  }

  function stats(data, ui) {
    const { element: el, append, labeledValue, integer } = ui;
    return append(el("div", "creator-stats"),
      labeledValue("Subscribers", integer(data.stats.subscribers)),
      labeledValue("Lifetime views", integer(data.stats.totalViews)),
      labeledValue("Original uploads", integer(data.stats.uploads)));
  }

  function sources(regular, discovery, ui) {
    const { element: el, append, integer } = ui;
    const total = regular + discovery;
    const block = el("div", "creator-sources");
    const track = el("div", "creator-sources__track");
    track.setAttribute("aria-hidden", "true");
    const followers = el("span", "creator-sources__regular");
    followers.style.width = `${total ? regular / total * 100 : 0}%`;
    const strangers = el("span", "creator-sources__discovery");
    strangers.style.width = `${total ? discovery / total * 100 : 0}%`;
    append(block, append(track, followers, strangers), append(el("div", "creator-sources__labels"),
      el("span", "", `● Subscribers ${integer(regular)}`),
      el("span", "", `● Non-followers ${integer(discovery)}`)));
    return block;
  }

  function videoCard(video, ui, latest = false) {
    const { element: el, append, integer, money } = ui;
    const card = el("article", `creator-video ${video.demonetized ? "creator-video--flagged" : ""}`);
    card.dataset.nodeKey = `creator-video-${video.id}`;
    append(card, append(el("div", "creator-row"),
      el("span", "eyebrow", `${latest ? "LATEST UPLOAD / " : ""}#${String(video.id).padStart(3, "0")} · DAY ${video.publishedDay}`),
      el("span", `creator-badge ${video.demonetized ? "creator-badge--warning" : ""}`,
        video.demonetized ? "DEMONETIZED" : video.reedited ? "REUPLOADED" : video.revenue > 0 ? "EARNING ADS" : "PUBLISHED")),
      el("h3", "", video.title), el("p", "creator-video__premise", video.premise),
      append(el("div", "creator-video__numbers"),
        append(el("div"), el("strong", "creator-view-count", integer(video.views)), el("span", "card-note", video.reedited ? "reupload views" : "views")),
        ui.labeledValue("Subscriber change", `+${integer(video.subscribersGained)} / −${integer(video.subscribersLost)}`),
        ui.labeledValue("Ad revenue", money(video.revenue))),
      sources(video.subscriberViews, video.discoveryViews, ui),
      el("p", "creator-feedback", video.feedback));
    const details = el("details", "creator-video__details");
    details.dataset.nodeKey = `creator-video-details-${video.id}`;
    const summary = el("summary", "", "Footage & audience details");
    summary.dataset.focusKey = `creator-video-details-${video.id}`;
    append(details, summary, append(el("div", "creator-video__metadata"),
      ui.labeledValue("Filmed", `Day ${video.filmedDay} · ${video.location}`),
      ui.labeledValue("Camera", video.camera),
      ui.labeledValue("Kit ceiling when filmed", `${integer(video.ceiling)} views`),
      el("p", "card-note", "The kit ceiling limits reach; it does not predict or create demand.")));
    card.append(details);
    if (video.reedited) append(card, el("p", "creator-reupload-note", `Original: ${integer(video.originalViews)} views. Replacement: ${integer(video.views)} views. Both count toward lifetime views. Subscriber changes shown above belong to the original upload; they are not awarded again.`));
    if (video.incident) {
      const incident = el("div", "creator-incident");
      append(incident, el("strong", "", video.reedited ? "EDIT RESOLVED" : "AD REVIEW / FLAGGED"), el("p", "", video.incident));
      if (video.demonetized && !video.reedited) append(incident,
        el("p", "card-note", `Spend one trail day removing the incident. The replacement will get exactly ${integer(Math.floor(video.originalViews / 2))} views: half the original, rounded down. One re-edit only.`),
        command(`creator.reedit.${video.id}`, ui, { className: "game-button--wide", noShortcut: !latest }));
      card.append(incident);
    }
    const title = card.querySelector("h3");
    title.id = `creator-video-${video.id}`;
    title.tabIndex = -1;
    return card;
  }

  function orders(data, ui) {
    if (!data.orders.length) return null;
    const { element: el, append, money } = ui;
    const card = section("Your latest business expenses", "V&H / PARCEL TRACKING", ui, "creator-orders");
    const list = el("ul");
    for (const order of data.orders) {
      const row = el("li");
      row.dataset.nodeKey = `creator-order-${order.gearId}`;
      append(list, append(row, el("strong", "", order.name), el("span", "", money(order.price)),
        el("small", "", `Locker delivery · ${order.daysRemaining} trail day${order.daysRemaining === 1 ? "" : "s"} remaining`)));
    }
    append(card, list, el("p", "card-note", "Already charged to road cash. Spend the day filming, traveling, or waiting. Delivered cameras must be selected; compatible accessories fit automatically."),
      command("creator.wait-order", ui));
    return card;
  }

  function bag(data, ui) {
    const { element: el, append, integer } = ui;
    const card = section("The camera bag", "EQUIPMENT / CURRENT SETUP", ui);
    append(card, append(el("div", "creator-kit-summary"),
      append(el("div"), el("strong", "", data.kit.camera),
        el("p", "card-note", `${data.kit.mount || "Fixed lens"} · ${data.kit.hdmi ? "Clean HDMI output" : "No external HDMI monitor"}`)),
      ui.labeledValue("Audience ceiling", integer(data.kit.ceiling))),
      el("p", "creator-ceiling-note", "More gear only raises this upper limit. It does not increase interest, improve an idea, or guarantee a single extra view. Footage keeps the kit it was filmed with."));
    const cameras = el("div", "creator-camera-list");
    for (const gear of data.gear.filter(g => g.owned && g.category === "camera")) {
      const row = el("div", "creator-bag-row");
      row.dataset.nodeKey = `creator-bag-${gear.id}`;
      append(row, append(el("div"), el("strong", "", gear.name), el("p", "card-note", `Base ceiling ${integer(gear.ceiling)}`)));
      if (gear.active) row.append(el("span", "creator-badge", "IN USE"));
      else row.append(command(gear.equipActionId, ui, { noShortcut: true }));
      cameras.append(row);
    }
    card.append(cameras);
    const accessories = data.gear.filter(g => g.owned && g.category !== "camera");
    if (accessories.length) {
      const list = el("ul", "creator-accessories");
      for (const gear of accessories) {
        const row = el("li");
        row.dataset.nodeKey = `creator-fitted-${gear.id}`;
        append(list, append(row, el("span", "", gear.name), el("small", gear.active ? "creator-positive" : "",
          gear.active ? `FITTED · +${integer(gear.ceiling)} ceiling` : !gear.compatible ? "INCOMPATIBLE WITH CURRENT CAMERA" : "SPARE · STRONGER ITEM FITTED")));
      }
      card.append(list);
    } else card.append(el("p", "card-note", "No supporting equipment. Your phone is already a complete production department."));
    append(card, el("p", "card-note", "The strongest compatible item in each accessory category fits automatically. Accessories do not stack within a category. Lenses need the matching mount; monitors need clean HDMI."));
    return card;
  }

  function studio(data, ui) {
    const { element: el, append, integer, money } = ui;
    const page = el("div", "creator-tab-content");
    page.dataset.nodeKey = "creator-studio";
    const production = section(data.draft ? "One video. Zero guarantees." : "Your next big idea is outside.", "STUDIO / FIELD PRODUCTION", ui, "creator-production");
    if (data.draft) {
      const draft = data.draft;
      append(production, el("span", "creator-badge", "FOOTAGE READY"), el("h3", "creator-draft-title", draft.title),
        el("p", "", draft.premise), el("p", "card-note", `Filmed day ${draft.filmedDay} · ${draft.location}`),
        el("p", "card-note", `${draft.camera} · ${integer(draft.ceiling)} view ceiling`),
        append(el("div", "creator-actions"), command("creator.publish", ui, { className: "game-button--solid" }), command("creator.discard", ui)),
        el("p", "card-note", "Publishing costs no additional time. Discarding does not return the filming day or money. Every original upload has a 15% chance of an ad restriction."));
    } else {
      append(production, el("p", "", "Press record. The road decides what happens; the audience decides whether it cares."),
        append(el("div", "creator-production__facts"), ui.labeledValue("Filming here", data.filmingLocation),
          ui.labeledValue("Production cost", `${money(data.productionCost)} + 1 trail day`)),
        command("creator.film", ui, { className: "game-button--solid game-button--wide" }),
        el("p", "card-note", "Ideas arrive at random after filming. Local stories require being at that stop. Filming spends a day without adding miles; your family still needs supplies."));
      if (ui.balance < data.productionCost) production.append(el("p", "creator-negative", `You need ${money(data.productionCost)} in road cash to film.`));
    }
    const draftTitle = production.querySelector(".creator-draft-title");
    if (draftTitle) { draftTitle.id = "creator-draft-title"; draftTitle.tabIndex = -1; }
    append(page, production, orders(data, ui));
    if (data.videos.length) page.append(videoCard(data.videos[0], ui, true));
    else append(page, append(el("div", "creator-empty"), el("span", "creator-empty__mark", "▷"),
      append(el("div"), el("h3", "", "No uploads. Immaculate reputation."), el("p", "card-note", "Your first audience report will appear here after publishing."))));
    append(page, bag(data, ui));
    return page;
  }

  function gearArt(category) {
    const image = svg("svg", { viewBox: "0 0 100 64", class: "creator-gear-art", "aria-hidden": "true", fill: "none", stroke: "currentColor", "stroke-width": 1.5 });
    const paths = {
      camera: "M15 20H33L38 13H61L66 20H85V53H15Z M21 26H32 M74 26H79",
      light: "M23 8H77V36H23Z M28 13H72V31H28Z M50 36V57 M35 59L50 48L65 59 M44 39H56",
      storage: "M32 7H59L70 18V57H32Z M38 13V25 M45 13V25 M52 13V25 M59 18V25 M38 32H64V50H38Z",
      power: "M31 13H69V58H31Z M43 6H57V13 M45 23L39 38H51L46 49L62 32H51L56 23Z",
      monitor: "M12 9H88V47H12Z M18 15H82V40H18Z M50 47V57 M38 57H62 M73 44H78",
      lens: "M25 16L75 11V53L25 48Z M32 16V48 M39 15V49 M64 13V52 M69 12V52 M45 21H57 M45 26H57",
      filter: "M23 52L16 59 M76 11L83 4",
      audio: "M35 12H48V38H35Z M55 12H68V38H55Z M39 7H44V12 M59 7H64V12 M35 46H68V58H35Z M41 18H43 M60 18H63",
      support: "M42 6H59V17H42Z M35 19H66V25H35Z M46 25L22 59 M55 25L79 59 M50 25V59 M36 43H65",
    };
    image.append(svg("path", { d: paths[category] || paths.camera }));
    if (category === "camera") image.append(svg("circle", { cx: 53, cy: 35, r: 15 }), svg("circle", { cx: 53, cy: 35, r: 9 }));
    if (category === "filter") image.append(svg("circle", { cx: 50, cy: 32, r: 25 }), svg("circle", { cx: 50, cy: 32, r: 20 }), svg("path", { d: "M32 24L60 45 M40 15L69 36", opacity: .45 }));
    return image;
  }

  function shop(data, ui) {
    const { element: el, append, integer, money } = ui;
    const page = el("div", "creator-tab-content");
    page.dataset.nodeKey = "creator-shop";
    append(page, append(el("div", "creator-shop-masthead"),
      el("strong", "creator-shop-logo", "V&H"), append(el("div"), el("h2", "", "Victor & Horoshilov"),
        el("p", "", "PHOTO · VIDEO · ASPIRATIONAL DEBT"), el("small", "", "Since your last impulse purchase."))),
      el("p", "creator-shop-pitch", "“Your channel has potential. We accept that potential in dollars.”"),
      el("p", "creator-ceiling-note", "Equipment raises the maximum views a video can receive. It does not raise audience demand. A $1,499 light can illuminate a video that gets 23 views."),
      orders(data, ui));
    const filters = el("div", "creator-filters");
    filters.setAttribute("aria-label", "Filter equipment by category");
    for (const [id, label] of categories) {
      const button = el("button", "creator-filter", label);
      button.type = "button";
      button.dataset.focusKey = `creator-filter-${id}`;
      button.setAttribute("aria-pressed", String(category === id));
      button.onclick = () => { category = id; ui.refresh(); };
      filters.append(button);
    }
    const products = data.gear.filter(g => g.id !== "phone" && (category === "all" || g.category === category));
    const results = el("p", "card-note", `${products.length} items · Paid now, delivered after 1 trail day · Road cash ${money(ui.balance)}`);
    results.setAttribute("role", "status");
    append(page, filters, results);
    const grid = el("div", "creator-product-grid");
    for (const gear of products) {
      const card = el("article", "creator-product");
      card.dataset.nodeKey = `creator-product-${gear.id}`;
      append(card, append(el("div", "creator-product__top"), gearArt(gear.category),
        append(el("div"), el("span", "eyebrow", categories.find(c => c[0] === gear.category)?.[1]),
          el("strong", "creator-product__price", money(gear.price)))),
        el("h3", "", gear.name), el("p", "creator-product__specs", gear.specs),
        el("p", "creator-product__pitch", `“${gear.pitch}”`),
        el("p", "creator-product__ceiling", gear.category === "camera" ? `Base ceiling: ${integer(gear.ceiling)} views` : `Ceiling contribution: +${integer(gear.ceiling)}`));
      if (gear.category === "lens") card.append(el("p", "card-note", `Requires active ${gear.mount} camera.`));
      if (gear.category === "monitor") card.append(el("p", "card-note", "Requires active camera with clean HDMI."));
      if (gear.owned) {
        card.append(el("span", "creator-product__state", gear.active ? "OWNED / IN USE" : "OWNED / IN CAMERA BAG"));
        if (gear.equipActionId && !gear.active) card.append(command(gear.equipActionId, ui, { className: "game-button--wide", noShortcut: true }));
      } else {
        const buy = command(gear.buyActionId, ui, { className: "game-button--wide", noShortcut: true,
          label: gear.buyBlock || `Order · ${money(gear.price)}` });
        if (buy) {
          buy.setAttribute("aria-label", `${gear.name}: ${gear.buyBlock || `Order for ${money(gear.price)}`}`);
          card.append(buy);
        }
      }
      grid.append(card);
    }
    append(page, grid, el("p", "creator-fineprint", "Supporting equipment fits automatically when compatible. Only the strongest item per category contributes. Cameras arrive in the bag; select one in Studio. Shipping is improbably free. Regret is included."));
    return page;
  }

  function audienceChart(data, ui) {
    const { element: el, append, integer } = ui;
    const videos = data.videos.slice(0, 18).reverse();
    const figure = el("figure", "creator-chart");
    append(figure, append(el("div", "creator-row"), el("h3", "", "An audience, in theory"), el("span", "card-note", "LATEST 18 UPLOADS")));
    if (!videos.length) {
      append(figure, el("p", "creator-chart__empty", "NO SIGNAL YET"), el("figcaption", "card-note", "Publish a video to begin the audience history."));
      return figure;
    }
    const graph = svg("svg", { viewBox: "0 0 640 185", role: "img", "aria-label": `Views for ${videos.length} recent videos, oldest to newest. Subscriber views and non-follower views are stacked. Exact counts appear in each video's report below.` });
    const maximum = Math.max(10, ...videos.map(v => v.views));
    const base = 148, height = 112, x0 = 73, width = 550, slot = width / videos.length, barWidth = Math.min(33, slot * .62);
    for (const fraction of [0, .5, 1]) {
      const y = base - fraction * height;
      graph.append(svg("line", { x1: x0, x2: 626, y1: y, y2: y, class: "creator-chart__grid" }),
        svg("text", { x: 63, y: y + 4, "text-anchor": "end", class: "creator-chart__axis" }, integer(Math.round(maximum * fraction))));
    }
    for (const [index, video] of videos.entries()) {
      const x = x0 + slot * index + (slot - barWidth) / 2;
      const regularHeight = video.subscriberViews / maximum * height;
      const discoveryHeight = video.discoveryViews / maximum * height;
      const group = svg("g");
      group.append(svg("title", {}, `${video.title}: ${integer(video.views)} views. ${integer(video.subscriberViews)} subscribers; ${integer(video.discoveryViews)} non-followers.`),
        svg("rect", { x, y: base - regularHeight, width: barWidth, height: regularHeight, fill: "#a6c7ad" }),
        svg("rect", { x, y: base - regularHeight - discoveryHeight, width: barWidth, height: discoveryHeight, fill: "#eeb68a" }));
      if (videos.length <= 9 || index % 2 === 0 || index === videos.length - 1) group.append(svg("text", { x: x + barWidth / 2, y: 168, "text-anchor": "middle", class: "creator-chart__axis" }, `#${video.id}`));
      graph.append(group);
    }
    append(figure, graph, append(el("figcaption", "creator-chart__caption"),
      el("span", "", "● Subscriber views"), el("span", "", "● Non-follower views")),
      el("p", "card-note", "A repaired video shows its replacement views. Lifetime totals include both the original and replacement."));
    return figure;
  }

  function library(data, ui) {
    const { element: el, append, integer } = ui;
    const page = el("div", "creator-tab-content");
    page.dataset.nodeKey = "creator-library";
    append(page, audienceChart(data, ui),
      append(el("div", "creator-library-summary"), ui.labeledValue("Original uploads", integer(data.stats.uploads)),
        ui.labeledValue("Reuploads", integer(data.stats.reuploads)), ui.labeledValue("Days creating", integer(data.stats.daysWorked))),
      sources(data.stats.subscriberViews, data.stats.discoveryViews, ui),
      el("p", "creator-ceiling-note", "Ad review can flag any original upload: a 15% chance, even with the expensive camera. You can repair an affected video once for a day of work and half its original views."));
    for (const video of data.videos) page.append(videoCard(video, ui));
    if (!data.videos.length) page.append(el("p", "creator-empty", "Your library is empty. Make some footage in Studio, then publish it here on the internet."));
    if (data.stats.uploads > data.videos.length) page.append(el("p", "card-note", "Showing the latest 60 video records. Lifetime totals retain earlier uploads."));
    return page;
  }

  function notices(data, ui) {
    if (!data.notices.length) return null;
    const { element: el, append } = ui;
    const details = el("details", "creator-notices");
    details.dataset.nodeKey = "creator-notices";
    const summary = el("summary", "", "Channel activity / receipts");
    summary.dataset.focusKey = "creator-notices";
    const list = el("ol");
    for (const notice of data.notices) {
      const item = el("li", "", notice.text);
      item.dataset.nodeKey = `creator-notice-${notice.id}`;
      list.append(item);
    }
    append(details, summary, list);
    return details;
  }

  function body(data, ui) {
    const { element: el, append } = ui;
    if (journey !== ui.journeyId) { journey = ui.journeyId; category = "all"; nameDraft = null; }
    const page = el("div", "creator-desk");
    const header = el("div", "creator-channel-head");
    append(header, append(el("div"), el("span", "eyebrow", data.started ? "YOUR CHANNEL / THIS JOURNEY" : "CREATOR DESK / ANYWHERE ON THE ROAD"),
      data.started ? el("h2", "", data.name) : null), command("creator.leave", ui, { className: "creator-return" }));
    const channelTitle = header.querySelector("h2");
    if (channelTitle) { channelTitle.id = "creator-channel-title"; channelTitle.tabIndex = -1; }
    page.append(header);
    if (!data.started) { page.append(onboarding(data, ui)); return page; }
    append(page, stats(data, ui));
    const nav = el("nav", "creator-tabs");
    nav.setAttribute("aria-label", "Travel channel sections");
    for (const tab of ["studio", "shop", "library"]) {
      const button = command(`creator.tab.${tab}`, ui, { className: "creator-tab" });
      if (button) {
        button.setAttribute("aria-current", data.tab === tab ? "page" : "false");
        nav.append(button);
      }
    }
    append(page, nav, data.tab === "shop" ? shop(data, ui) : data.tab === "library" ? library(data, ui) : studio(data, ui), notices(data, ui));
    return page;
  }

  function progress(label, current, required, ui) {
    const { element: el, append, integer } = ui;
    const block = el("div", "creator-partner-progress");
    const meter = el("progress");
    meter.max = required;
    meter.value = Math.min(current, required);
    // Attributes are mirrored explicitly for the shared DOM reconciler.
    meter.setAttribute("max", String(required));
    meter.setAttribute("value", String(Math.min(current, required)));
    meter.setAttribute("aria-label", `${label}: ${integer(current)} of ${integer(required)}`);
    append(block, append(el("div", "creator-row"), el("span", "", label), el("strong", "", `${integer(current)} / ${integer(required)}`)), meter);
    return block;
  }

  function rail(data, ui) {
    const { element: el, append, money, integer } = ui;
    const wrapper = el("div", "creator-rail");
    const ledger = el("section", "rail-card creator-ledger");
    append(ledger, ui.panelHeading("Business account / This journey", "The actual business model"),
      el("span", "eyebrow", "LIFETIME NET"), el("strong", `creator-net ${data.stats.net < 0 ? "creator-negative" : "creator-positive"}`, signed(data.stats.net, money)));
    const list = el("dl", "creator-ledger-lines");
    for (const [label, value, tone] of [
      ["Equipment orders", `−${money(data.stats.gearSpent)}`, ""],
      ["Production costs", `−${money(data.stats.productionSpent)}`, ""],
      ["Lifetime ad earnings", money(data.stats.revenue), "creator-positive"],
      ["Already paid to road", money(data.stats.paidOut), ""],
      ["Unpaid ad balance", money(data.stats.unpaid), "creator-positive"],
    ]) append(list, el("dt", "", label), el("dd", tone, value));
    append(ledger, list, el("p", "card-note", "Net = all ad earnings minus gear and production costs. Unpaid earnings are included in net, but cannot buy fuel until transferred."));
    if (data.started) append(ledger, command("creator.withdraw", ui, { className: "game-button--wide", noShortcut: true }),
      el("p", "card-note", `${money(data.partner.payoutMinimum)} minimum. Whole dollars transfer to road cash; cents remain here.`));
    const partner = el("section", "rail-card creator-partner");
    append(partner, ui.panelHeading("Fictional partner program", data.partner.monetized ? "Ads are switched on." : "First, qualify for ads."),
      el("span", `creator-badge ${data.partner.monetized ? "" : "creator-badge--neutral"}`, data.partner.monetized ? "MONETIZED CHANNEL" : "NO ADS YET"),
      progress("Subscribers", data.stats.subscribers, data.partner.subscriberThreshold, ui),
      progress("Lifetime views", data.stats.totalViews, data.partner.viewThreshold, ui),
      el("p", "card-note", data.partner.monetized
        ? "Eligible uploads earn ads. An individual video can still be demonetized. Buying equipment does not buy an audience."
        : "Meet both thresholds to unlock ad income. Older uploads do not receive back pay. The qualifying upload can earn ads if it passes review."),
      el("p", "creator-fineprint", "These are in-game thresholds, not the real YouTube partner rules."));
    const fieldNotes = el("section", "rail-card creator-field-notes");
    append(fieldNotes, ui.panelHeading("Production notes", "Your family is still on this trip."),
      ui.labeledValue("Days spent creating", integer(data.stats.daysWorked)),
      el("p", "card-note", "Filming and re-editing each cost a trail day. Travel and deliveries continue to matter. Your career survives closing the laptop."),
      el("p", "card-note", "Audience taste is unpredictable. Subscribers can stay, skip an upload, or leave. Discovery can bring strangers, or absolutely nobody."),
      el("p", "creator-fineprint", "This channel is a money trap with occasional exceptions. More equipment means more money to earn back."));
    append(wrapper, ledger, partner, fieldNotes);
    return wrapper;
  }

  window.TrailCreator = { body, rail };
})();
