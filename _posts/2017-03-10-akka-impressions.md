---
layout: post
title: "Experience of Akka.NET"
tags: [Akka.NET]
description: ""
keywords: akka.net
published: false
---

Recently I finished a major development task, which makes big use of the [Akka.NET]. Since this was the first time when I used it for a serious software, I ended up with many conclusions that - I believe - might help someone who's considering going this way.

Don't get me wrong - I'm certainly not an expert in this area. What I want to discuss is what I struggled with and what was great. And all this from the perspective of Akka's fresh apprentice. I decided to write this because I think there is lack of this kind of analysis related to Akka.

I don't want to give long explanations on what exactly Akka.NET is, since You can easily find good sources, starting with their website. To just give few words: it is a toolkit and runtime that allows to employ [actor model] approach in .NET software. Akka's is advertised as a way to do concurrent and distributed applications which has something to do with the actor model stuff but also with a system building philosophy adopted from Erlang.

###Akka.NET is hard to unit test
This probably was one of the first very characteristic things I noticed. 

###State is great again

###Asynchrony is hard

###Language support is important

 
Akka
 - very good for top down design
     - receive actor and all state methods on top with meaningful names
     - very readable

- pattern for tasks is verbose
- weak typing
- 
    
-testing
    - hard to verify state of actor
        - state should be hidden
        - state should only be available through messages
        - exposing state through public properties would break actor idea
        - public state could be only used in tests, but such things tend to be abused
    - independently testable (decoupled by design)
-actor are bubbles of state
-asynchrony is hard (no guarantees on order of messages from different actors)
    - answer to message sent by actor in state A can be received after actor changed state to B


-logging 
   - serilog dont log actors with {@Actor} (destructuring)


[Akka.NET] getakka.net
[actor model] http://www.brianstorti.com/the-actor-model/