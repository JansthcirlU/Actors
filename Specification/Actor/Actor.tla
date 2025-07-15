---- MODULE Actor ----
EXTENDS TLC, Naturals

VARIABLES supportedMessageTypes

ActorType == [id: Nat, state: States, supportedMessages: Seq(supportedMessageTypes), messages: <<>>]

====