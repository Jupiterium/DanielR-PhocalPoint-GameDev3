# DanielR-PhocalPoint-GameDev3

You're in a dystopian-looking world and have to solve puzzles to move on. Game inspired by titles like Superliminal, Manifold Garden, Stanley Parable Portal.

## Legend
**WASD** -> Move around

**Hold Shift** -> Sprint

**Left Mouse Button** -> Grab objects

While grabbed, you can:

-> **press T** to transform object state

-> **hold R** to rotate the object via mouse movement

You can also aim at an object and **hold Right Mouse Button + press T** to quickly transform the object state.

## Important
PS: There seems to be an issue with level transitioning, so just load each level from Unity idividually afterwards and play/test them again.
-> Assets/Scenes/...levels...

### Notes
The game is optimized with reusable/modular scripts & prefabs, as well as instantiation and deletion of unused objects in the scene to unload the gpu and memory. Also used baked lighting, coroutines, procedural animation for ui elements, layering, and more.
