# Judge access

AgriVerse Episode 1 is distributed as a universal macOS application. Judges do not need
Node.js, Unity, source code, an OpenAI API key, or OpenAI credits.

## Download and install

1. Download `AgriVerse-macOS-Universal.zip` and
   `AgriVerse-macOS-Universal.zip.sha256` from the
   [AgriVerse releases page](https://github.com/Trik-mink/AgriVerse/releases).
2. In Terminal, from the download directory, verify the archive:

   ```sh
   shasum -a 256 -c AgriVerse-macOS-Universal.zip.sha256
   ```

3. Extract the ZIP with Finder or:

   ```sh
   ditto -x -k AgriVerse-macOS-Universal.zip AgriVerse-release
   ```

4. Open `AgriVerse.app`.

The application is ad-hoc signed but not Apple-notarized. If macOS quarantines it,
Control-click `AgriVerse.app`, choose **Open**, and confirm once. Do not disable Gatekeeper.

## Controls

- Mouse drag / wheel — rotate and zoom the Field Network globe
- `Tab` / `Shift+Tab` — focus the next or previous field location
- `Enter` — select a focused location
- `WASD` and mouse — walk and look in the field world
- `E` — interact
- `N` — open or close the Field Journal
- `Escape` — close the active surface or release the cursor
- Click the world — recapture the cursor

## Expected journey

Select Vietnam, enter a name, and begin the field mission. The complete episode is:

**investigate three sites → interview three stakeholders → plan → simulate → receive
feedback → revise → compare futures → policy brief → Judge View → certificate**

The India, Kenya, Brazil, and Netherlands pins are incoming previews and do not start
placeholder missions.

## Hosted service window

The release client uses a temporary HTTPS judge service. It is configured to stop accepting
cost-bearing GPT requests no later than **August 6, 2026 at 8:00 pm Eastern
(August 7 at 00:00 UTC)**. No OpenAI key is stored in or requested by the application.

The service uses strict validation, session and IP rate limits, bounded concurrency, a
durable US$9 internal lifetime spending cap, and an explicit judging-window expiration.
Scenario loading and the global Field Network remain available without a model call.
Its secret-free readiness endpoint is
`https://agriverse-judge-api.onrender.com/health`.

## Troubleshooting

- **FIELD NETWORK OFFLINE:** Check the internet connection and choose **Retry Connection**.
  Incoming locations remain explorable; Vietnam cannot begin until its packaged scenario is
  ready.
- **Rate limit reached:** Wait for the displayed retry interval. Repeated rapid retries do
  not create successful fallback output.
- **Judge service budget exhausted:** The temporary live GPT allowance has ended. The game
  reports the condition and does not fabricate stakeholder, simulation, feedback, or brief
  results.
- **Judge access expired:** The judging window has closed. Health and explanatory behavior
  may remain available, but cost-bearing stages are intentionally disabled.
- **Cursor appears trapped:** Press `Escape`. Click the world to recapture it.
- **Text is clipped:** Use 1280×720 or 1920×1080, the two verified presentation sizes.

The project owner can inspect the public health endpoint without making an OpenAI request.
Service suspension, redeployment, and key revocation are owner-only recovery operations and
never require sharing credentials with a judge.
