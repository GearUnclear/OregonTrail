#!/usr/bin/env python3
"""Exercise RUG.RUN against a disposable host, including real SSE updates and cash settlement.
Run: python3 tests/crypto_integration.py http://127.0.0.1:18765
"""
import json
import time
from backend_integration import Client, Stream, prepare_trip


def act(client, action_id, **values):
    # A market tick can win the race between a test read and write. Retry only a confirmed stale rejection.
    for attempt in range(8):
        state = client.refresh()
        body = {'actionId': action_id, 'expectedRevision': state['revision'],
                'expectedJourneyId': state['journeyId'], **values}
        status, result, _ = client.request('/api/game/actions', body)
        client.state = result['state']
        if status == 409:
            continue
        assert status == 200 and result['accepted'], (status, result)
        return body
    raise AssertionError('Repeated revision conflicts')


def main():
    player = Client()
    prepare_trip(player, name='Coin Tester')
    baseline = player.state
    launch = next(a for a in baseline['screen']['actions'] if a['label'] == 'Launch a crypto coin')
    player.action(launch)
    player.settle()
    assert player.state['screen']['kind'] == 'crypto'
    assert player.state['crypto']['phase'] == 'lobby'
    assert player.state['hud']['balance'] == baseline['hud']['balance']
    act(player, 'crypto.leave')
    player.settle()
    assert player.state['hud']['turns'] == baseline['hud']['turns'], 'Browsing cannot consume a day'
    launch = next(a for a in player.state['screen']['actions'] if a['label'] == 'Launch a crypto coin')
    player.action(launch)
    player.settle()
    current = player.state
    status, invalid, _ = player.request('/api/game/actions', {
        'actionId': 'crypto.rename', 'expectedRevision': current['revision'],
        'expectedJourneyId': current['journeyId'], 'text': '<img onerror=boom>'})
    assert status == 400 and not invalid['accepted'], 'Invalid coin names rejected at the server'
    act(player, 'crypto.rename', text='Pothole Protocol')
    act(player, 'crypto.stake.75')
    act(player, 'crypto.narrative.utility')
    saved = player.refresh()['crypto']
    assert saved['name'] == 'Pothole Protocol' and saved['stake'] == 75 and saved['narrativeId'] == 'utility'

    observer = Client()
    untouched = observer.refresh()
    stream = Stream(player)
    stream.next()
    try:
        launch_body = act(player, 'crypto.launch')
        assert player.state['crypto']['phase'] == 'live'
        assert player.state['hud']['balance'] == baseline['hud']['balance'] - 87
        status, duplicate, _ = player.request('/api/game/actions', launch_body)
        assert status == 409 and not duplicate['accepted'], 'Launch cannot be replayed'
        act(player, 'crypto.hype.memes')
        assert player.state['hud']['balance'] == baseline['hud']['balance'] - 91
        assert player.state['crypto']['work']['id'] == 'memes'
        assert not next(a for a in player.state['screen']['actions'] if a['actionId'] == 'crypto.hype.ama')['enabled']
        streamed = stream.next(lambda s: s.get('crypto', {}).get('market', {}).get('elapsed', 0) >= 4, timeout=9)
        assert len(streamed['crypto']['candles']) >= 5, 'Live stream contains authoritative candles'
        assert not streamed['driving']['isDriving'], 'Trading cannot drive the vehicle'
        assert streamed['hud']['turns'] == baseline['hud']['turns'], 'Market ticks are not trail days'
        player.refresh()
        if player.state['crypto']['phase'] == 'live':
            act(player, 'crypto.pull-out')
            assert player.state['crypto']['phase'] == 'exiting'
            assert 1 <= player.state['crypto']['market']['exitRemaining'] <= 3
            assert not player.state['screen']['actions'], 'Cannot issue more orders during settlement'
        result = stream.next(lambda s: s.get('crypto', {}).get('phase') == 'result', timeout=9)
        receipt = result['crypto']['receipt']
        assert result['hud']['balance'] == baseline['hud']['balance'] + receipt['net']
        assert receipt['marketing'] == 4 and receipt['launchFee'] == 12
        assert result['crypto']['career']['launches'] == 1
        time.sleep(1.2)
        assert player.refresh()['hud']['balance'] == result['hud']['balance'], 'Payout is applied exactly once'
        act(player, 'crypto.leave')
        assert player.state['hud']['turns'] == baseline['hud']['turns'] + 1, 'Returning consumes one trail day'
        assert observer.refresh() == untouched, 'Another journey is unaffected'
        print('PASS crypto HTTP: setup, invalid names, budget, duplicate prevention, live candles, campaign locks, settlement, day cost, isolation')
        print(json.dumps({'net': receipt['net'], 'returned': receipt['returned'], 'elapsed': receipt['duration']}))
    finally:
        stream.close()


if __name__ == '__main__':
    main()
