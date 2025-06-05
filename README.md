# Exploring the actor model in .NET

🚧 🚨 **Work in progress!** 🚨 🚧

## Software as communicating actors

The [Actor Model](https://en.wikipedia.org/wiki/Actor_model) has a long and rich history, both in academia and in the software industry.
The basic idea is to allow concurrent computation by designing the software components as *actors*.
These actors can communicate with each other through *messages* and react to them.
Each actor has a private state and handles messages in a synchronous, single-threaded manner.
The concurrent nature of the system emerges when you allow actors to communicate with each other simultaneously.
In this repo, I have implemented a basic actor type to demonstrate how to do distributed breadth-first search through actor communication alone.

## Designing a thread-safe actor

### The basic components

I did not know much about concurrent programming when I started this project,
so I decided that a good first step would be to write some interfaces to describe what an actor would look like and how you could use them.

I landed on a simple type hierarchy for:

- an actor with an ID to which you can send messages
- an actor ID type which can be compared and equated to other IDs of the same type
- a message type with a sender ID which specifies who can receive it

Designing types is fun, but when I finally got out of the analysis paralysis phase, it was time to actually implement the actor.

### Implementing the actor

For my implementation of the actor, I was inspired by Erlang, one of the earliest implementations of the actor model in a non-academic, industry-oriented programming language.

Here are the basic building blocks I used to implement my actor:

- a method to send messages to the actor
- a mailbox to signal received messages
- a way to handle messages synchronously
- an internal event loop to listen to new messages and handle them
- the safe disposal of an actor

For the mailbox, I decided to go with the built-in `Channel<T>`, which was designed exactly for robust, in-process data passing, ideal for messaging.
It is worth taking a look at the [Channels documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) on Microsoft Learn to follow the implementation.
The channel is an `IAsyncEnumerable<T>`, which means that you can use `await foreach` to iterate over it.
Whenever a new message is received, the loop advances to yield the new message so that it can be handled with a method call inside the loop.
To kickstart the event loop, i.e. to start listening for new messages, I needed a `Task` that should not be awaited until disposal, in a fire-and-forget way. Moreover, I also needed an internal `CancellationTokenSource` which I could call to interrupt the event loop.

Sending the message introduced the first pains of concurrent design.
If my data type has to support multiple threads or processes talking to it simultaneously, it could be entirely possible that one process sends a message at the same time that another process tries to dispose the actor, or vice versa.
I managed to land on something *good enough* for my toy project, but it required significantly more research than I had expected.

## Using actors to run a concurrent shortest-path search

### Graphs and shortest paths with weighted edges

A graph is a data structure representing a network of nodes connected by edges.
These edges are directional, which means that an edge from A to B is not the same as an edge from B to A.
Edges also have weights, which could represent anything from a distance between two locations or the latency between two servers.

Shortest path algorithms are graph search algorithms.
Such algorithms assign the shortest distance from a given starting node to every other node in the graph.
For graphs with weighted edges, the shortest distance is given by the path made of consecutive edges such that the sum of all edge weights is smaller than or equal to that of any other path.

### Using actors to communicate search distance

I wanted to know if I could implement a concurrent algorithm to detect the shortest path (least total weight) using actors.
To do this, I first create a communication network based on the original graph.
I spawn an actor for each node with a default distance `null` (to represent an unreachable node).
Then I link each actor to its neighbours to allow communication between them.

When the start node actor receives a message to kick off the shortest-path search, it first sets its own distance to zero and then tells its neighbours that they are at distance zero from the starting point.
Each neighbour can then update their own distance from the start value based on the distance received from their predecessor and signal their own neighbours, and so on.
If their current distance from the starting node is shorter or equal to what would be the new distance from another predecessor, they ignore the message and do not propagate the distance updating chain.

Eventually, all actors will settle on their own shortest distance and they will stop sending messages, at which point the algorithm terminates.

### Benchmarking performance

There is a significant overhead when creating the actor communication network based on the original graph.
On the language level, there is also an overhead when doing asynchronous message passing.
The main application will include benchmarks for a variety of scenarios to see how the concurrent algorithm compares against the synchronous one.