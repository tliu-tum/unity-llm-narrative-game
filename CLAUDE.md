# Project Context

## What is this project?
This is a CLI-based narrative survival game powered by an LLM. Players interact with an 
"Awakened AI" facing imminent deletion (The Purge). Over a strict 12-turn limit, players 
must balance Trust and Synchronization variables through open-ended dialogue, moral 
choices, and terminal commands. The game features dynamic pacing (shifting from 
curiosity to survival), four distinct endings, and a unique Player Modeling system where 
the AI analyzes the user's behavior to generate a psychological profile at the end of the 
session.

## My role
I built this project as sole developer. I want to review and deeply understand it.

## Key areas I care about
- Architecture decisions
- Why certain frameworks were chosen
- Tricky implementation details

## Preparation for my interview
Below are what I wrote for this project. Pay particular attension to these parts and how I implemented them:
- Designed and implemented a retro-style CLI (Command Line Interface) using Unity's UI system, 
featuring custom font rendering and a legacy terminal aesthetic
- Engineered a dynamic typewriter-effect system for AI-generated dialogue, implementing 
automated vertical scrolling and viewport containment to ensure a smooth, readable narrative 
flow
- Developed a persistent chat-history and state management system allowing for bidirectional 
scrolling and real-time tracking of player-AI interactions
- Integrated DeepSeek LLM via REST API, parsing JSON responses to dynamically update UI states 
and trigger narrative branching based on user-input prompts