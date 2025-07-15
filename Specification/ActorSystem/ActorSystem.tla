---- MODULE ActorSystem ----
EXTENDS Sequences, Naturals

CONSTANTS Messages  \* Set of possible message contents

VARIABLES actor1, actor2

vars == <<actor1, actor2>>

\* Actor states
States == {"idle", "busy", "disposed"}

\* Actor record structure
ActorType == [id: Nat, state: States, messages: Seq(Messages)]

\* Type invariant
TypeOK == /\ actor1 \in ActorType
          /\ actor2 \in ActorType
          /\ actor1.id # actor2.id  \* Actors have different IDs

\* Initial state
Init == /\ actor1 = [id |-> 1, state |-> "idle", messages |-> <<>>]
        /\ actor2 = [id |-> 2, state |-> "idle", messages |-> <<>>]

\* Send a message from one actor to another
Send(from, to, msg) == 
    /\ from.state # "disposed"     \* disposed actors can't send
    /\ to.state # "disposed"       \* disposed actors can't receive  
    /\ to' = [to EXCEPT !.messages = Append(@, msg)]
    /\ UNCHANGED from

\* Actor starts processing a message
StartProcessing(actor) ==
    /\ actor.state = "idle"
    /\ actor.messages # <<>>       \* has messages to process
    /\ actor' = [actor EXCEPT !.state = "busy", !.messages = Tail(@)]

\* Actor finishes processing normally  
FinishProcessing(actor) ==
    /\ actor.state = "busy"
    /\ actor' = [actor EXCEPT !.state = "idle"]

\* Cancel actor's current processing
CancelProcessing(actor) ==
    /\ actor.state = "busy" 
    /\ actor' = [actor EXCEPT !.state = "idle"]

\* Timeout forces actor back to idle
TimeoutProcessing(actor) ==
    /\ actor.state = "busy"
    /\ actor' = [actor EXCEPT !.state = "idle"]

\* Dispose an idle actor
Dispose(actor) ==
    /\ actor.state = "idle"
    /\ actor' = [actor EXCEPT !.state = "disposed", !.messages = <<>>]

\* System remains stable when all actors are disposed
SystemTerminated ==
    /\ actor1.state = "disposed"
    /\ actor2.state = "disposed"
    /\ UNCHANGED vars

\* All possible next-state actions
Next == \* Sending messages (using messages from Messages set)
        \/ \E msg \in Messages : Send(actor1, actor2, msg) /\ UNCHANGED actor1
        \/ \E msg \in Messages : Send(actor2, actor1, msg) /\ UNCHANGED actor2
        \* Processing messages
        \/ StartProcessing(actor1) /\ UNCHANGED actor2
        \/ StartProcessing(actor2) /\ UNCHANGED actor1
        \/ FinishProcessing(actor1) /\ UNCHANGED actor2  
        \/ FinishProcessing(actor2) /\ UNCHANGED actor1
        \* Cancellation and timeouts
        \/ CancelProcessing(actor1) /\ UNCHANGED actor2
        \/ CancelProcessing(actor2) /\ UNCHANGED actor1
        \/ TimeoutProcessing(actor1) /\ UNCHANGED actor2
        \/ TimeoutProcessing(actor2) /\ UNCHANGED actor1
        \* Disposal
        \/ Dispose(actor1) /\ UNCHANGED actor2
        \/ Dispose(actor2) /\ UNCHANGED actor1
        \* System termination (stuttering when all disposed)
        \/ SystemTerminated
        \* Processing messages
        \/ StartProcessing(actor1) /\ UNCHANGED actor2
        \/ StartProcessing(actor2) /\ UNCHANGED actor1
        \/ FinishProcessing(actor1) /\ UNCHANGED actor2  
        \/ FinishProcessing(actor2) /\ UNCHANGED actor1
        \* Cancellation and timeouts
        \/ CancelProcessing(actor1) /\ UNCHANGED actor2
        \/ CancelProcessing(actor2) /\ UNCHANGED actor1
        \/ TimeoutProcessing(actor1) /\ UNCHANGED actor2
        \/ TimeoutProcessing(actor2) /\ UNCHANGED actor1
        \* Disposal
        \/ Dispose(actor1) /\ UNCHANGED actor2
        \/ Dispose(actor2) /\ UNCHANGED actor1

\* Specification with fairness for timeouts
Spec == Init /\ [][Next]_vars 
             /\ WF_vars(TimeoutProcessing(actor1))
             /\ WF_vars(TimeoutProcessing(actor2))

\* State constraint to limit exploration
StateConstraint == 
    /\ Len(actor1.messages) <= 3
    /\ Len(actor2.messages) <= 3

\* Safety properties (invariants)
SafetyInvariants == 
    /\ TypeOK
    /\ actor1.id = 1 /\ actor2.id = 2  \* IDs never change
    /\ (actor1.state = "disposed" => actor1.messages = <<>>)  \* disposed actors have no messages
    /\ (actor2.state = "disposed" => actor2.messages = <<>>)

\* Liveness properties (eventually something good happens)
LivenessProperties ==
    \* If an actor receives a message and isn't disposed, it eventually processes it
    /\ (actor1.messages # <<>> /\ actor1.state # "disposed") ~> (actor1.messages = <<>>)
    /\ (actor2.messages # <<>> /\ actor2.state # "disposed") ~> (actor2.messages = <<>>)
    \* Busy actors eventually become idle (due to timeout fairness)
    /\ (actor1.state = "busy") ~> (actor1.state = "idle")
    /\ (actor2.state = "busy") ~> (actor2.state = "idle")

====