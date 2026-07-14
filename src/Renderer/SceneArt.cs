using System.Text;

namespace OregonTrailDotNet.Renderer
{
    /// <summary>
    ///     Central registry of ASCII art for the game's scenes. The large "hero" scenes (title, landmarks, victory,
    ///     etc.) are converted from public-domain images and live as string constants; the scrolling travel scene is
    ///     composed procedurally here so it can animate smoothly with a parallax effect.
    /// </summary>
    public static class SceneArt
    {
        // ---------------------------------------------------------------------------------------------------------
        // Hero scene art. Sized for an 80x24 console. Pieces are swapped between public-domain image conversions and
        // hand-authored art; each constant is self-contained so a render site can drop it in with no layout fuss.
        // ---------------------------------------------------------------------------------------------------------

        /// <summary>
        ///     Title splash: an open desert highway, converted from a public-domain Carol M. Highsmith
        ///     photograph (Library of Congress) of a road into the Mojave.
        /// </summary>
        public const string Title =
@"=++++***++********++=-::............-............:--=++++++++==-:::.
************++=--:... .............:*..................:--==+++++++==-
+**+*++===-::...     .........   ...-......  ..........  ...::---====+
+==---::..                ..    . ..#.          ........       ..:::--
::...                             ..#:             ......           ..
                                   .#-                  .
            ROUTE 66  --  MOJAVE DESERT  --  THE ASPHALT TRAIL";

        /// <summary>
        ///     Seattle Space Needle (victory screen and arrival at Seattle), converted from a public-domain
        ///     NOAA photograph cropped to the tower.
        /// </summary>
        public const string SeattleVictory =
@".................:..:::-------:-::::::::::::::::::......
.......:--:-=:::..:: ....::.  ...........:--::::::::::::
.........:::::....:#+. :=#*. ..:*-.....:::::::::::::::::
........::::::::::::*#==#%= .:+%+:::::::::::::::::::::::
.......::::::::::::::-%%@@:.**%-:::::::::::::::::::::::-
::::::::::::::::::::::#@@@+:*%%-::::::::::::::::--::----
:::::::::::::::::::::-%#%@-.==%*:::::::---::::----------
::::::::::::::::::::-%#-%# .-:-##=:---------------------
:-------------------#%+*@= .----*%+----------------===--
-------------------*%+=%#:.:--=--+%#====================
==================+%#+*%*.  .-====+%%+==========++++++++
+++==++++++++++==+%%#*%#*: .:=++++++#%#++++++++++*=+****
**= -++****++++++%#++#%+=:.::=*******#%#**********=+****
##: +#+####*****%%#*#%#=-  .:-********#%%######**#=+#***
     * VICTORY -- SEATTLE * THE SPACE NEEDLE *";

        /// <summary>Buc-ee's travel center (settlement arrival).</summary>
        public const string Bucees =
@"             ___________
           /// RED CAP \\\
       .--'---------------'--.
      /     ___       ___     \
     |     /o o\     /o o\     |
     |     \___/     \___/     |
     |           ()            |
     |       \         /       |
     |        '._____.'        |
     |         |[][]|          |
      \        |_||_|         /
       '.___________________.'
    =-= BUC-EE'S: FUEL . JERKY =-=";

        /// <summary>Cadillac Ranch (landmark arrival).</summary>
        public const string CadillacRanch =
@" .===================================================.
 |  CADILLAC RANCH   *   Amarillo TX   *   I-40 W    |
 '==================================================='
         /|       /|       /|       /|       /|
        /o|      /o|      /o|      /o|      /o|
       / /|     / /|     / /|     / /|     / /|
      / / |    / / |    / / |    / / |    / / |
~~~~~/_/__|~~~~/_/__|~~~~/_/__|~~~~/_/__|~~~~/_/__|~~~~~
      buried nose-down at the angle of the pyramids";

        /// <summary>
        ///     "Sunshine State Mutual" letterhead sun: the cheerful clip-art smiley that crowns the form letter
        ///     declaring the family's Florida home uninsurable at any premium.
        /// </summary>
        public const string SmilingSun =
@"            ##     ##     ##
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
            ##     ##     ##

        ~ SUNSHINE STATE MUTUAL ~
     * UNINSURABLE AT ANY PREMIUM. *
           ""HAVE A SUNNY DAY!""";

        /// <summary>Carhenge (landmark arrival), converted from a CC0 public-domain Wikimedia photograph.</summary>
        public const string Carhenge =
@"........::::::::::::::::::::::::::::::::::::--------------------
......................::::::::::::::::::::+-:::::::::::::::-----
...::::.:..................::.---::::...:#%==--::::---::::::::::
 . +*###%:::::...:--:.:..::-==*--:::::::#+:-*-*=-+%*=-::::::::::
::::*@@%+.+*+++=--**#%%*=#**=+#--::.-*++@-...:*=*@@:...-=====+++
**##@@@-..%@#%+-=#+=#@@%##*#=*@+-::.:-:=@*::::*#@@@-...*%%%%%%%%
%@@@@@%-.=%%%%%#%%%%%%@@@@@%#@@=:--.*%#%@@= ..*@@@@#+. -%@@@#+#%
##%%%%++*%%%%%%@%%%%%%%%@%@@@@@+-==-#@@@@@=:::#@@@@%#:-=%%%%###%
######*#############%%%%%%######################%%%%%%%%%%%%%%%%
            CARHENGE  --  ALLIANCE, NEBRASKA";

        /// <summary>Touchdown Jesus (landmark arrival).</summary>
        public const string TouchdownJesus =
@"    \                                /
     \            >ZAP<              /
      \                            /
       \         .----.           /
        \       (  ..  )         /
         \       '-||-'         /
          '-----.  ||  .-------'
                | #### |
                | #### |
               _/######\_
              |__________|
 [ TOUCHDOWN JESUS * King of Kings * I-75 ]";

        /// <summary>Wall Drug (settlement arrival).</summary>
        public const string WallDrug =
@"                                        ____
                                       / o  \
                                       |    |
      .-----------------.              |    |
      |   WALL  DRUG    |     _________|    |
      |  FREE ICE WATER |   _/              |
      |   DINO -> 5 MI  | _/                |
      '-------+---------'/                  |
    __________|_________/                   |
   /                                        |
   \__   _____   _____   _____   _____   ___/
      | |     | |     | |     | |     | |";

        /// <summary>
        ///     Flooded interstate (river-crossing screen), converted from a public-domain FEMA photograph of
        ///     cars in a flooded Oklahoma street.
        /// </summary>
        public const string FloodRiver =
@":-=====++++=--::::--==+====+++++++**#######*##****++****+++=-++===
====++***##=       .+%%%%%%%###****#####**+==+++====-==++=========
    .:::=+*+++=====+****++**++=====++**++===*+**+=-:-=***++==+----
     :::-=+++*******+=====--=---=--=+***+**##*##%%*=+**++++*+==---
     ...::::==+++===-::..   ...::::--++**+*+++*##==++++++====--+==
     .:..:::--===---::.         ...::::::...:-++=++-=-----=-===+++
.:::::::-----=====-----::::::::---------::--====***++++=+===-+++++
+++++++++++++**********#####################*=+******=--=====+++++
        FLOODED ROAD - FIND ANOTHER CROSSING";

        /// <summary>Compact three-line flood banner for the space-constrained river-crossing screens.</summary>
        public const string FloodRiverBanner =
@"  ~~~  WATER OVER ROADWAY  ~~~  the Interstate is a river now
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
  submerged signs, brown water, no bottom in sight...";

        /// <summary>Travel-center food counter (hunting / food-sweep screen).</summary>
        public const string FoodSweep =
@"+------------------------------------------------------------+
|  GAS-N-GULP TRAVEL CENTER    *  HOT FOOD 24HR  *  GRAB+GO  |
|   __________________    .--------------.    ____________   |
|  |[#][#][#][#][#][#]|   | BRISKET~~~~~~ |  | O  O  O  O |  |
|  |[#][#][#][#][#][#]|   | HEATLAMP||||| |  |COLA RED ICE|  |
|  | chips jerky soda |   | [=][=][=][=]  |  | ()  ()  () |  |
|  |[#][#][#][#][#][#]|   | taquitos roll |  | ()  ()  () |  |
|  |__________________|   '--------------'  |____________|   |
|    snack rows             hot-food bar        fountain     |
|  == FOOD SWEEP: grab what you can carry to the SUV ==      |
+------------------------------------------------------------+";

        /// <summary>
        ///     Headstone for the death screen, converted from a public-domain photograph of a U.S. military
        ///     headstone (Arlington National Cemetery, federal work).
        /// </summary>
        public const string Tombstone =
@"           %%%###*+==+++**#####
           %%%%*.   ...  .:*%#*
           %%%@+   ..:..   =%##
           %%%%+   .... . .*%##
           %%#%+  ....::. .#%%#
           #%%@*..........:%%##
           %%%%*..........-%%%%
           @@%@*  .::.....=%%%%
           @@@@*   .   ...*@@%%
           @%%%%*++-::::..%%%%%
           %@@@@@@%%####**###%%
           @@@%%@@@%%###%%%%%%%
               R . I . P .";

        /// <summary>
        ///     Returns scene art to show when arriving at the named location, or null if that location has no art
        ///     (the arrival screen then just shows its text). Matched on distinctive substrings of the trail names.
        /// </summary>
        /// <param name="locationName">The <see cref="Entity.Location.Location.Name" /> being arrived at.</param>
        /// <returns>Multi-line art, or null.</returns>
        /// <summary>
        ///     Grifter shoving a "graded" slabbed trading card in your face (Pokemon-card scam). Frame A of a two-frame
        ///     loop; pairs with <see cref="PokemonScammerB" /> to make him lunge and leer. Converted from AI line art.
        /// </summary>
        public const string PokemonScammerA =
@"                                ...
                   .*-   -#@@@@@@@@
                 #@@@*@@@@*.   @@@@@@@@@@@@@
               #@@ @@@*         .      .@@@@*.
          @@@@@@.                          -*@@@#
        +@@@@*@            -@@   +.   +#*   @@@**
       @@@@-          -*@@@@@+.@@@+#@@@@@#   +@@
        -@@   @@@@@@@@@*.@@@@@@@@@@*     @@@   @@
        @@   @@          .-.               @@* *@+
       -@+  @@    +#@@#-          -*@@#*.   +@@.@@
       .@# @@  +@@+   .*@@      @@#.   -@@#  *@@@@
        @@*@- @@         +@.   @@         @@  @@@*
      -*@@@@  @-    #@@-  @@  #@   @@@     @* -@@@*-
    -@@**@@+  @-    @@@*  @@  #@  -@@@     @+  @@**@@@
    @@ -@@@   #@.        #@    @@         @@   @@@+ *@-
    @@   @@    .@@#-.-+@@#      *@@*-.-*@@+    @@   @@
     @@@+@@       .+**-    @* @-   -**++-      @@*@@@.
       -#@@-   @@@@@@*.             +@@@##@@   @@#+
          @@  #@   @@-#@@@@@###@@@@@*.@.  +@  .@#
          -@#  @@#+@@   @.  #@        @-  @@  @@
           *@@  @@#@@@@@@@*+@@        @+#@+  @@
           *@@@. -@@@     ..@@       -@@#  *@@@@.
         #@@- #@@-  *@@@*-  *@  .+#@@@.  *@@-  *@@-
       +@@      +@@@+   -*#@@@@@#+.  .*@@@.      *@@
      @@*          +@@@@@#*++++*#@@@@@#.           @@.
     @@-                .-+**#**+.                  @@
    -@*   .@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@-   @@
    @@    @@                                    @@   -@*
   .@@-#@@@@  @@********@@                      @@@@#+@@
   *@@@+-*@@-.-+++*******.                     +@@*++@@@
   @@  -****#@@@                            @@@@****-  @@.
  @@          *@+                          .@@          @@
 #@-   .#@@@@@@@                            #@@@@@@#-    @@
 @@        .+@@*                            -@@+.        @@
 @@          -@@                            #@+          @@
 *@+   .#@@@@@*                              +@@@@@#.    @@
  @@*       @@                                @@       +@@
   -@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@-
        .-.                                      .--.";

        /// <summary>
        ///     The grifter mid-lunge: wider manic grin, bulging eyes, card thrust closer. Frame B of the two-frame loop
        ///     paired with <see cref="PokemonScammerA" /> for the scammer reveal animation.
        /// </summary>
        public const string PokemonScammerB =
@"                         .++
                     +@@@@@#.*@@@@@@@
                  #@@@- *@@@@*.  @@@@@@@#-
            @@@.@@@      -            .@@@@
          #@#@@@+                -*   -@@@
         @@  +-        +.  -@  #@@@@#.   *@@
    .@@@@@-          @@@- @@@@@@    +@@@.  @@
     @@@**       -@@@@@@@@#@@+         +@@ -@*
  +@@@#    .*@@@@@+  @@#.     .@@@@@@@@+ #@#@@
   .+@@  -@@*-   .-.         @@        *@+-@@@.
    @@   @@  .@@@*++*@@*    @-   .       @- @@@@@@-
    @@  +@- @@         *@. *@  @@@@@.    #@ -@@.-+@@
    +@- @@ #@     *@@@+ -@ -@  @@@@@#    @@  @@@-  @#
     @@ @@ @*    *@@@@@  @. @@ .@@@+    @@.   @@  @@.
      @@@@ @@     @@@@@ +@   -@@+.  .+@@@#.   #@@@@.
    @@@@@@  @@         #@       -*@@@@@@@@*   -@@
   @@ +@@@   +@@@*++#@@+  +@ *@      @@@. +@. .@+
   @@   @@-   -+#@@@#.           -@@@-*@  @@- *@
    @@@+-@@    -@@@@@@##***#@@@@@*@-   @@@@@  @@
      -*##@@   @.  @#   @@.  @#   @@@@#@@@@  @@
          -@@  @@@@@@---@@-+*@@@@@*@   @@+  @@
           .@@  +@@+@*--+@+.  @-   @@@@*  #@@*
            *@@@. .@@@#- @*   @@+#@@#.  @@@.-@@@.
          +@@- *@@#.  .+#@@@@@@*+   -@@@#      +@@.
         @@.      +@@@@@#*++++*#@@@@@+           *@@
       .@@             .-+****+-                   @@
      .@@    .........................---------.    @@
      @@   .@@@@@@@@@@@@@@@@@@@@@@#############@@   .@+
     .@+   +@. .**********                     *@-   @@
     @@#@@@@@. #@********@                     +@@@@@@@
     @@# +@@@@@*                             @@@@@@+ -@@
    @@         @@.                         .@@         @@
   -@+        .@@.                          @@-         @@
   @@    -#@@@@@.                           .@@@@#+     @@
   @@          @@                           @@          @@
   @@    *@@@@@@-                            @@@@@@*.   @@
    @@      -@@                               @@.      @@.
     @@@*+--+@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@#++*@@@@
       .+*##*.                                  -+*+-";

        /// <summary>
        ///     A gruff, chain-smoking celebrity cornering the party at a rest stop to push cans of his
        ///     non-alcoholic beer. Frame A of a two-frame loop (cigarette smouldering, can held low); pairs with
        ///     <see cref="TomHollandB" /> to make him take a drag and thrust the can. Hand-authored.
        /// </summary>
        public const string TomHollandA =
@"                 (
                  )     ' ' '
                 (          ' ' '
          .-------.
         / .-   -. \
        |  (o) (o)  |
        |     <     |___
        |   \___/   |   ) <- cig
         \  \___/  /
          '-.___.-'
         ___|   |___
        /   |   |   \       .-----.
       / /| |   | |\ \      | BERO|
      |_/ | |   | | \_|     |=====|
          | |___| |         | 0.0%|
          |_|   |_|         '-----'";

        /// <summary>
        ///     The same encounter mid-lunge: a fresh drag glowing, more smoke, and the can thrust right at the
        ///     viewer. Frame B of the two-frame loop paired with <see cref="TomHollandA" />.
        /// </summary>
        public const string TomHollandB =
@"              ( ' )
               ) ' (    ' ' ' '
              ( ' )        ' ' ' '
          .-------.
         / .-   -. \
        |  (O) (O)  |
        |     v     |___
        |   .---.   |###) <- drag
         \  '---'  /
          '-.___.-'      .-----.
         ___|   |___     | BERO|
        /   |   |   \===>|=====|  <- ""just try it""
       / /| |   | |\ \   | 0.0%|
      |_/ | |   | | \_|  '-----'
          | |___| |
          |_|   |_|";

        public static string ForLocation(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
                return null;
            if (locationName.Contains("Buc-ee"))
                return Bucees;
            if (locationName.Contains("Touchdown Jesus"))
                return TouchdownJesus;
            if (locationName.Contains("Wall Drug"))
                return WallDrug;
            if (locationName.Contains("Carhenge"))
                return Carhenge;
            if (locationName.Contains("Cadillac"))
                return CadillacRanch;
            if (locationName.Contains("Seattle"))
                return SeattleVictory;
            return null;
        }

        private const int RoadW = 70;

        /// <summary>
        ///     SUV drawn in side profile. Held still in the near-left of the travel scene while the world scrolls
        ///     behind it, which reads as the vehicle driving forward.
        /// </summary>
        private static readonly string[] Suv =
        {
            @"      ______________",
            @"   __/  |   |    |  \__",
            @"  |__o___________o____|"
        };

        /// <summary>Body row of the scrolling roadside props (cacti, billboard, pole), aligned to <see cref="PropBase" />.</summary>
        private static readonly string PropBody;

        /// <summary>Base row of the scrolling roadside props; the trunks/posts that meet the ground.</summary>
        private static readonly string PropBase;

        /// <summary>Repeating road surface; scrolls fastest to sell the speed.</summary>
        private static readonly string RoadLine;

        /// <summary>
        ///     Builds the fixed-width scrolling strips once. Props are stamped into equal-width strips so their
        ///     columns stay vertically aligned no matter how far the scene has scrolled.
        /// </summary>
        static SceneArt()
        {
            var body = new char[RoadW];
            var bas = new char[RoadW];
            for (var i = 0; i < RoadW; i++)
            {
                body[i] = ' ';
                bas[i] = ' ';
            }

            // A few roadside props spaced across the track: cactus, billboard, utility pole, cactus.
            PlaceProp(body, bas, 9, @"\|/", @" | ");
            PlaceProp(body, bas, 27, @"[$]", @" | ");
            PlaceProp(body, bas, 45, @" | ", @" | ");
            PlaceProp(body, bas, 61, @"_Y_", @" | ");

            PropBody = new string(body);
            PropBase = new string(bas);

            // Road surface: a dash-and-dot pattern that, scrolled fast, looks like asphalt rushing by.
            var road = new char[RoadW];
            for (var i = 0; i < RoadW; i++)
                road[i] = i % 4 == 0 ? '_' : '.';
            RoadLine = new string(road);
        }

        /// <summary>
        ///     Builds one frame of the looping "driving" scene. The SUV holds still while roadside props and the
        ///     road surface scroll leftward, faster the nearer they are to the road (parallax). Pass an
        ///     ever-incrementing step (a tick counter) to animate it.
        /// </summary>
        /// <param name="step">Monotonically increasing animation step.</param>
        /// <returns>A six-row scene string.</returns>
        public static string TravelScene(int step)
        {
            var rows = new char[6][];
            for (var r = 0; r < 6; r++)
            {
                rows[r] = new char[RoadW];
                for (var c = 0; c < RoadW; c++)
                    rows[r][c] = ' ';
            }

            // A fixed sun in the top-right sky.
            Stamp(rows, RoadW - 5, 0, new[] { @"\ | /", @"- O -", @"/ | \" });

            // Scrolling scenery and road, each at its own parallax speed (road is nearest, so fastest).
            Overlay(rows[3], Window(PropBody, step * 2));
            Overlay(rows[4], Window(PropBase, step * 2));
            Overlay(rows[5], Window(RoadLine, step * 4));

            // The SUV last, so it occludes any scenery it overlaps. Clear its bounding box first so scrolling
            // scenery cannot peek through the windows and gaps in its outline.
            ClearBox(rows, 3, 2, Suv);
            Stamp(rows, 3, 2, Suv);

            var sb = new StringBuilder();
            for (var r = 0; r < 6; r++)
                sb.AppendLine(new string(rows[r]).TrimEnd());
            return sb.ToString();
        }

        /// <summary>Stamps a two-row prop into the body/base strips at the given column, ignoring spaces.</summary>
        private static void PlaceProp(char[] body, char[] bas, int x, string topRow, string baseRow)
        {
            for (var i = 0; (i < topRow.Length) && (x + i < body.Length); i++)
                if (topRow[i] != ' ')
                    body[x + i] = topRow[i];
            for (var i = 0; (i < baseRow.Length) && (x + i < bas.Length); i++)
                if (baseRow[i] != ' ')
                    bas[x + i] = baseRow[i];
        }

        /// <summary>Returns a <see cref="RoadW" />-wide window into a strip, scrolled left by <paramref name="offset" />.</summary>
        private static string Window(string strip, int offset)
        {
            var doubled = strip + strip;
            var off = ((offset % strip.Length) + strip.Length) % strip.Length;
            return doubled.Substring(off, strip.Length);
        }

        /// <summary>Copies non-space characters from <paramref name="source" /> onto a row.</summary>
        private static void Overlay(char[] row, string source)
        {
            for (var i = 0; (i < row.Length) && (i < source.Length); i++)
                if (source[i] != ' ')
                    row[i] = source[i];
        }

        /// <summary>Blanks the bounding rectangle of <paramref name="art" /> at (x, y) so it can be drawn opaquely.</summary>
        private static void ClearBox(char[][] rows, int x, int y, string[] art)
        {
            var w = 0;
            foreach (var line in art)
                if (line.Length > w)
                    w = line.Length;
            for (var r = 0; (r < art.Length) && (y + r < rows.Length); r++)
                for (var c = 0; (c < w) && (x + c < rows[y + r].Length); c++)
                    rows[y + r][x + c] = ' ';
        }

        /// <summary>Stamps multi-row art onto the grid at (x, y), ignoring spaces so it composites cleanly.</summary>
        private static void Stamp(char[][] rows, int x, int y, string[] art)
        {
            for (var r = 0; (r < art.Length) && (y + r < rows.Length); r++)
                for (var c = 0; (c < art[r].Length) && (x + c < rows[y + r].Length); c++)
                    if (art[r][c] != ' ')
                        rows[y + r][x + c] = art[r][c];
        }
    }
}
