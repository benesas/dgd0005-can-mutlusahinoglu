# Week 3 - Pong connects and replicates

**Student:** Can Mutluşahinoğlu  
**Student ID:** 2400004837

## Before submission

The original predictions were not recorded before implementation, so the answers below are retrospective and their grades are left open. Tick-rate readings still need to be added. The original step-by-step development commits were not recorded.

## Checkpoint D

**1. Which component gives the paddle an ObjectId?**

The paddle's NetworkObject carries its ObjectId. FishNet assigns the ID when the server spawns the object, so the different machines can refer to the same paddle.

**2. Who chose LEFT, and how did the client find out?**

The server chose it in PaddleController.OnStartServer. It checked which sides were already taken and wrote the first free side into Side.Value. Side is a SyncVar, so the client received the value and Place positioned and coloured the paddle.

**3. Which line stops me from moving someone else's paddle?**

`if (!IsOwner) return;` at the start of PaddleController.Update. Each machine has copies of the paddles, but only the owner copy continues to the keyboard input.

## Predictions

These are answers written after implementation, rather than a record of predictions made before Step 6.

| Question | Retrospective answer | Original prediction | Grade |
| --- | --- | --- | --- |
| I press W, but nobody else sees movement. Who needs to tell whom? | My client sends its paddle position to the server, and the server sends it to the other clients. | Not recorded | Not assessable |
| Which machine should control the real ball, and what do clients need? | The server should move the ball; clients need its position updates to draw their copies. | Not recorded | Not assessable |
| What is the first cheat if the server trusts my paddle position? | I could send a fake position and teleport my paddle to the ball, or move much faster than allowed. | Not recorded | Not assessable |

## Design sheet

| Thing | State or event? | FishNet piece | Object | Who writes or calls? |
| --- | --- | --- | --- | --- |
| Ball position | State | NetworkTransform | Ball | Server |
| Ball in play? | State | SyncVar<bool> | Ball | Server |
| Two scores | State | SyncVar<int> | GameState | Server |
| Whose serve | State | SyncVar<int> | GameState | Server |
| Let me serve | Request event | ServerRpc | Paddle | Owner client |
| LEFT scored, to all | Event | ObserversRpc | Scorer paddle | Server |
| YOU SERVE, to one | Event | TargetRpc | Conceder paddle | Server |
| Event feed | State | SyncList<string> | GameState | Server |

BallSpawner creates the ball at `(0, Court.BallY, 0)` in OnStartServer, then calls Spawn to make it a networked object. The first line of NetworkBall.Update is `if (!IsServerInitialized) return;`, so clients do not run the ball simulation.

This sheet was prepared after implementation; a separate design-sheet commit from before Step 7 is not available in this package.

## Step 8

**1. Which RPC would I use for a hit sound on all clients, and for a private warning?**

I would use an ObserversRpc for the hit sound, because all clients watching the object should hear it. I would use a TargetRpc for a message such as "not your serve", because it should go only to that player. These are choices for the question; this game does not add those extra sounds or warnings.

**2. Where is the score, and who may change it?**

LeftScore and RightScore are SyncVars on GameState. Only the server changes them. GameState.Goal updates the score and ServingSide before sending the goal and serve messages.

**3. Why can the ball pass through my paddle on my screen even though the server bounced it?**

The server used its own copy of my paddle. My input moves my local paddle immediately, but the server receives that movement later. The copies can be at slightly different positions when the hit happens. The server's collision result is the one used for the match.

## Tick-rate experiment

Use the same server, two players, network and game resolution for each run. Set NetworkManager > TimeManager > Tick Rate to 10, 30 and 60 in turn, on every participating instance. Start a fresh run after each change, keep a rally going for at least ten seconds, then read the server's **out B/s** value from its HUD. Watch the ball and the other player's paddle in a client window. Restore 30 on all project copies afterwards.

| Tick rate | Server out B/s during rally | Ball in a client window | Other paddle |
| --- | --- | --- | --- |
| 10 | Pending measurement | Pending observation | Pending observation |
| 30 | Pending measurement | Pending observation | Pending observation |
| 60 | Pending measurement | Pending observation | Pending observation |

Expected explanation, to compare with the measurements:

- **What did 60 cost?** More updates have to be sent each second, so it uses more bandwidth; the actual change depends on what moved and how FishNet packed it.
- **What did it buy?** Remote objects receive fresh positions more often, so they can look smoother and less delayed than at 10 ticks.
- **Why can the ball step even for the player moving a paddle?** My paddle responds to local input each frame, but my ball copy still depends on positions sent by the server.

The earlier local development checks are not used as the table's rally measurements. This table should contain readings from the actual exercise.

## Section 14 - Self-review

### 1. Why is there no shared now?

A message needs time to travel, so I see another machine's earlier state rather than its exact present. The lecture gives Istanbul to Frankfurt as about 35-50 ms measured RTT. At 60 fps, one frame takes about 16.7 ms, so that round trip is about 2.1-3 frames. If the route is roughly symmetric, the one-way delay is around 17.5-25 ms, or about 1-1.5 frames. The physical lower limits in the notes are 9.5 ms one way and 19 ms round trip.

### 2. How do the three topologies compare?

| | Dedicated server | Listen server | Peer-to-peer |
| --- | --- | --- | --- |
| Trust | Central server decides | Player host decides | Peers follow an agreement rule |
| Cost | Server hosting and upload | Host provides resources | Peers provide resources |
| Latency | Players connect to server | Host has a local advantage | Direct links where possible |

The intended course setup is a dedicated server: a separate process runs the match without being a player. In Step 5 that is the extra instance tagged Server, normally Player 2. In Step 11 a server instance on one laptop accepts the other laptop's client. If I also need to play on the server laptop, I use another client process. Clicking Start Server and Start Client in the same instance would instead make it a listen server/host.

### 3. Why use UDP instead of TCP?

For moving objects, a new position is often more useful than waiting for a lost old one. TCP delivers an ordered stream, so missing data can hold up later data; UDP lets the game choose what needs reliable delivery. A goal announcement should be reliable, while frequent ball position updates can normally be unreliable because newer updates replace older ones. FishNet handles the required reliability on top of the transport.

### 4. What do NetworkObject and NetworkBehaviour provide?

NetworkObject gives an object its network identity and lets FishNet spawn it, assign ownership and track its observers. NetworkBehaviour gives a script networking callbacks, ownership/server checks, SyncTypes and RPCs. Hud has neither: ConnectionHud is a normal MonoBehaviour that draws local information and calls the managers, so the HUD itself does not need to be replicated.

### 5. What runs on the different paddle copies?

For a paddle owned by client 0, using separate server and client processes:

| Copy | IsOwner | IsServerInitialized | Network start callback | May read keyboard? |
| --- | --- | --- | --- | --- |
| Server | false | true | OnStartServer | No |
| Client 0 | true | false | OnStartClient | Yes |
| Client 1 | false | false | OnStartClient | No |

Awake runs when each copy is created. OnStartServer and OnStartClient are FishNet callbacks, called when that copy is initialized for its network role. On a host, both roles can exist in the same process, so the table's separate-process assumption matters.

### 6. What changed between Steps 5 and 6?

NetworkTransform made the movement cross the network. The position travels from the owner client to the server and then to another observing client. Client Authoritative is enabled for the paddle, so this week's game trusts its owner about movement. It is disabled for the ball because only the server should decide where the ball goes.

### 7. Why were there several balls before, but only one now?

The original Court.Awake ran separately in every process and created an unrelated local ball in each one. BallSpawner.OnStartServer runs only on the server, which creates one ball and spawns its replicas for clients. A client cannot create an authoritative shared ball by calling Spawn; a normal client-side spawn request is rejected rather than becoming the server's object. Instantiate by itself would only create a local object.

### 8. Which values are state and which are events?

| Thing | Type | FishNet choice |
| --- | --- | --- |
| Score | State | SyncVar<int> on GameState |
| LEFT scored | Event | ObserversRpc |
| Whose serve | State | SyncVar<int> on GameState |
| YOU SERVE | Event | TargetRpc |
| Paddle-hit sound | Event | ObserversRpc, if added |
| Ball in play | State | SyncVar<bool> on Ball |
| Ball position | State | NetworkTransform on Ball |

The position is state, but NetworkTransform already handles it, so I do not need another position SyncVar. The feed is a SyncList even though its entries describe events, because a late joiner needs to receive the stored recent history.

### 9. Who decides a disputed hit?

The server decides using its own copies of the paddles and ball. A client may first see what looks like a hit or miss, then see the ball follow the server's result as later position updates arrive. In this game, clients do not report hits at all; NetworkBall checks them on the server. If a client tries to write InPlay directly, the normal server-write SyncVar rules prevent that from becoming shared authority. Letting clients control that value would allow an invalid serve or restart, so the owner sends a ServerRpc and the server checks it first.

### 10. What is the position bandwidth estimate?

For one client, `8 objects x 30 updates/s x 40 bytes = 9,600 bytes/s`. With four clients, the server sends about `9,600 x 4 = 38,400 bytes/s` in total. This is a rough payload estimate, rather than a precise HUD reading: headers, batching, changed-value updates and ownership settings can change the measured amount. At 10 ticks there are fewer opportunities to send fresh positions, so the expected traffic is lower and remote movement can look less smooth. My own paddle still responds to local frame input. The actual observed comparison remains to be filled in after the experiment above.

## Code questions for practice

| If asked about this code... | My answer |
| --- | --- |
| `if (!IsOwner) return;` | Only my paddle copy reads my keyboard; the other copies stop here. |
| `Side.Value = !left ? 0 : !right ? 1 : 2;` | The server chooses LEFT if it is free, otherwise RIGHT, otherwise spectator. |
| `Side.OnChange += SideChanged;` | Every copy can react when its synchronized side changes; this eventually calls Place. |
| `[ServerRpc] RequestServe()` | The owner asks the server to serve; the server checks the side and whether the ball is already in play. |
| `Serve(1 - Side.Value)` | Side 0 serves towards side 1, and side 1 serves towards side 0. |
| `if (!IsServerInitialized) return;` | Only the server runs the ball simulation or changes the protected game state. |
| `Spawn(ball.gameObject)` | The server registers the instantiated ball as a networked object and makes it available to observers. |
| `LeftScore.Value++` | The server increases LEFT's synchronized score; clients receive the new value. |
| `ServingSide.Value = 1 - scorerSide` | The player who conceded gets the next serve. |
| `[ObserversRpc] AnnounceGoal(...)` | The server sends a one-time goal message to the clients observing that paddle. |
| `[TargetRpc] YouServe(target)` | The server sends a one-time serve message only to the chosen connection. |
| `if (Instance.Feed.Count > 6) Instance.Feed.RemoveAt(0);` | The server removes the oldest entry so the shared history stays at six lines. |
| `OnDestroy` unsubscriptions | The object stops listening to events when it is destroyed, so old callbacks do not remain attached. |

## Section 13 - Vocabulary practice

| Term | My explanation |
| --- | --- |
| Topology | How the machines connect and which machine is trusted to decide the game state. |
| Dedicated server | A separate server process that runs the match without being a player. |
| Listen server | A player's process runs both the server and that player's client. |
| Peer-to-peer | Players connect as peers rather than relying on one dedicated central server. They need rules for agreeing on the result. |
| Client | The player's game instance. It reads local input and displays the shared world. |
| Host | A process that is both server and client at the same time. |
| Connection | A network link between a client and the server. |
| ClientId | The identifier FishNet gives a client connection. A reconnect can receive a new one. |
| Tick | One step of the networking clock. |
| Tick rate | How many networking ticks happen each second. This project's saved setting is 30. |
| Packet | A group of bytes sent over the network. |
| UDP vs. TCP | UDP sends datagrams without guaranteeing delivery or order. TCP provides an ordered reliable byte stream, which can delay later data while missing data is recovered. |
| Reliable / unreliable | Reliable delivery makes the library recover important messages. Unreliable delivery can skip old updates that newer ones will replace. |
| Round-trip time (RTT) | How long it takes for a message to go to the other machine and for a reply to come back. |
| Ping | A request/reply check used to estimate RTT; the displayed value is commonly called ping too. |
| Bandwidth | The amount of data transferred per second, such as the HUD's B/s reading. |
| NetworkManager | The main FishNet component that connects the server, client, timing and other managers. |
| Transport | The layer that sends and receives the bytes. This game uses Tugboat. |
| NetworkObject | The component that gives an object a network identity, ownership and spawn state. |
| ObjectId | The identifier of a spawned network object, so the machines can refer to the same object. |
| NetworkBehaviour | A script base class that provides FishNet callbacks, RPCs, SyncTypes and network-role checks. |
| Scene object | An object saved in the scene, such as this game's GameState or Court. |
| Prefab | A saved object template. The server uses the Paddle and Ball prefabs to create runtime objects. |
| Spawn | Register an object with FishNet on the server so its observers can receive a networked copy. |
| PlayerSpawner | The component that creates an owned player prefab when a client finishes loading its starting scenes. |
| Owner | The connection assigned ownership of a network object. |
| Ownership | The relationship that lets a client identify its own object and use owner-only operations. It does not automatically make every field client-writable. |
| Observer | A client allowed to receive a particular network object and its updates. |
| Replication | Sending shared state so other machines can maintain copies of the same world. |
| State vs. event | State describes what is true now, such as the score. An event describes something that happened, such as a goal announcement. |
| SyncVar | A synchronized value whose current state is also sent to late joiners. The server writes our gameplay SyncVars. |
| SyncList | A synchronized list. Our feed uses one so reconnecting clients receive the recent entries. |
| OnChange | A callback used to react when a synchronized value changes, such as updating a paddle's colour and side. |
| NetworkTransform | A component that replicates position and can also replicate rotation, scale and parent changes, depending on its settings. |
| Client-authoritative | The owning client supplies the position that the server relays. We use this for paddles this week, so fake movement is still a possible cheat. |
| Interpolation | Displaying movement between received states to make remote objects move more smoothly, at the cost of some delay. |
| ServerRpc | A client-to-server method call. In this game it carries a request to serve. |
| ObserversRpc | A server-to-observers method call. Our goal announcement uses it. |
| TargetRpc | A server-to-one-client method call. Our YOU SERVE message uses it. |
| RequireOwnership | An RPC setting that controls whether the caller must own the object; our serve RPC keeps the normal ownership requirement. |
| Serialization | Turning values into bytes for transmission, then reading the bytes back into values on the receiving machine. |
| Interest management | Choosing which clients should receive which objects, so clients do not need updates about everything. |

## Submission checks

- [ ] Complete the 10/30/60 tick-rate table during real rallies.
- [ ] If an original prediction record exists, add it and grade it honestly; otherwise keep the retrospective label.
- [ ] Restore tick rate 30 in every project copy used for the demo.
- [ ] Confirm the demo date and submit before 12:00 on that day, as required by the homework.
- [ ] Review both READMEs and practise explaining the code without reading these answers word for word.
- [ ] After review, arrange the files in the required `dgd0005-can-mutlusahinoglu` repository and push the actual available history. An initial upload now cannot prove commits happened after each earlier step, or that the design sheet was committed before Step 7.
- [ ] Share the real design-sheet commit link with the instructor when available. No link has been sent from this package.

The package includes the Unity source and both README files. It excludes Library, Temp, Logs, Builds and UserSettings.
