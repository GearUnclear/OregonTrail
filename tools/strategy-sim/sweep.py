#!/usr/bin/env python3
"""Run policy comparisons against the compiled live game rules (no network)."""
import argparse
import collections
import concurrent.futures
import json
import pathlib
import statistics
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
DLL = ROOT / 'tools/strategy-sim/bin/Release/net8.0/OregonTrailDotNet.dll'

POLICIES = {
    'banker_minivan_steady': {},
    'banker_minivan_strenuous': {'pace': 2},
    'banker_minivan_grueling': {'pace': 3},
    'banker_minivan_11clothes': {'clothes': 11},
    'banker_minivan_11clothes_strenuous': {'clothes': 11, 'pace': 2},
    'banker_minivan_11clothes_grueling': {'clothes': 11, 'pace': 3},
    'banker_minivan_no_clothes': {'clothes': 0, 'food': 300},
    'banker_minivan_8gas': {'gas': 8},
    'banker_minivan_12gas': {'gas': 12},
    'banker_minivan_meager': {'ration': 2},
    'banker_minivan_barebones': {'ration': 3},
    'banker_minivan_no_restock': {'restock': 0},
    'banker_minivan_float': {'crossing': 2},
    'banker_minivan_ford': {'crossing': 1},
    'banker_minivan_wait': {'crossing': 3},
    'banker_hybrid': {'vehicle': 3, 'food': 196, 'transmissions': 0},
    'banker_hybrid_strenuous': {'vehicle': 3, 'food': 196, 'transmissions': 0, 'pace': 2},
    'banker_hybrid_grueling': {'vehicle': 3, 'food': 196, 'transmissions': 0, 'pace': 3},
    'banker_ev': {'vehicle': 4, 'food': 146, 'transmissions': 0},
    'banker_ev_strenuous': {'vehicle': 4, 'food': 146, 'transmissions': 0, 'pace': 2},
    'banker_ev_grueling': {'vehicle': 4, 'food': 146, 'transmissions': 0, 'pace': 3},
    'banker_pickup': {'vehicle': 2, 'food': 496, 'tires': 0, 'alternators': 0, 'transmissions': 0},
    'carpenter_minivan': {'profession': 2, 'transmissions': 0},
    'carpenter_minivan_strenuous': {'profession': 2, 'transmissions': 0, 'pace': 2},
    'carpenter_minivan_grueling': {'profession': 2, 'transmissions': 0, 'pace': 3},
    'farmer_light': {'profession': 3, 'clothes': 0, 'food': 150, 'tires': 0, 'alternators': 0, 'transmissions': 0},
    'farmer_heavy': {'profession': 3, 'pack': 1, 'clothes': 4, 'food': 296, 'tires': 0, 'alternators': 0, 'transmissions': 0},
    'farmer_heavy_grueling': {'profession': 3, 'pack': 1, 'clothes': 4, 'food': 296, 'tires': 0, 'alternators': 0, 'transmissions': 0, 'pace': 3},
    'banker_score': {'arm': 1},
    'banker_unarmed': {'arm': 2},
    'banker_solo': {'caravan': 2, 'checkpoint': 1},
    'banker_heavy': {'pack': 1},
}

def interval(successes, n):
    p = successes / n
    z = 1.95996398454
    center = (p + z*z/(2*n))/(1+z*z/n)
    half = z*((p*(1-p)/n + z*z/(4*n*n))**.5)/(1+z*z/n)
    return [round((center-half)*100, 2), round((center+half)*100, 2)]

def summarize(rows, party_size=4):
    n = len(rows)
    wins = [r for r in rows if r['Outcome'] == 'win']
    full_party = sum(r['Living'] == party_size for r in wins)
    return dict(trials=n, wins=len(wins), win_pct=round(len(wins)/n*100, 2),
                ci95=interval(len(wins), n),
                outcomes=dict(collections.Counter(r['Outcome'] for r in rows)),
                median_days_wins=statistics.median([r['Days'] for r in wins]) if wins else None,
                mean_living_wins=round(statistics.mean([r['Living'] for r in wins]), 2) if wins else None,
                full_party_wins=full_party,
                full_party_pct=round(full_party/n*100, 2),
                full_party_ci95=interval(full_party, n),
                median_score_wins=statistics.median([r['Score'] for r in wins]) if wins else None)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--trials', type=int, default=100)
    ap.add_argument('--seed', type=int, default=1)
    ap.add_argument('--workers', type=int, default=4)
    ap.add_argument('--output', default='output/strategy-analysis/exploration')
    ap.add_argument('--policies', nargs='*')
    ap.add_argument('--config', help='JSON object of policy names to option overrides')
    ap.add_argument('--dll', type=pathlib.Path, default=DLL, help='Compiled game runner to compare')
    a = ap.parse_args()
    policies = json.loads(pathlib.Path(a.config).read_text()) if a.config else POLICIES
    names = a.policies or list(policies)
    out = ROOT / a.output
    out.mkdir(parents=True, exist_ok=True)
    def run(name):
        overrides = policies[name]
        command = ['dotnet', str(a.dll.resolve()), '--trials', str(a.trials), '--seed', str(a.seed)]
        for key, value in overrides.items():
            command += ['--'+key, str(value)]
        proc = subprocess.run(command, capture_output=True, text=True, check=True)
        (out / (name+'.jsonl')).write_text(proc.stdout)
        rows = [json.loads(line) for line in proc.stdout.splitlines()]
        if len(rows) != a.trials:
            raise RuntimeError(f'{name}: expected {a.trials} outcomes, got {len(rows)}')
        errors = [r for r in rows if r['Outcome'] in ('error', 'policy-error', 'timeout')]
        party_size = 3 if overrides.get('vehicle') in (3, 4) else 4
        result = dict(name=name, options=overrides, seed=a.seed, party_size=party_size, **summarize(rows, party_size))
        if errors:
            result['errors'] = errors[:3]
        print(json.dumps(result), flush=True)
        return result
    with concurrent.futures.ThreadPoolExecutor(max_workers=a.workers) as ex:
        results = list(ex.map(run, names))
    (out / 'summary.json').write_text(json.dumps(results, indent=2)+'\n')
    if any('errors' in r for r in results):
        raise SystemExit('Some trials had harness errors; inspect results before interpreting rates.')

if __name__ == '__main__':
    main()
