# AgriVerse Unity checkpoint

This Unity 6.5 client reads the existing Express `GET /api/scenario`
endpoint. Unity contains no OpenAI key, prompts, private stakeholder goals,
or model calls.

## Start the existing backend

From the repository root:

```sh
npm run dev:server
```

The Editor URL is `http://localhost:8787`. The local Web build is served on
`http://localhost:8000`; both that origin and the Vite development origin are
in the backend's default CORS allowlist. Override the exact browser origins
with `CORS_ALLOWED_ORIGINS` in the backend `.env`.

## Manual Unity Editor setup

Do these clicks in Unity `6000.5.4f1`:

1. In Unity Hub, click **Projects**, click **Add**, choose this repository's
   `unity` directory, select Editor `6000.5.4f1`, and click the project.
2. In the Project window, double-click
   `Assets/Scenes/SampleScene.unity`.
3. In the Hierarchy, click **GameObject > UI > Legacy > Text** (shown as
   **Text - Legacy** in some layouts). Unity also creates a Canvas and
   EventSystem when needed.
4. Rename the new text object to `ScenarioTitleText`.
5. Select `ScenarioTitleText`. In its Rect Transform, choose the center/stretch
   anchor preset, give it enough width for one title line, and place it in the
   visible center of the Canvas.
6. In the Text component, set the temporary text to `Loading scenario…`,
   alignment to centered, and a readable font size and color against the
   camera background.
7. In the Hierarchy, click **GameObject > Create Empty** and rename it
   `ScenarioBootstrap`.
8. With `ScenarioBootstrap` selected, click **Add Component**, search for
   `Scenario Title Presenter`, and click it.
9. Drag `ScenarioTitleText` from the Hierarchy into the presenter's
    **Scenario Title Text** field.
10. Leave **Editor Api Base Url** as `http://localhost:8787`.
11. For the local Web verification, leave **Web Api Base Url** as
    `http://localhost:8787`. For a deployed build, replace it with the HTTPS
    Express origin and add the Web host's exact origin to
    `CORS_ALLOWED_ORIGINS` on the backend.
12. Click **File > Save**.
13. With `npm run dev:server` running, click the Unity **Play** button. Confirm
    that the UI changes from `Loading scenario…` to the title returned by
    `/api/scenario`, then click **Play** again to stop. Confirm the Console has
    no errors.

## Manual Web build

1. Click **File > Build Profiles**.
2. Click **Add Build Profile**, choose **Web**, then click **Add Build Profile**.
3. Select the Web profile and click **Switch Profile** if it is not active.
4. In **Scene List**, click **Add Open Scenes** and ensure only the intended
   checkpoint scene is enabled.
5. Click **Build**.
6. Choose `unity/Builds/WebGL` as the output directory and confirm. `Builds/`
   is intentionally ignored by Git.
7. From the repository root, serve the Brotli-compressed result with Unity's
   bundled Web server so the `.br` files receive the required
   `Content-Encoding` headers:

   ```sh
   '/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/Resources/Scripting/MonoBleedingEdge/bin/mono' \
     '/Applications/Unity/Hub/Editor/6000.5.4f1/PlaybackEngines/WebGLSupport/BuildTools/SimpleWebServer.exe' \
     "$PWD/unity/Builds/WebGL" \
     'http://localhost:8000/'
   ```

   Leave this terminal running during the browser check.
8. Open `http://localhost:8000` in a supported browser. In browser developer
   tools, confirm `GET http://localhost:8787/api/scenario` returns `200`, the
   response has `Access-Control-Allow-Origin: http://localhost:8000`, and the
   scenario title appears in the Unity canvas.

## Investigation manual Unity Editor setup

The Investigation stage creates its UI, gray primitive test-site markers, and
the persistent Evidence Notebook at runtime. It has no data-entry fields or
scene-object references to assign: the only content source is the existing
sanitized `GET /api/scenario` response.

1. Open `Assets/Scenes/SampleScene.unity`.
2. If you completed the title-only checkpoint setup above, select
   `ScenarioBootstrap` and `ScenarioTitleText` in the Hierarchy, then press
   **Delete**. The Investigation controller now supplies its own scenario title
   and status text; removing the earlier placeholder prevents duplicate UI.
3. In the Hierarchy, choose **GameObject > Create Empty** and name it
   `InvestigationBootstrap`.
4. With `InvestigationBootstrap` selected, click **Add Component**, search for
   **Investigation Controller**, and add it.
5. Leave **Editor Api Base Url** as `http://localhost:8787`.
6. Leave **Web Api Base Url** as `http://localhost:8787` for a local Web build.
   For a deployed Web build, replace it with the HTTPS Express origin and add
   the build host's exact origin to the backend `CORS_ALLOWED_ORIGINS` value.
7. Do not assign the four **Preserve** fields. They intentionally retain the
   Unity primitive component types required in a stripped Web build.
8. Click **File > Save**.
9. From the repository root, start the existing backend with
   `npm run dev:server`.
10. In Unity, click **Play**. The Game view should show three gray cubes with
    scenario-supplied labels, a selected-reading panel, and an Evidence
    Notebook. The controller creates or upgrades the EventSystem to the Input
    System UI module automatically.
11. Click one gray cube. Its readout should show salinity, sample season, salt
    pattern, freshwater access, note, and source IDs from the backend. Click
    **Collect sample** to record it.
12. Repeat for every marker. The notebook should retain all readings and the
    gate should change from **Interviews locked** to **Interviews unlocked**
    only after every scenario test site is recorded.
13. Click **Play** again to stop. Do not create stakeholder or proposal UI yet;
    the next authorized stage is Interviews.

## Interviews manual Unity Editor setup

The Interviews stage builds its gray stakeholder markers, chat UI, portrait
request, retry control, and plan gate at runtime. It reads public stakeholder
identity and persona from the existing sanitized scenario and sends questions
only to the existing Express stakeholder endpoint.

1. Keep `InvestigationBootstrap` in `SampleScene`; its session evidence is what
   makes the Investigation-to-Interviews handoff meaningful during one Play
   session.
2. In the Hierarchy, choose **GameObject > Create Empty** and name it
   `InterviewBootstrap`.
3. With it selected, click **Add Component**, search for **Interview
   Controller**, and add it.
4. Leave **Editor Api Base Url** and **Web Api Base Url** at
   `http://localhost:8787` for local verification. Use the same deployed HTTPS
   Express origin and CORS configuration described above for a Web deployment.
5. Click **File > Save**. No UI references, marker prefabs, portraits, or
   stakeholder IDs need to be assigned in the Inspector.
6. Start the existing backend from the repository root with `npm run dev:server`.
7. Click **Play**. During Investigation the entire Interviews stage is hidden
   and receives no pointer input. Record every water sample first; then the
   gray stakeholder cylinders and chat appear. The chat shows their
   scenario-provided name, role, public persona, prior Q&A, question field,
   send button, and retry button.
8. Ask at least one question of every stakeholder. The plan gate remains locked
   until every stakeholder has a recorded reply, then changes to **Plan
   unlocked**. A failed request shows a readable error and enables **Retry**.
9. Portraits first request an optimized WebP derivative and fall back to the
   existing optimized JPEG; if neither can load, the UI displays the
   stakeholder's name-and-role badge. Asset provenance remains in
   `public/assets/ATTRIBUTIONS.md`.
10. Click **Play** again to stop. Do not create the plan UI yet; the next
    authorized stage is the plan builder.

## Runtime layout contract

The runtime keeps one active activity panel at a time. The title and current
instruction share the top bar; the current activity uses the left column; the
Evidence Notebook remains in the right column; chat input is the bottom row.
When Interviews starts, the water-reading panel hides. When Plan starts, the
chat panel hides. Passive panel, text, and portrait graphics do not intercept
world-marker clicks.

## Plan Builder manual Unity Editor setup

1. Keep `InvestigationBootstrap` and `InterviewBootstrap` in `SampleScene`.
   They provide the evidence and interview gates for the same Play session.
2. Choose **GameObject > Create Empty** and name it `PlanBootstrap`.
3. Click **Add Component**, search for **Plan Controller**, and add it.
4. Leave both API URL fields at `http://localhost:8787` for local use; use the
   deployed HTTPS Express URL and its CORS configuration for a Web deployment.
5. Click **File > Save**, start the existing backend with `npm run dev:server`,
   and click **Play**.
6. After all three stakeholder replies, finish reading the open conversation
   and click **Continue to planning**. The chat then closes before the plan form
   appears in the left column. Select a target site, one or more interventions,
   optional support measures and parameters, then enter a rationale and click
   **Run simulation**.
7. The result confirms `fit_assessment.overall`. It intentionally does not
   display consequences yet; that belongs to the next stage.

For a repeatable pipeline check, the Editor also provides
**AgriVerse > Build > Checkpoint Web**. It creates a temporary primitive
verification scene, builds it to `Builds/WebGL`, and removes that temporary
scene without changing `SampleScene`. The active build profile must already
be Web.

## Consequences manual Unity Editor setup

1. Keep `InvestigationBootstrap`, `InterviewBootstrap`, and `PlanBootstrap` in
   `SampleScene`; all four stages share one runtime session.
2. Choose **GameObject > Create Empty** and name it `ConsequencesBootstrap`.
3. Click **Add Component**, search for **Consequences Controller**, and add it.
   It has no Inspector references or API fields: it waits for the Plan Builder's
   saved simulator response.
4. Save the scene, start the backend with `npm run dev:server`, then click
   **Play**. Complete Investigation, Interviews, and a plan simulation.
5. The left column switches to Consequences. Use **Previous year** and **Next
   year** to inspect the five stored years. The right Evidence Notebook stays
   visible. With the Feedback and Policy Brief bootstraps below also present,
   **Get feedback** starts the live feedback request.

## Feedback, revision, and brief manual Unity Editor setup

1. Keep `InvestigationBootstrap`, `InterviewBootstrap`, `PlanBootstrap`, and
   `ConsequencesBootstrap` in `SampleScene`.
2. Create an empty `FeedbackBootstrap` object and add **Feedback Controller**.
3. Create an empty `PolicyBriefBootstrap` object and add **Policy Brief
   Controller**. Neither component needs Inspector references; they share the
   existing session and use the same local API URLs as the prior controllers.
4. Save the scene, start `npm run dev:server`, and click **Play**. After a
   simulation, **Get feedback** requests the live grounded grader result.
5. Choose **Revise plan** to return to the Plan Builder with the previous site,
   interventions, support measures, parameters, and rationale intact. Submit a
   revised plan, review its replacement consequences, request new feedback,
   then choose **Generate policy brief**. The final scrollable panel ends with
   `Investigation complete`.

## Locked implementation order

After this checkpoint, product work proceeds only in this order:

1. investigation
2. interviews
3. plan
4. simulation
5. feedback
6. revision
7. brief

Each stage stays on primitives until the complete loop works. No visual polish
starts before that loop is complete.
