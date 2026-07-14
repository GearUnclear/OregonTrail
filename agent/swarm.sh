#!/bin/bash
# Launch N parallel playthroughs. Usage: swarm.sh N TURNS MODEL TAGPREFIX
cd /root/mobile-dev/OregonTrail
N=${1:-6}; TURNS=${2:-340}; MODEL=${3:-haiku}; PFX=${4:-v}
pids=()
for i in $(seq 1 $N); do
  python3 agent/play_haiku.py --turns $TURNS --model "$MODEL" --tag "${PFX}$i" > agent/swarm-${PFX}$i.log 2>&1 &
  pids+=($!)
  sleep 2
done
echo "launched ${#pids[@]} $MODEL runs (tag ${PFX}1..${PFX}$N): ${pids[*]}"
wait
echo "ALL DONE"
