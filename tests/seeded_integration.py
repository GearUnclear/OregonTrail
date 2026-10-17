#!/usr/bin/env python3
"""Replay seeded journeys over real HTTP (#69). Run against a disposable seeded host, never production:

    ASPNETCORE_ENVIRONMENT=Development GameHost__TestSeeds=true \\
        dotnet ./publish/OregonTrailDotNet.dll --urls http://127.0.0.1:18764
    python3 tests/seeded_integration.py http://127.0.0.1:18764

Each scenario starts two journeys with the same pinned seed, plays both with the same fixed policy, and requires the
same sequence of settled snapshots from both, plus the outcome the seed was picked for. If a rule change moves an
outcome, find a new seed with the search mode and update the constant:

    python3 tests/seeded_integration.py http://127.0.0.1:18764 --search viral 0 120
    python3 tests/seeded_integration.py http://127.0.0.1:18764 --search river 0 60

Each line of search output is one seed. For viral, pick a seed whose "upload" isn't null and set VIRAL_SEED and
VIRAL_UPLOAD to it. For river, pick one with "lostPassengers" above 0 and set RIVER_SEED. Every searched seed is a
journey the host keeps for SessionLifetime, and the host admits only MaximumSessions (256 by default), so start the
seeded host with GameHost__MaximumSessions=4096 for long searches, or restart it between them. Changes to how the
creator channel or crypto desk seed their generators move VIRAL_SEED, so search again after them; #71 did.
"""
import concurrent.futures
import json
import sys
import time

import backend_integration
from backend_integration import Client

SEED_HEADER = 'X-Test-Seed'
# Picked with --search. VIRAL_SEED's 32nd upload from Cape Coral goes viral and draws a Viral Hog offer (a viral
# roll is about 0.16% per upload, and most seeds lose the party before 60 uploads). Seed 52 goes viral on its
# first upload, but 32 filming days replay far more of the journey.
# RIVER_SEED gunning it through the first river crossing drowns two passengers.
VIRAL_SEED, VIRAL_UPLOAD = 105, 32
RIVER_SEED = 2
MAX_UPLOADS = 60
# A settled screen that a fixed (one-second) game tick can still change must hold still for longer than two ticks.
QUIET_SECONDS = 2.3


def start(seed):
    """A new cookie jar, so the seed always starts a new journey."""
    client = Client()
    status, state, _ = client.request(headers={SEED_HEADER: str(seed)})
    assert status == 200, (status, state)
    client.state = state
    return client


def semantic(state):
    """The snapshot without the fields that never describe the game: the host's journey id and its frame counter."""
    return {key: value for key, value in state.items() if key not in ('revision', 'journeyId')}


def enabled(state):
    return [action for action in state['screen']['actions'] if action['enabled']]


def settle(client):
    """Wait until the journey waits for the player. Acting earlier would race the real-time game clock.

    Driving, resting, waiting out an event, and crossing all advance on one-second game ticks. Other screens only
    change when the player acts, so they are ready as soon as the action's own transitions have finished.
    """
    quiet_since, last = None, None
    deadline = time.monotonic() + 240
    while time.monotonic() < deadline:
        time.sleep(.25)
        state = client.refresh()
        if state['driving']['isDriving']:
            quiet_since = None
            continue
        if state['screen']['kind'] != 'activity' and enabled(state):
            return state
        current = semantic(state)
        if current != last:
            quiet_since, last = time.monotonic(), current
        elif time.monotonic() - quiet_since >= QUIET_SECONDS:
            return state
    raise AssertionError(f'Journey never settled: {client.state["screen"]}')


def act(client, action_id, **values):
    state = client.state
    body = {'actionId': action_id, 'expectedRevision': state['revision'], 'expectedJourneyId': state['journeyId'], **values}
    status, result, _ = client.request('/api/game/actions', body)
    assert status == 200 and result['accepted'], (action_id, status, result)
    client.state = result['state']
    return settle(client)


def find(state, suffix):
    return next(action for action in enabled(state) if action['actionId'].endswith(suffix))


def default_action(state):
    """The policy for everything the scenario doesn't decide: the first advertised choice, never the studio."""
    actions = [action for action in enabled(state) if not action['actionId'].startswith('creator.')]
    return next((action for action in actions if action.get('group') == 'drive'), actions[0] if actions else None)


def setup(client, trace):
    """Crypto bro, minivan, one named driver plus auto-filled seats, March, travel light, a fixed opening haul."""
    state = settle(client)
    for _ in range(40):
        trace.append(semantic(state))
        screen = state['screen']
        sid = screen['id']
        if screen['kind'] == 'travel':
            return state
        if sid.endswith('mainmenuscreen'):
            state = act(client, find(state, '.select.0')['actionId'])
        elif sid.endswith('professionselector'):
            state = act(client, find(state, '.select.0')['actionId'])
        elif sid.endswith('vehicleselector'):
            state = act(client, next(a for a in enabled(state) if a.get('vehicleId') == 'minivan')['actionId'])
        elif sid.endswith('inputplayernames'):
            state = act(client, screen['input']['actionId'], text='' if state['party'] else 'Seedy')
        elif sid.endswith('packthecardecision'):
            state = act(client, find(state, '.select.1')['actionId'])
        elif screen['kind'] == 'store':
            for item, amount in [('gas', 10), ('snacks', 250), ('leggings', 4), ('tire', 2)]:
                row = next(row for row in state['store']['rows'] if row['itemId'] == item)
                state = act(client, row['setActionId'], value=min(amount, row['maxQuantity']))
            state = act(client, 'store.checkout')
        else:
            state = act(client, default_action(state)['actionId'])
    raise AssertionError(f'Setup did not reach travel: {state["screen"]}')


def back_to_studio(client, state, trace):
    """A filming day can raise an ordinary road or party event above the studio."""
    for _ in range(20):
        if state.get('creator'):
            return state
        assert state['screen']['kind'] != 'game-over', 'The seeded journey ended before the studio returned'
        trace.append(semantic(state))
        state = act(client, default_action(state)['actionId'])
    raise AssertionError(f'Studio did not return: {state["screen"]}')


def viral(seed, uploads=MAX_UPLOADS):
    """Open the studio at Cape Coral and upload until a video goes viral. Returns the trace and the upload count."""
    client, trace = start(seed), []
    state = setup(client, trace)
    # Every upload costs a trail day. Bare-bones meals keep a long session from starving the party.
    state = act(client, next(a for a in enabled(state) if a['label'] == 'Change food rations')['actionId'])
    state = act(client, next(a for a in enabled(state) if a['label'].lower().startswith('bare bones'))['actionId'])
    trace.append(semantic(state))
    state = act(client, 'creator.open')
    state = act(client, 'creator.start', text='Seeded Channel')
    for upload in range(1, uploads + 1):
        trace.append(semantic(state))
        if 'creator.film' not in {a['actionId'] for a in enabled(state)}:
            return client, trace, None
        state = back_to_studio(client, act(client, 'creator.film'), trace)
        state = act(client, 'creator.publish')
        trace.append(semantic(state))
        if state['creator']['license']:
            return client, trace, upload
        state = back_to_studio(client, state, trace)
    return client, trace, None


def river(seed):
    """Drive from Cape Coral to the first river crossing and ford it."""
    client, trace = start(seed), []
    state = setup(client, trace)
    for _ in range(80):
        trace.append(semantic(state))
        screen = state['screen']
        if screen['kind'] == 'game-over':
            return client, trace, None
        if screen['kind'] == 'river' and screen['id'].endswith('-rivercross'):
            break
        state = act(client, default_action(state)['actionId'])
    else:
        raise AssertionError(f'Never reached the river: {state["screen"]}')
    before = state
    # Gunning it through the high water is the riskiest choice, so a search finds a costly crossing quickly.
    state = act(client, next(a for a in enabled(state) if a['label'].lower().startswith('gun it'))['actionId'])
    incidents = []
    for _ in range(20):
        trace.append(semantic(state))
        screen = state['screen']
        if screen['id'].endswith('locationdepart') or screen['kind'] == 'game-over':
            break
        if screen['kind'] == 'event' or screen['id'].endswith('eventskipday'):
            incidents.append(screen['description'])
        state = act(client, default_action(state)['actionId'])
    else:
        raise AssertionError(f'The crossing never finished: {state["screen"]}')
    # Snacks are eaten every day, so only count gear the crossing itself can take.
    def gear(snapshot):
        return {item['itemId']: item['quantity'] for item in snapshot['inventory'] if item['itemId'] != 'snacks'}
    lost_gear = {item: count - gear(state)[item] for item, count in gear(before).items() if count > gear(state)[item]}
    return client, trace, {
        'river': before['hud']['locationName'],
        'incidents': incidents,
        'lostGear': lost_gear,
        'lostPassengers': before['hud']['livingPartyCount'] - state['hud']['livingPartyCount'],
        'days': state['hud']['turns'] - before['hud']['turns'],
        'end': screen['id'].split('-')[-1],
    }


def twice(scenario, seed):
    """Play the same seed in two journeys at once. Their wall clocks and tick phases differ; their games must not."""
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        first, second = pool.map(lambda _: scenario(seed), range(2))
    assert first[0].state['journeyId'] != second[0].state['journeyId'], 'Each seed request starts its own journey'
    for step, (left, right) in enumerate(zip(first[1], second[1])):
        assert left == right, f'Seed {seed} diverged at settled step {step}:\n{json.dumps(left)[:2000]}\n{json.dumps(right)[:2000]}'
    assert len(first[1]) == len(second[1]) and first[2] == second[2], (len(first[1]), len(second[1]), first[2], second[2])
    return first


def check_guards():
    """Seeds start new journeys only, and only well-formed ones."""
    assert Client().request(headers={SEED_HEADER: 'seven'})[0] == 400
    assert Client().request(headers={SEED_HEADER: '-1'})[0] == 400
    assert Client().request(headers={SEED_HEADER: str(2 ** 31)})[0] == 400
    client = start(1)
    status, problem, _ = client.request(headers={SEED_HEADER: '1'})
    assert status == 400 and 'new journey' in problem['detail'], (status, problem)
    status, problem, _ = client.request('/api/game/events', headers={SEED_HEADER: '2'})
    assert status == 400, status
    assert client.refresh()['journeyId'] == client.state['journeyId'], 'A refused seed leaves the journey alone'
    # Separate journeys with one seed generate the same randomized route.
    assert start(1).state['progress'] == start(1).state['progress']
    print('PASS seed header: malformed and out-of-range values, re-seeding a live journey, and the stream are refused', flush=True)


def check_viral():
    client, trace, upload = twice(viral, VIRAL_SEED)
    assert upload == VIRAL_UPLOAD, f'Seed {VIRAL_SEED} went viral on upload {upload}, not {VIRAL_UPLOAD}; rerun --search viral'
    state = client.state
    offer, video = state['creator']['license'], state['creator']['videos'][0]
    assert video['viral'] and offer['videoId'] == video['id'], (offer, video)
    assert {a['actionId'] for a in state['screen']['actions']} == {'creator.license-accept', 'creator.license-decline'}
    cash, turns = state['hud']['balance'], state['hud']['turns']
    accept = {'actionId': 'creator.license-accept', 'expectedRevision': state['revision'], 'expectedJourneyId': state['journeyId']}
    status, result, _ = client.request('/api/game/actions', accept)
    assert status == 200 and result['accepted'], (status, result)
    sold = result['state']
    assert sold['hud']['balance'] == cash + offer['offer'] and sold['hud']['turns'] == turns, 'The sale pays road cash once, same day'
    assert sold['creator']['license'] is None and sold['creator']['videos'][0]['licensed']
    assert sold['creator']['stats']['licenseIncome'] == offer['offer']
    status, replay, _ = client.request('/api/game/actions', accept)
    assert status == 409 and not replay['accepted'], 'Replaying the accepted answer is stale'
    print(f'PASS seed {VIRAL_SEED}: two journeys replay {len(trace)} identical settled screens to a viral upload '
          f'#{upload}; creator.license-accept paid ${offer["offer"]:,} once over HTTP', flush=True)


def check_river():
    client, trace, result = twice(river, RIVER_SEED)
    assert result and result['incidents'] and result['lostPassengers'] > 0, \
        f'Seed {RIVER_SEED} crossed without drowning anyone: {result}; rerun --search river'
    party = client.state['party']
    assert sum(person['healthKey'] == 'dead' for person in party) >= result['lostPassengers'], party
    print(f'PASS seed {RIVER_SEED}: two journeys replay {len(trace)} identical settled screens through {result["river"]}, '
          f'with the same incident and losses: {json.dumps(result)}', flush=True)


def search(kind, first, count):
    """Print what each seed in a range does under the policy, one JSON line per seed."""
    def run(seed):
        try:
            if kind == 'viral':
                client, trace, upload = viral(seed)
                return {'seed': seed, 'upload': upload, 'steps': len(trace), 'turns': client.state['hud']['turns']}
            client, trace, result = river(seed)
            return {'seed': seed, 'steps': len(trace), **(result or {'end': 'game-over before the river'})}
        except AssertionError as error:
            return {'seed': seed, 'error': str(error)[:300]}
    with concurrent.futures.ThreadPoolExecutor(max_workers=24) as pool:
        for outcome in pool.map(run, range(first, first + count)):
            print(json.dumps(outcome), flush=True)


def main():
    status, problem, _ = Client().request(headers={SEED_HEADER: '1'})
    assert status == 200, f'This host refuses test seeds ({status}: {problem}). Start it with ' \
        'ASPNETCORE_ENVIRONMENT=Development and GameHost__TestSeeds=true.'
    if '--search' in sys.argv:
        at = sys.argv.index('--search')
        search(sys.argv[at + 1], int(sys.argv[at + 2]), int(sys.argv[at + 3]))
        return
    check_guards()
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        for future in [pool.submit(check_viral), pool.submit(check_river)]:
            future.result()


if __name__ == '__main__':
    backend_integration.BASE = sys.argv[1].rstrip('/') if len(sys.argv) > 1 and not sys.argv[1].startswith('--') \
        else backend_integration.BASE
    main()
