# Vocal Coach

A singing trainer that listens to you sing and shows, in real time, whether you're hitting the note.
It includes guided vocal exercises, coaching feedback, practice routines, a tuner, a range finder and progress tracking.

Built with .NET 10 MAUI so the same code runs on **Windows** (for development) and **iPhone**.

## Run it

```bash
dotnet run --project src/VocalCoach.App -f net10.0-windows10.0.19041.0
```

Run the tests:

```bash
dotnet test tests/VocalCoach.Core.Tests
```

**Testing without singing (Debug builds only):** set `VOCALCOACH_LOOPBACK=1` and turn on *Settings › Play along while I sing*.
The microphone is replaced by the app's own reference audio, detuned 12¢ flat, so you can watch the whole pipeline work.

## How it works

```
Microphone ─► PitchTracker (YIN) ─► PitchSample stream ─► PracticeSession ─► NoteScorer ─► SessionSummary ─► CoachingEngine
                                           │                    ▲
                                           ▼                    │
                                   PitchGraphDrawable ◄── SessionPlan (timeline of Listen/Sing notes) ─► ToneSynth ─► Speaker
```

| Project | What's in it | Platform code? |
|---|---|---|
| `src/VocalCoach.Core` | Pitch detection (YIN + median smoothing), note maths, exercise library, session timeline, scoring, coaching rules, lessons, JSON progress store | **None.** Plain `net10.0` |
| `src/VocalCoach.App` | MAUI UI (pages built in C#), drawables for the pitch graph, tuner needle and score chart | Only `Platforms/Windows/WindowsAudio.cs` and `Platforms/iOS/IosAudio.cs` |
| `tests/VocalCoach.Core.Tests` | 25 xUnit tests, including pitch accuracy from E2 to C6 and an end-to-end synth → tracker check | |

### The singing pedagogy
- **Exercises** (`ExerciseLibrary.cs`) follow how a teacher runs warm-ups at the piano. The app plays a pattern, you sing it back, and it moves up a semitone each time, staying inside your *comfortable* range.
- **Tolerance** depends on your level: ±40¢ for Beginner, ±25¢ for Intermediate, ±15¢ for Advanced. The target bars on the graph are drawn exactly that thick, so keeping your line inside a bar means you're in tune.
- **Scoring** ignores the first 150 ms of each note, because finding a pitch takes a moment. It measures time on target, steadiness (cents standard deviation), scooping into notes, and octave errors.
- **Coaching** (`CoachingEngine.cs`) turns those numbers into specific advice: flat or sharp tendencies, instability, scooping, and weak upper or lower registers. It also picks a daily routine (warm-up → your weakest areas → cool-down).

## Moving to iPhone

The iOS target already compiles (`dotnet build src/VocalCoach.App -f net10.0-ios`). To run it on a device:
1. Pair Visual Studio with a Mac running Xcode, or build on the Mac directly.
2. Set your Apple team and a real bundle ID (`ApplicationId` in the csproj, currently `com.vocalcoach.app`).
3. The microphone prompt text is already in `Platforms/iOS/Info.plist` (`NSMicrophoneUsageDescription`).
4. Audio uses `AVAudioEngine` with a `PlayAndRecord` session, so reference tones and the microphone run together.

Still to do for a polished iOS release:
- Add tab bar icons (the tabs currently show text only).
- Test `IosAudio.cs` on a real device. It compiles but hasn't been run.
- Optionally move audio to a single shared `AVAudioEngine`, which gives tighter sync between playback and input.
- Recommend headphones for "Play along", or add echo cancellation (`AVAudioSession` voice-processing mode).

Data is stored as JSON under `FileSystem.AppDataDirectory/VocalCoach/` (`profile.json`, `history.json`). Crashes are appended to `crash.log` in the same folder.
