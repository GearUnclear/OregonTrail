#!/usr/bin/env python3
"""Exercise the real HTTP host: isolation, concurrency, revisions, streaming, and driving.
Run against a disposable host: python3 tests/backend_integration.py http://127.0.0.1:18764
"""
import concurrent.futures
import http.cookiejar
import json
import queue
import statistics
import sys
import threading
import time
import urllib.error
import urllib.request

BASE = sys.argv[1].rstrip('/') if len(sys.argv) > 1 else 'http://127.0.0.1:18764'


class Client:
    def __init__(self):
        self.cookies = http.cookiejar.CookieJar()
        self.http = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.cookies))
        self.state = None

    def request(self, path='/api/game', body=None, headers=None):
        request = urllib.request.Request(BASE + path, data=None if body is None else json.dumps(body).encode(),
                                         headers={'Content-Type': 'application/json', **(headers or {})})
        try:
            response = self.http.open(request, timeout=15)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            data = response.read()
            return response.status, json.loads(data) if data else None, response.headers

    def refresh(self):
        status, state, _ = self.request()
        assert status == 200, (status, state)
        self.state = state
        return state

    def action(self, action=None, **values):
        state = self.state or self.refresh()
        action = action or next(action for action in state['screen']['actions'] if action['enabled'])
        body = {'actionId': action['actionId'], 'expectedRevision': state['revision'],
                'expectedJourneyId': state['journeyId'], **values}
        status, result, _ = self.request('/api/game/actions', body)
        assert status == 200 and result['accepted'], (status, result)
        self.state = result['state']
        return body

    def settle(self):
        time.sleep(.18)
        return self.refresh()


class Stream:
    def __init__(self, client, last_id=None):
        request = urllib.request.Request(BASE + '/api/game/events', headers={'Last-Event-ID': last_id} if last_id else {})
        client.cookies.add_cookie_header(request)
        self.response = urllib.request.urlopen(request, timeout=20)
        assert self.response.headers['Content-Type'] == 'text/event-stream'
        self.states = queue.Queue()
        self.thread = threading.Thread(target=self.read, daemon=True)
        self.thread.start()

    def read(self):
        try:
            while line := self.response.readline():
                if line.startswith(b'data: '):
                    state = json.loads(line[6:])
                    if 'revision' in state:
                        self.states.put(state)
        except (OSError, ValueError):
            pass

    def next(self, predicate=lambda state: True, timeout=8):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            state = self.states.get(timeout=max(.01, deadline - time.monotonic()))
            if predicate(state):
                return state
        raise AssertionError('No matching streamed state')

    def close(self):
        # Closing the socket directly wakes a pending readline without waiting for the next heartbeat.
        self.response.fp.raw._sock.shutdown(2)
        self.thread.join(timeout=2)
        self.response.close()


def prepare_trip(client, vehicle='minivan', name='Driver', packing=1):
    client.refresh()
    packed = False
    visited_store = False
    for step in range(50):
        state = client.state
        screen = state['screen']
        if screen['id'].endswith('packthecardecision'):
            assert not packed and not visited_store, 'Packing must happen once, before shopping'
            assert not state['driving']['isDriving'], 'Packing must happen before driving'
            packed = True
            balance = state['hud']['balance']
            client.action(screen['actions'][packing])
            if packing == 0:
                assert client.state['hud']['balance'] == balance + 600, 'Packing cash is available before shopping'
            client.settle()
            continue
        if screen['kind'] == 'store':
            assert packed, 'The first supply stop must follow packing at home'
            visited_store = True
            for item, amount in [('gas', 10), ('leggings', 2), ('snacks', 150), ('tire', 2)]:
                row = next((row for row in client.state['store']['rows'] if row['itemId'] == item), None)
                if row:
                    client.action({'actionId': row['setActionId']}, value=min(amount, row['maxQuantity']))
            snacks = next(row['quantity'] for row in client.state['store']['rows'] if row['itemId'] == 'snacks')
            client.action({'actionId': 'store.checkout'})
            loaded = next(item['quantity'] for item in client.state['inventory'] if item['itemId'] == 'snacks')
            assert loaded == max(0, snacks - (80 if packing == 0 else 0)), 'Opening checkout applies the packing consequence once'
        elif screen['kind'] == 'travel':
            assert packed and visited_store, 'Departure follows packing and shopping'
            return
        elif screen['input']:
            client.action(text=name if screen['input']['kind'] == 'text' else None, value=0)
        else:
            choices = [action for action in screen['actions'] if action['enabled']]
            chosen = next((action for action in choices if action.get('vehicleId') == vehicle), None)
            chosen = chosen or next((action for action in choices if 'travel light' in action['label'].lower()), None)
            chosen = chosen or (choices[0] if choices else None)
            if chosen:
                client.action(chosen)
                if chosen.get('vehicleId'):
                    assert client.state['hud']['vehicleId'] == vehicle, 'Setup preview must show the selected vehicle'
        client.settle()
    raise AssertionError(f'Setup did not reach travel: {client.state["screen"]}')


def main():
    health = Client()
    status, _, headers = health.request('/healthz')
    assert status == 200 and 'Set-Cookie' not in headers
    assert health.request('/api/missing')[0] == 404

    clients = [Client() for _ in range(16)]
    started = time.perf_counter()
    with concurrent.futures.ThreadPoolExecutor(max_workers=16) as pool:
        states = list(pool.map(lambda client: client.refresh(), clients))
    cold_ms = (time.perf_counter() - started) * 1000
    assert len({state['journeyId'] for state in states}) == len(clients)
    assert all(state['screen']['kind'] == 'setup' for state in states)
    client, other = clients[:2]
    other_before = other.state
    status, _, headers = client.request()
    assert status == 200 and 'no-store' in headers['Cache-Control']
    assert client.request(headers={'If-None-Match': headers['ETag']})[0] == 304

    stream = Stream(client)
    assert stream.next()['journeyId'] == client.state['journeyId']
    original_revision = client.state['revision']
    original = client.action()
    pushed = stream.next(lambda state: state['revision'] > original_revision)
    assert pushed['journeyId'] == client.state['journeyId']
    assert other.refresh() == other_before
    status, rejected, _ = client.request('/api/game/actions', original)
    assert status == 409 and not rejected['accepted']
    client.refresh()
    mismatch = dict(original, expectedRevision=client.state['revision'], expectedJourneyId='previous-host')
    assert client.request('/api/game/actions', mismatch)[0] == 409
    invalid = dict(original, expectedRevision=client.state['revision'], actionId='invented.action')
    assert client.request('/api/game/actions', invalid)[0] == 400
    stream.close()
    resumed = Stream(client, f'{client.state["journeyId"]}:0')
    assert resumed.next()['revision'] >= client.state['revision']
    resumed.close()
    print('PASS health, 16 isolated sessions, ETag, pushed actions, stale revision/host rejection, reconnect', flush=True)

    connections = [Stream(other) for _ in range(4)]
    for connection in connections:
        connection.next()
    assert other.request('/api/game/events')[0] == 429
    for connection in connections:
        connection.close()
    time.sleep(.1)
    replacement = Stream(other)
    replacement.next()
    replacement.close()
    print('PASS HTTP stream admission limit and disconnect cleanup', flush=True)

    ended = clients[3]
    incarnation = ended.state['journeyId']
    ended.action(next(action for action in ended.state['screen']['actions'] if action['label'] == 'End session'))
    ended.settle()
    assert not ended.state['running']
    ended.action()
    for _ in range(20):
        ended.settle()
        if ended.state['screen']['kind'] == 'setup':
            break
    assert ended.state['running'] and ended.state['screen']['kind'] == 'setup'
    assert ended.state['journeyId'] == incarnation
    print('PASS end-session and restart lifecycle', flush=True)

    # Two simultaneous mutations of one revision can accept at most one command.
    contender = clients[2]
    state = contender.state
    body = {'actionId': state['screen']['actions'][0]['actionId'], 'expectedRevision': state['revision'],
            'expectedJourneyId': state['journeyId']}
    cookie = '; '.join(f'{cookie.name}={cookie.value}' for cookie in contender.cookies)
    def race(_):
        return Client().request('/api/game/actions', body, {'Cookie': cookie})[0]
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        assert sorted(pool.map(race, range(2))) == [200, 409]
    print('PASS concurrent command conflict', flush=True)

    drivers = [Client() for _ in range(4)]
    vehicles = ['minivan', 'pickupcamper', 'hybridcrossover', 'electrichatchback']
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        list(pool.map(lambda trip: prepare_trip(trip[0], trip[1], trip[1], trip[2]),
                      zip(drivers, vehicles, [1, 0, 2, 1])))
    for driver, vehicle in zip(drivers, vehicles):
        assert driver.state['hud']['vehicleId'] == vehicle
        assert all(person['name'] == vehicle for person in driver.state['party'])
        assert not driver.state['driving']['isDriving']
        hud = driver.state['hud']
        assert hud['foodPerDay'] == 2 * hud['livingPartyCount'], 'Filling uses two pounds per living passenger'
        assert hud['foodDaysRemaining'] == int(hud['foodPoundsRemaining'] / hud['foodPerDay'])
    print('PASS parallel full setup, four vehicles, names, store checkout, parked state', flush=True)

    # An odd-size party must keep the fractional daily rate in the real JSON contract.
    ration_driver = drivers[2]
    for label, expected in [('meager', 4.5), ('bare bones', 3), ('filling', 6)]:
        ration_driver.action(next(a for a in ration_driver.state['screen']['actions']
                                  if a['label'] == 'Change food rations'))
        ration_driver.settle()
        ration_driver.action(next(a for a in ration_driver.state['screen']['actions']
                                  if a['enabled'] and a['label'].lower().startswith(label)))
        ration_driver.settle()
        hud = ration_driver.state['hud']
        assert hud['foodPerDay'] == expected, 'Ration changes update the decimal supply forecast'
        assert hud['foodDaysRemaining'] == int(hud['foodPoundsRemaining'] / expected)
    print('PASS live ration choices and fractional food forecasts', flush=True)

    # Random road events are allowed; retry a fresh drive until we observe a moving interval, then stop it.
    driver = drivers[0]
    live = Stream(driver)
    live.next()
    for attempt in range(15):
        driver.refresh()
        assert not driver.state['screen']['id'].endswith('packthecardecision'), 'Departure must not return home'
        if driver.state['driving']['isDriving']:
            break
        actions = [a for a in driver.state['screen']['actions'] if a['enabled']]
        assert actions, driver.state['screen']
        drive = next((a for a in actions if a.get('group') == 'drive'), actions[0])
        driver.action(drive)
        try:
            moving = live.next(lambda state: state.get('driving', {}).get('isDriving'), timeout=2)
            driver.state = moving
            break
        except queue.Empty:
            pass
    assert driver.state['driving']['isDriving'], driver.state['screen']
    assert driver.state['driving']['vehicleId'] == 'minivan'
    assert driver.state['driving']['destination']
    driver.action(next(a for a in driver.state['screen']['actions'] if a['enabled'] and a['kind'] == 'continue'))
    stopped = live.next(lambda state: not state.get('driving', {}).get('isDriving'))
    assert not stopped['driving']['isDriving']
    live.close()
    print('PASS server-pushed driving start and stop', flush=True)
    print(json.dumps({'parallelColdSessions': 16, 'coldWallMs': round(cold_ms, 1)}))


if __name__ == '__main__':
    main()
