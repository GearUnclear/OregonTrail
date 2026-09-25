#!/usr/bin/env python3
"""Timing and settlement checks against a disposable host."""
import time
from backend_integration import Client, prepare_trip


def main():
    client = Client()
    prepare_trip(client, name='Food Tester')
    for _ in range(15):
        state = client.refresh()
        if any('grab some food' in a['label'].lower() for a in state['screen']['actions']):
            break
        actions = [a for a in state['screen']['actions'] if a['enabled']]
        chosen = next((a for a in actions if 'Stop driving' in a['label']), None)
        chosen = chosen or next((a for a in actions if a.get('group') == 'drive'), None)
        chosen = chosen or next((a for a in actions if a['actionId'] != 'creator.open'), None)
        if chosen: client.action(chosen)
        client.settle()
    baseline = client.state
    client.action(next(a for a in baseline['screen']['actions'] if 'grab some food' in a['label'].lower()))
    state = client.state
    assert state['foodSweep'] and state['screen']['input'] is None, 'Sweep opens directly without typing'
    rounds = set()
    while client.state.get('foodSweep'):
        sweep = client.state['foodSweep']
        if sweep['round'] not in rounds and not sweep['resolved']:
            rounds.add(sweep['round'])
            # Start timing from receipt, just as the local animation does.
            elapsed = (sweep['zoneStart'] + sweep['zoneEnd']) // 2
            time.sleep(elapsed / 1000)
            body = client.action({'actionId': sweep['grabActionId']}, value=elapsed)
            assert client.state['foodSweep']['resolved']
            assert 'Got it!' in client.state['foodSweep']['feedback']
            pounds = client.state['foodSweep']['pounds']
            status, rejected, _ = client.request('/api/game/actions', body)
            assert status == 409 and not rejected['accepted'], 'Replay rejected'
        time.sleep(.04)
        client.refresh()
    # A day can raise a normal event above the result; dismiss it if needed.
    for _ in range(15):
        if client.state['screen']['id'].endswith('huntingresult'):
            break
        client.action()
        client.settle()
    assert client.state['screen']['id'].endswith('huntingresult')
    assert pounds == 100, 'Perfect timing reaches carry cap'
    assert client.state['hud']['turns'] == baseline['hud']['turns'] + 1
    before = next(i['quantity'] for i in client.state['inventory'] if i['itemId'] == 'snacks')
    client.action()
    client.settle()
    after = next(i['quantity'] for i in client.state['inventory'] if i['itemId'] == 'snacks')
    assert after == before + pounds, 'Result loads food exactly once'
    assert client.state['screen']['kind'] == 'travel'
    assert client.state['hud']['turns'] == baseline['hud']['turns'] + 1
    print('PASS direct food sweep, timing hits, replay rejection, automatic trays, one day, 100 lb settlement')


if __name__ == '__main__':
    main()
