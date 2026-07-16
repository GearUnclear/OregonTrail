#!/bin/bash
# Launch N parallel GLM playthroughs. Usage: swarm_glm.sh N TURNS MODEL TAGPREFIX
# Defaults reflect the working provider in this env (opencode-go/glm-5.2).
cd /root/mobile-dev/OregonTrail
N=${1:-6}; TURNS=${2:-340}; MODEL=${3:-opencode-go/glm-5.2}; PFX=${4:-glm}
pids=()
for i in $(seq 1 $N); do
  python3 agent/play_glm.py --turns $TURNS --model "$MODEL" --tag "${PFX}$i" \
    > agent/swarm-${PFX}$i.log 2>&1 &
  pids+=($!)
  sleep 5   # opencode cold-start is heavier than claude -p; stagger more
done
echo "launched ${#pids[@]} $MODEL runs (tag ${PFX}1..${PFX}$N): ${pids[*]}"
wait
echo "ALL DONE"