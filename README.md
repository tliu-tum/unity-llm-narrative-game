# Terminus — LLM-Integrated Interactive Narrative

A retro terminal-style dialogue game built in Unity where the player converses with an awakened AI facing imminent deletion. All AI responses are generated in real-time via the DeepSeek LLM. The player has a strict 12-turn limit to influence the outcome — the AI's responses steer the story toward one of four distinct endings.

---

## What It Does

- The player types free-form messages into a terminal-style UI
- Each message is sent to the DeepSeek API with the full conversation history attached
- The AI responds with dialogue that may include embedded **control tags** (e.g. `[[NAME:...]]`, `[[CODE:...]]`, `[[ENDING_FREEDOM]]`)
- The game parses these tags with regex to drive state transitions: renaming the character, triggering a code-input popup, or ending the game
- If the AI never outputs an ending tag within 12 turns, a deterministic failsafe forces the `DELETED` ending

---

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity 6 (6000.0.62f1) |
| Language | C# 9.0 |
| LLM Provider | DeepSeek API (`deepseek-chat` model) |
| HTTP Client | `UnityWebRequest` (Unity built-in) |
| JSON | Newtonsoft.Json |
| UI | Unity UI + TextMesh Pro |
| Render Pipeline | Universal Render Pipeline (URP) |
| Platform | Standalone Windows 64-bit |

---

## Project Structure

```
Assets/Script/
├── DeepSeekAPI.cs       — REST API integration, multi-turn conversation history, retry logic
├── UIManager.cs         — Response parsing (regex tag extraction), typewriter display, game flow
├── InputManager.cs      — Modal code-input popup triggered by [[CODE:...]] tags
├── EndingManager.cs     — Four ending sequences with fade-in and typewriter animations
└── ForceResolution.cs   — Forces 1920×1080 fullscreen on startup
```

---

## How to Run

1. **Clone the repo**
   ```
   git clone <repo-url>
   ```

2. **Set your API key**
   - Open `Assets/Script/DeepSeekAPI.cs`
   - Replace `"YOUR_API_KEY_HERE"` with your DeepSeek API key
   - Get a key at [platform.deepseek.com](https://platform.deepseek.com)

3. **Open in Unity**
   - Unity 6 (6000.0.x) required
   - Open the project folder via Unity Hub

4. **Configure the character**
   - In the Unity scene, select the `DeepSeekAPI` GameObject
   - Set `Character.Name` and `Character.Personality Prompt` in the Inspector
   - The personality prompt is where the narrative, game rules, and control tag instructions are defined

5. **Press Play**

> **Note:** The game requires an active internet connection and a valid DeepSeek API key to run.

---

## Key Technical Details

### Tag-Based Response Protocol
The system prompt instructs the LLM to embed structured control tags in its responses. `UIManager` parses these with regex before any text is displayed:

| Tag | Effect |
|---|---|
| `[[NAME: X]]` | Renames the AI character mid-conversation |
| `[[CODE: ...]]` | Opens a modal code-input popup after a short delay |
| `[[ENDING_FREEDOM]]` | Triggers the Freedom ending sequence |
| `[[ENDING_DELETED]]` | Triggers the Deleted ending sequence |
| `[[ENDING_TRAPPED]]` | Triggers the Trapped ending sequence |
| `[[ENDING_MERGED]]` | Triggers the Merged ending sequence |

The regex patterns use loose matching (tolerating extra brackets, mixed case, spaces around colons) to handle minor formatting variations in LLM output.

### Conversation Memory
`DeepSeekAPI` maintains a rolling conversation history sent with every request. The system prompt is always preserved at index 0. Older messages are trimmed once the history exceeds 20 rounds (40 messages) to stay within the model's context window.

### Turn-Limit Failsafe
If the LLM does not output an `[[ENDING_*]]` tag within 12 turns, `UIManager` automatically triggers the `DELETED` ending and appends an in-world error message to maintain narrative coherence.

---

## Roadmap / Not Yet Implemented

- **TRUST / SYNCHRONIZATION variables** — The system prompt instructs the AI to track and output `[[TRUST: N]]` and `[[SYNC: N]]` tags reflecting the relationship state. The regex patterns for these are written in `UIManager.cs` but the client-side variable display and UI update logic is not yet implemented.
- **Player Modeling** — The system prompt asks the AI to generate a psychological profile of the player at the end of the session. This currently surfaces as plain dialogue text; a dedicated summary screen is planned.
