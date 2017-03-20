---
layout: post
title: "Experience of Akka.NET, part 1"
tags: [Akka.NET]
description: ""
keywords: akka.net
published: true
---

Recently I finished a major development task, which makes big use of the [Akka.NET]. Since this was the first time when I used it for a serious software, I ended up with many conclusions that - I believe - might help someone who's considering going this way.

Don't get me wrong - I'm certainly not an expert in this area. What I want to discuss is what I struggled with and what was great. And all this from the perspective of Akka's fresh apprentice. I decided to write this because I think there is a lack of this kind of analysis related to Akka.

I don't want to give long explanations on what exactly Akka.NET is since You can easily find good sources, starting with their website. To just give few words: it is a toolkit and runtime that allows employing the [actor model](http://www.brianstorti.com/the-actor-model/) approach in .NET software. Akka's is advertised as a way to do concurrent and distributed applications, which has something to do with the actor model stuff but also with a system building philosophy adopted from Erlang. 

This series of articles assumes You already know what the actor model is and that You have some initial knowledge on Akka.NET's usage. In specific, it would be good to know about ReceiveActors and the concept of "becoming".

###Akka.NET is hard to unit test
This probably was one of the first very characteristic things I noticed. While the authors provide some tooling and libraries to simplify it, it is lots of boilerplate code and the shape of the tests is something that in a normal object-oriented world would be a red light for me.
What's the case? Most of the non-trivial actors I implemented were, in fact, little state machines (`Become()` was being used often to switch between states). If I want to write a test that assumes actor under test (AUT) is already in a given state I need to somehow I need to initialize it somehow. See, the state of an actor is not the only current set of message handling methods ("how it will react to messages in this state"), but also any internal ...state (like values of any fields, etc.).

Normally, for testing purpose, I'd inject such things via setters/constructor injection. But with actors, adding setters is simply against the model. Actors should communicate only via messages. Sure, one can justify that setters are only for testing and they'll never be used in production code, but You know - it's a promise one cannot fulfill unless being the only developer through the entire lifespan of the code. Once You expose a setter or even a getter, everyone can use it if he has appropriately typed reference to it. And I'm not even sure if it's concurrency-safe to use a getter of an actor inside the code of another actor - again because of the model.

Injecting internal state via constructor while saner is also more difficult because of the concept of actor Props which are Akka's way to spawn new actors. Just take a look:

{% highlight csharp %}
var actor = Sys.ActorOf(Props.Create(() => new MyActorClass()));
{% endhighlight %}

In my tests, I took another approach that looks to be more actor-model compliant. Getting actors to intended state by sending them a bunch of messages that will make them transition there from their initial state. Like this:

{% highlight csharp %}
//given
...

//when
testedActor.Tell(someMsg1);
testedActor.Tell(someMsg2);
testedActor.Tell(someMsg3);

// this message should actually trigger the tested behavior
testedActor.Tell(finalMsg); 

//then
...
{% endhighlight %}

My thinking is that this is an antipattern in the world of unit tests. It assumes that all the reactions for messages 1 to 3 are already working correctly. For me, it's not a unit test at all. In fact, it tests a longer scenario.
Yet, I think this kind of tests is quite valuable in this model, so in the end, I chose this way.

But that's not all about testing. There's more. There's asynchrony. 
Testing an actor means sending a message to it and expecting some message in response. If we designed our AUT to use asynchronous (preferred) communication via `Tell()` method, our test doesn't block on this call. So we need to give the actor some time to do it's processing and react in the way we expect. The standard `ExpectMsg()` family of assertion methods provided by Akka's authors all have so timeouts after which they fail. The `ExpectNoMsg()` has to wait for it's entire "timeout". This adds another dimension of complexity to our tests and once again violates the principles of a good unit test. 

But as I mentioned, I simply stopped considering them to be "unit" tests.

*This concludes part 1 of this series. In the next parts I want to focus on topics of asynchronicity and language support for actor model.*


