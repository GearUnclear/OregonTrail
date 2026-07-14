#!/usr/bin/env python3
# Classify a Haiku playthrough log as WIN / LOSS / INCOMPLETE, and note fired modern events.
import sys, re, os
def classify(path):
    t = open(path, errors="ignore").read()
    win = ("GameOver(GameWin)" in t) or ("GameOver(FinalPoints)" in t)
    loss = "GameOver(GameFail)" in t
    # process exited without a clean GameOver (e.g. stranded soft-lock / turn cap) => not a win
    if win and not loss: outcome = "WIN"
    elif loss and not win: outcome = "LOSS"
    elif win and loss: outcome = "WIN"  # win form wins ties
    else: outcome = "INCOMPLETE"
    events = sorted(set(re.findall(r'\b(DoorLockFire|ZeroVisibility|TheDeadCompressor|FractureCritical|TheLedgeSelfie|TheChargedLemonade|GeneralAdmission|TheSounder|EyesOffJustForASecond|TheRentalScooter|HoldMyBeer|TheColdPlunge|TheMiracleInjection|TheBossbabeTrailer|BlueSmokeRedSky|ThePredatoryTow|RedDeadRedemption3)\b', t)))
    # also detect by narrative phrases since window headers may not name the event
    phrases = {
      "carnage/wipe":"no survivors", "selfie":"vertical video of the sky","lemonade":"keeping time",
      "pileup-fog":"forty vehicles","heat-dome":"heat dome","bridge":"FRACTURE CRITICAL",
      "hogs":"sounder","crush":"held vertical","scooter":"e-scooter","coldplunge":"cold plunge",
      "ozempic":"weight-loss pens","mlm":"downline","genderreveal":"gender-reveal","tow":"from the hook",
      "rdr3":"pre-ordered",
    }
    fired = [k for k,p in phrases.items() if p.lower() in t.lower()]
    turns = len(re.findall(r'^TURN ', t, re.M))
    return outcome, turns, events, fired

if __name__ == "__main__":
    paths = sys.argv[1:]
    W=L=I=0
    for p in paths:
        o,tn,ev,fr = classify(p)
        if o=="WIN": W+=1
        elif o=="LOSS": L+=1
        else: I+=1
        print(f"{os.path.basename(p):42} {o:10} turns={tn:4}  fired={','.join(fr) if fr else '-'}")
    n=W+L+I
    print(f"\nWIN={W} LOSS={L} INCOMPLETE={I}  (n={n})  win%_of_decided={100*W/(W+L):.0f}%" if (W+L) else f"WIN={W} LOSS={L} INCOMPLETE={I}")
