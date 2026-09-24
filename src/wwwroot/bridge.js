(() => {
  "use strict";

  const relativeUrl = (path) => new URL(path, document.baseURI).toString();
  let etag = null;

  async function request(path, options = {}) {
    const controller = new AbortController();
    const timeout = window.setTimeout(() => controller.abort(), 12000);
    try {
      const response = await fetch(relativeUrl(path), {
        ...options, cache: "no-store", signal: controller.signal,
        headers: {
          Accept: "application/json",
          ...(options.body ? { "Content-Type": "application/json" } : {}),
          ...options.headers,
        },
      });
      if (response.status === 304) return null;
      const body = await response.json();
      if (!response.ok && !body?.state) {
        throw new Error(body?.detail || body?.message || `Server returned ${response.status}`);
      }
      if (path === "api/game") etag = response.headers.get("ETag");
      return body;
    } catch (error) {
      throw new Error(error.name === "AbortError" ? "The game server took too long to respond." : error.message);
    } finally { window.clearTimeout(timeout); }
  }

  window.OregonTrailApi = Object.freeze({
    // Bootstrap first to establish the HttpOnly journey cookie before opening the event stream.
    watch(onState, onError, onConnection) {
      let source = null;
      let timer = null;
      let watchdog = null;
      let stopped = false;
      let inFlight = null;
      let retryDelay = 1000;
      let generation = 0;

      function healthy() {
        retryDelay = 1000;
        onConnection(true);
      }
      function armWatchdog() {
        clearTimeout(watchdog);
        watchdog = setTimeout(() => {
          source?.close();
          source = null;
          onConnection(false);
          refresh();
        }, 40000);
      }
      function scheduleFallback() {
        if (timer || stopped || document.hidden) return;
        timer = setTimeout(() => { timer = null; refresh(); }, retryDelay);
        retryDelay = Math.min(retryDelay * 2, 15000);
      }
      function connect() {
        if (source || stopped || document.hidden || navigator.onLine === false) return;
        if (!window.EventSource) { scheduleFallback(); return; }
        source = new EventSource(relativeUrl("api/game/events"));
        const activeSource = source;
        source.addEventListener("state", (event) => {
          if (source !== activeSource) return;
          try {
            const state = JSON.parse(event.data);
            if (!state?.screen || !Number.isSafeInteger(state.revision)) throw new Error("Invalid live state");
            healthy();
            onState(state);
            clearTimeout(timer);
            timer = null;
            armWatchdog();
          } catch {
            source?.close();
            source = null;
            onConnection(false);
            onError("The live update could not be read. Reconnecting…");
            scheduleFallback();
          }
        });
        source.addEventListener("heartbeat", () => {
          if (source !== activeSource) return;
          healthy();
          armWatchdog();
        });
        source.onerror = () => {
          if (source !== activeSource) return;
          clearTimeout(watchdog);
          if (source?.readyState === EventSource.CLOSED) source = null;
          onConnection(false);
          scheduleFallback();
        };
        armWatchdog();
      }
      function refresh() {
        if (stopped || document.hidden || navigator.onLine === false) return Promise.resolve();
        if (inFlight) return inFlight;
        const requestedGeneration = generation;
        inFlight = request("api/game", { headers: etag ? { "If-None-Match": etag } : {} })
          .then((state) => {
            if (stopped || document.hidden || requestedGeneration !== generation) return;
            onConnection(true);
            if (state) onState(state);
            connect();
            // Successful fallback snapshots stay frequent enough to show travel, while network failures back off.
            retryDelay = Math.min(retryDelay, 3000);
            if (!source || source.readyState !== EventSource.OPEN) scheduleFallback();
          })
          .catch((error) => {
            if (stopped || document.hidden || requestedGeneration !== generation) return;
            onConnection(false);
            onError(error.message);
            scheduleFallback();
          })
          .finally(() => { inFlight = null; });
        return inFlight;
      }
      function disconnect() {
        generation++;
        clearTimeout(timer);
        clearTimeout(watchdog);
        timer = null;
        source?.close();
        source = null;
        onConnection(false);
      }
      function visibility() {
        disconnect();
        if (!document.hidden) {
          // If a hidden-tab request is still completing, reconnect after it drains.
          (inFlight || Promise.resolve()).finally(refresh);
        }
      }
      document.addEventListener("visibilitychange", visibility);
      window.addEventListener("online", refresh);
      window.addEventListener("offline", disconnect);
      refresh();
      return Object.freeze({
        refresh,
        stop() {
          stopped = true;
          source?.close();
          clearTimeout(timer);
          clearTimeout(watchdog);
          document.removeEventListener("visibilitychange", visibility);
          window.removeEventListener("online", refresh);
          window.removeEventListener("offline", disconnect);
        },
      });
    },

    action(actionId, expectedRevision, expectedJourneyId, text, value, onSuccess, onFailure) {
      const payload = { actionId, expectedRevision, expectedJourneyId };
      if (typeof text === "string") payload.text = text;
      if (Number.isFinite(value)) payload.value = value;
      return request("api/game/actions", { method: "POST", body: JSON.stringify(payload) })
        .then((body) => onSuccess({ ...body.state, responseMessage: body.message || "", actionAccepted: body.accepted === true }))
        .catch((error) => onFailure(error.message));
    },
  });
})();
