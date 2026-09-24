# Travel channel idea provenance

The requested content pipeline uses 20 actual `gpt-6-luna` creator agents at `xhigh` reasoning. Each owns ten IDs and a separate editorial territory. `author-brief.md` supplies the complete story brief and requires all four primary game-story files to be read. Authors inspect the growing central pool before proposing; each author submits their own batch through `pool.py`, which uses `fcntl.flock` and atomic replacement to reject duplicate normalized titles and semantic concept keys. Authors correct collisions and editorial findings themselves.

Runtime content: `src/Module/Creator/video-ideas.json`.

`manifest.json` records the actual canonical creator task, requested model/effort, territory and accepted IDs for each batch. `batches/` holds the original author-owned submissions. `audit-report.json` and `review.md` record final structural, geographic and semantic review.

Every batch contributes six weak ideas (5–35), three middling ideas (36–69), and one great idea (80–98). At least eight ideas per batch are usable anywhere. Place-specific filming uses only the exact IDs agreed with gameplay. Seattle-only footage is excluded because reaching the destination finishes the trail. Potential scores and concept keys are internal content metadata.

Validate the accepted pool:

```bash
python3 tools/creator-content/pool.py audit --complete
python3 tools/creator-content/audit.py
```

The idea pool is fiction in the game's satirical 2028 world, not a claim that depicted businesses or locations behave this way today.
