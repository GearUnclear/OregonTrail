#!/usr/bin/env python3
"""Real HTTP journey checks for DEAD AIR. Run against a disposable host, never production."""
import json
from backend_integration import Client, Stream, prepare_trip
from crypto_integration import act


def reject(client, action_id, text=None):
    state = client.refresh()
    status, response, _ = client.request('/api/game/actions', {
        'actionId': action_id, 'expectedRevision': state['revision'],
        'expectedJourneyId': state['journeyId'], 'text': text})
    assert status == 400 and not response['accepted'], (status, response)
    client.state = response['state']


def settle_studio(client):
    # Spending an actual trail day may put a normal road/party event above the studio.
    for _ in range(15):
        state = client.settle()
        if state.get('creator'):
            return state
        assert state['screen']['kind'] != 'game-over', 'Unexpectedly lost disposable test journey'
        actions = [a for a in state['screen']['actions'] if a['enabled']]
        if actions:
            client.action(actions[0])
    raise AssertionError('Studio did not return after event')


def assert_public_payload(value):
    if isinstance(value, dict):
        assert not {'algorithmscore', 'potential', 'conceptkey', 'filmedideas', 'random'} & {key.lower() for key in value}, 'Internal creator fields leaked'
        for child in value.values():
            assert_public_payload(child)
    elif isinstance(value, list):
        for child in value:
            assert_public_payload(child)


def start_driving(client):
    # Subscribe before departure: ContinueOnTrail starts motion on a clock pulse,
    # not synchronously with its form transition. Never click its first action
    # while waiting, because that action intentionally opens the creator desk.
    live = Stream(client)
    try:
        for _ in range(12):
            current = client.refresh()
            if current['driving']['isDriving']:
                return
            if current['screen']['id'].endswith('continueontrail'):
                client.state = live.next(lambda s: s.get('driving', {}).get('isDriving'), timeout=5)
                return
            actions = [a for a in current['screen']['actions'] if a['enabled'] and not a['actionId'].startswith('creator.')]
            if actions:
                chosen = next((a for a in actions if a.get('group') == 'drive'), actions[0])
                act(client, chosen['actionId'])
            client.settle()
        raise AssertionError('Fixture did not reach actual driving')
    finally:
        live.close()


def main():
    player, other = Client(), Client()
    untouched = other.refresh()
    prepare_trip(player, name='Video Tester')
    baseline = player.refresh()
    assert any(a['actionId'] == 'creator.open' for a in baseline['screen']['actions'])
    act(player, 'creator.open')
    state = settle_studio(player)
    assert state['screen']['kind'] == 'creator' and not state['creator']['started']
    act(player, 'creator.leave')
    player.settle()
    assert player.state['hud']['turns'] == baseline['hud']['turns'], 'Browsing costs no day'

    # Start the channel for the first time from the actual moving-road screen.
    start_driving(player)
    assert player.state['driving']['isDriving'], 'Fixture must reach actual driving'
    act(player, 'creator.open')
    state = settle_studio(player)
    assert not state['driving']['isDriving'] and state['creator']['filmingLocation'].startswith('On the road')
    reject(player, 'creator.start', '<script>')
    act(player, 'creator.start', text='Family & Fog')
    assert player.state['creator']['name'] == 'Family & Fog'
    reject(player, 'creator.start', 'Second channel')
    reject(player, 'creator.buy.lens-prime')
    reject(player, 'creator.buy.monitor-basic')
    reject(player, 'creator.buy.invented')
    reject(player, 'creator.publish')
    reject(player, 'creator.withdraw')
    reject(player, 'creator.reedit.1')

    start = player.refresh()
    stream = Stream(player)
    stream.next()
    try:
        buy_body = act(player, 'creator.buy.light-clip')
        assert player.state['hud']['balance'] == start['hud']['balance'] - 19
        assert player.state['creator']['orders'][0]['daysRemaining'] == 1
        assert player.state['creator']['kit']['ceiling'] == 50000
        status, stale, _ = player.request('/api/game/actions', buy_body)
        assert status == 409 and not stale['accepted'], 'Purchase replay rejected'
        reject(player, 'creator.buy.light-clip')
        act(player, 'creator.film')
        state = settle_studio(player)
        assert state['hud']['turns'] == start['hud']['turns'] + 1, 'Filming consumes exactly one trail day'
        assert state['progress']['milesTraveled'] == start['progress']['milesTraveled'], 'Filming cannot drive'
        assert state['hud']['balance'] == start['hud']['balance'] - 27
        assert state['creator']['draft']['ceiling'] == 50000, 'Footage uses the kit at capture, before delivery'
        assert state['creator']['kit']['ceiling'] == 52000 and not state['creator']['orders']
        draft = state['creator']['draft']
        reject(player, 'creator.film')
        act(player, 'creator.leave')
        player.settle()
        act(player, 'creator.open')
        state = settle_studio(player)
        assert state['creator']['draft'] == draft and state['creator']['name'] == 'Family & Fog'
        publish_body = act(player, 'creator.publish')
        state = settle_studio(player)
        assert state['creator']['stats']['uploads'] == 1 and state['creator']['draft'] is None
        video = state['creator']['videos'][0]
        assert video['views'] == video['subscriberViews'] + video['discoveryViews']
        assert video['views'] <= draft['ceiling']
        assert video['location'] == draft['location']
        status, stale, _ = player.request('/api/game/actions', publish_body)
        assert status == 409 and not stale['accepted'], 'Publish replay rejected'
        reject(player, 'creator.publish')
        streamed = stream.next(lambda s: (s.get('creator') or {}).get('stats', {}).get('uploads') == 1)
        assert streamed['creator']['videos'][0] == video, 'Stream carries authoritative analytics'

        assert_public_payload(state)
        if video['demonetized']:
            old_turns = state['hud']['turns']
            old_subs = state['creator']['stats']['subscribers']
            act(player, f'creator.reedit.{video["id"]}')
            state = settle_studio(player)
            repaired = state['creator']['videos'][0]
            assert repaired['views'] == video['views'] // 2 and repaired['reedited']
            assert not repaired['demonetized'] and state['creator']['stats']['subscribers'] == old_subs
            assert state['hud']['turns'] == old_turns + 1
            reject(player, f'creator.reedit.{video["id"]}')

        act(player, 'creator.tab.shop')
        assert player.state['creator']['tab'] == 'shop'
        act(player, 'creator.tab.library')
        assert player.state['creator']['tab'] == 'library'
        stats = player.state['creator']['stats']
        act(player, 'creator.leave')
        player.settle()
        act(player, 'creator.open')
        state = settle_studio(player)
        assert state['creator']['stats'] == stats, 'Reopening does not duplicate payouts, views or day costs'
        assert other.refresh() == untouched, 'Career cannot affect another journey'
        print('PASS creator HTTP: start mid-drive, park, free browsing, names, compatibility, wallet, orders, real trail days without miles, preserved drafts, exact-once publish, SSE analytics, hidden scores, tabs, isolation')
        print(json.dumps({'video': video['title'], 'views': video['views'], 'demonetized': video['demonetized'], 'stats': stats}))
    finally:
        stream.close()


if __name__ == '__main__':
    main()
