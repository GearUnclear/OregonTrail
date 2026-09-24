/* Authored character art. Decorative only; game information lives in semantic HTML. */
(() => {
  const vehicles = Object.freeze({
    minivan: String.raw`
          _________________________
         /  ____  ____  ____  |     \
        |  |____||____||____| |      \___
        |                 __ | __      _|
        |___ /---\___________|___/---\__|
             (o)                 (o)`,
    pickupcamper: String.raw`
       .-------------------.
       |  CAMPER  [______] |   _______
       |___________________|__/__|___\___
       |___________________|  __ |      _|
        \___ /===\_________|_____/===\__|
             ((o))               ((o))`,
    hybridcrossover: String.raw`
                 __========__
             ___/____|___|___ \_
            /  /_____|___|___\  \____
           /              __ |  HYB  _\
           \___/---\_________|_/---\__|
                (o)             (o)`,
    electrichatchback: String.raw`
                    .-----------.
                   /  ___ | ___  \
                  /__/___||____\  \_
                  |  [:::]  __ |   _\
                  \__/---\_____|/---\|
                      (o)        (o)`,
  });

  // Fixed dimensions keep the art still as individual characters animate. Vehicle identity
  // comes from the API, never from a translated display name or a menu position.
  function vehicleFrame(vehicleId, step = 0, moving = false, compact = false) {
    const width = 54;
    const rows = Array.from({ length: compact ? 8 : 11 }, () => Array(width).fill(" "));
    const stamp = (text, x, y) => {
      [...text].forEach((character, index) => {
        if (x + index >= 0 && x + index < width) rows[y][x + index] = character;
      });
    };
    const id = Object.hasOwn(vehicles, vehicleId) ? vehicleId : "minivan";
    const wheel = moving ? ["o", "+", "o", "x"][step % 4] : "o";
    let body = vehicles[id].trimEnd().slice(1).replaceAll("(o)", `(${wheel})`);
    if (id === "electrichatchback" && moving) {
      body = body.replace("[:::]", ["[:::]", "[.::]", "[:.:]", "[::.]"][Math.floor(step / 2) % 4]);
    }
    const top = compact ? 0 : 3;
    body.split("\n").forEach((line, index) => stamp(line, 0, top + index));
    if (!compact) {
      const drift = moving ? Math.floor(step / 7) : 0;
      const skyWidth = width + 5;
      const cloudX = (39 - drift % skyWidth + skyWidth) % skyWidth - 5;
      stamp(".---.", cloudX, 0);
      stamp("(___)", cloudX, 1);
      stamp("\\ | /", 46, 0);
      stamp("- O -", 46, 1);
      const scenery = "   /\\      /\\_/\\            /\\        /\\_/\\       ".repeat(3);
      const horizonOffset = moving ? Math.floor(step / 3) % 54 : 0;
      stamp(scenery.slice(horizonOffset, horizonOffset + width), 0, 2);
    }
    if (moving && id !== "electrichatchback") {
      const tail = { minivan: 8, pickupcamper: 7, hybridcrossover: 11 }[id];
      stamp(["  .", " . ", ".  ", "   "][step % 4], tail - 4, top + 4);
    }
    const road = "____    ____    ____    ____    ____    ____    ____    ____    ";
    const offset = moving ? (step * 2) % 8 : 0;
    stamp(".".repeat(width), 0, rows.length - 2);
    stamp(road.slice(offset, offset + width), 0, rows.length - 1);
    return rows.map((row) => row.join("")).join("\n");
  }

window.TrailScenes = Object.freeze({
  vehicles,
  vehicleFrame,
  // Original Sunshine State Mutual letterhead from SceneArt.SmilingSun (a5a9de4a).
  smilingSun: String.raw`            ##     ##     ##
            ####  ####  ####
      ##     ##          ###    ##
       ####   ############   ####
        #  ##################  #
  ###### ###################### ######
    ### ######################## ###
        ######   ######    #####
   ### ######## ######## ######## ###
  #### ########################## ####
        ########################
    ### #######  ######  ####### ###
  ###### #########    ######### ######
        #  ##################  #
       ####   ############   ####
      ###    ##          ###   ###
            ####  ####  ####
            ##     ##     ##`,
  road: String.raw`
                                     \  |  /
                                   -- .---. --
                .                     (   )
             .     .                 '---'
     _      .       .      _        /  |  \
    / \    /\       /\    / \             .
 __/   \__/  \_____/  \__/   \___/\______/ \___
                .                 .
  .     .             /  :  \        ___________
                     /   :   \      | SEATTLE  |
        _           /    :    \     |  KEEP NW |
     --(_)--       /     :     \    |__________|
       /|\        /             \         ||
      / | \      /       :       \        ||
        |       /        :        \       ||
  ......|....../                   \......||...
              /          :          \
             /           :           \
            /                         \
      _____/____         :             \
     / _|___|_  \        :              \
    | |       |  |                       \
    | |_______|  |       :                \
    |  []   []   |       :                 \
    |____________|                          \
     (_)      (_)        :                   \
 ....................... : ....................`,
  store: String.raw`
               __________________________
              / / / / / / / / / / / / / /\
             /_/_/_/_/_/_/_/_/_/_/_/_/_/  \
             |   FUEL  /  FOOD  /  AMMO   |
             |  ____   __________   ____ |
             | |    | |  OPEN    | |    ||
          ___|_|____|_|__________|_|____||___
            [ $ ]                      [ $ ]
            |___|                      |___|
        ....|   |......................|   |....`,
  river: String.raw`
           ROAD CLOSED // WATER OVER ROAD

                 |\              /|
        _________|_\            /_|_________
          _   _     \          /     _   _
                     \        /
         ~  ~ ~~~  ~~~~ ~~~~ ~~~  ~~ ~  ~
          ~~~~  ~  ~  ~~~~~~  ~  ~~~~ ~~~
        ~  ~ ~~~  ~~   ~  ~~  ~~~   ~  ~~
          ~~   ~ ~~~~~   ~~~~  ~  ~~~~  ~`,
  event: String.raw`
                         /\
                        /!!\
                       / !! \
                      /  !!  \
                     /   ..   \
                    /__________\
                         ||
          _______________||_______________
         . . . . . . . . || . . . . . . . .`,
  end: String.raw`
                     ___________
                    /           \
                   /  END OF THE \
                  |     ROAD      |
                  |               |
                  |   .  -  .     |
                  |_______________|
              ....|...............|....`,
  seattle: String.raw`
                         |
                      ___|___
                     /_______\
                      \_____/
                        | |
                _       | |       _
           ____| |___   | |   ___| |____
          | [] [] [] |  | |  | [] [] [] |
          | [] [] [] | /   \ | [] [] [] |
       ...|__________|/_____\|__________|...`,
});
})();
