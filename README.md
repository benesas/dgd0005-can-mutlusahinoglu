# Pong

**Student:** Can Mutluşahinoğlu

**Student ID:** 2400004837

Pong is a simple two-player game made with Unity 6000.3.2f1 and FishNet. Each player owns one paddle and moves only that paddle. The ball has no player owner: the server creates it and handles its movement, collisions and goals. Both players share the same score, serving side and recent event list. The first two clients get LEFT and RIGHT; any extra client watches as a spectator. In Homework 2, clients send an input axis each tick. The server validates and rate-limits that input, moves the paddle, and sends its position to every client. The ball and match state also come from the server.

## Running the server and clients

1. Open the Pong project and its Pong scene in Unity 6000.3.2f1; in a separate game instance, enter Play Mode and click **Start Server** only (UDP port 7770).
2. Start the first client in another instance, enter the server's IPv4 address (or 127.0.0.1 on the same computer) and click **Start Client**; it receives LEFT.
3. Start the second client in another instance or on another computer, enter the server's IPv4 address and click **Start Client**; it receives RIGHT.

W/S or the arrow keys move the local paddle. Space requests a serve. LEFT serves first, then the player who conceded serves next.

For a dedicated-server setup, keep the server and clients in separate processes. Starting both server and client in one process also works, but that is a host setup.

## Project

- Scene: `Pong/Assets/Scenes/Pong.unity` (from this folder).
- FishNet source: official 4.7.3 release tag, labelled 4.7.3R on the releases page. The upstream source still contains a 4.7.2 version constant.
- Multiplayer Play Mode is included. A player tagged `Server` starts the server through ServerBootstrap.
- Network tick rate: 30. Paddle interpolation: 2. Both paddles and the ball use server authority.
- Each paddle accepts at most one eligible input per 30 ms of server tick time.
- C sends twenty input requests, X sends axis 50, and T tries a local teleport. These debug keys are required by Homework 2.
- Previous homework answers: [Week 3](Week03/README.md).
- Current homework answers and evidence: [Week 4](Week04/README.md).

