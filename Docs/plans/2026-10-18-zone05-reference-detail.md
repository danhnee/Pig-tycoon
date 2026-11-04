# Zone05 research observatory implementation plan

Goal: replace the 508-triangle research building with a reproducible, editable 3D observatory following the supplied 03_ResearchObservatory reference, while retaining its world placement and the existing atlas/meshbin contract.

Architecture: a focused NumPy geometry module returns vertices, atlas UVs and triangles to the existing generator. A bounded Blender review script replaces only Zone05 in a copy of the existing master, exports its meshbin and GLB, and renders matched before/after and four asset viewpoints. Existing zones and gameplay remain owned by their current sources.

Tech stack: Blender 5.3.0, NumPy, existing atlas texture and Unity meshbin format. User authorized implementation and region locks through argon_codex_collab on 18 October 2026.

- [x] Verify the MCP handoff, source file and reference image; claim generator and Zone05 locks; confirm Unity Editor is closed.
- [x] Preserve a hash-addressed baseline of Zone05, master blend and generator before edits.
- [x] Implement `Tools/observatory_geometry.py`: closed beveled masonry, timber framing, staggered slate roof tiles, arched door/windows, dome with aperture and brass telescope. Keep transform at (-7.8, 6.4, sample_ground_z), with yaw -0.05 and footprint contained in the existing local area.
- [x] Replace only the Zone05 construction block in `Tools/build_true360_glb_masterpiece.py` with the module call, retaining terrain AO/light stamps and other zones.
- [x] Implement `Tools/review_observatory.py`: open supplied master, hash other zones, render old asset with fixed studio camera, replace Zone05, validate finite data/index ranges/triangle areas/UV bounds and relative footprint, render identical camera plus side/rear/close views and full-world context, save candidate outputs. Fail on validation errors and leave live assets alone.
- [x] Run Blender review, inspect the actual images, correct two first-pass errors, then incorporate the four specific findings from Argon's independent review and render pass 3.
- [x] Ask Argon for independent review using the shared mailbox; message 4 contains the received review, and message 6 records the corrections.
- [ ] Integrate the validated Zone05 output without the current sync helper, which overwrites the canonical generator from scratch. Preserve source hashes and all unrelated outputs.
- [x] Run Core regression with `dotnet run --project Tests/PigTycoon.Runner.csproj`: 72/72 pass. Audit the focused generator diff; compile the two Editor scripts offline: 0 warnings/errors after correcting the validation JSON field initializer.
- [ ] Release locks with an explicit handoff after the pending cross-zone coordination checkpoint.

Delivered source package: `ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/`.
Engine blocker: signed official 6000.6.3f1 installer downloaded and verified, but direct installation returned Windows 740 and standard user-authorized elevation returned 1223. No wrong-version Editor was launched. Unity scene/collider/render acceptance remains pending.

Unity acceptance remains gated on the required 6000.6.3f1 executable. The currently advertised 6000.6.2f1 executable is not used. A Blender render or successful Core test is not credited as Unity rendering/collision acceptance.
