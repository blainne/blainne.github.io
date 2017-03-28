---
layout: post
title: "Experience of Akka.NET, part 2"
tags: [Akka.NET]
description: ""
keywords: akka.net
published: true
comments: true
excerpt_separator: <!--more-->
---
In the [previous part](https://blainne.github.io/2017/03/21/akka-impressions/), I presented my conclusions of unit testing Akka.NET. The next big thing when starting to work with actor model based approach is how it's omnipresent asynchronous communication requires different ways of designing and reasoning about the code.

*Once again I want to remind that I'm writing from the perspective of a person just starting the journey into Akka.NET and actor model. My "findings" should only be perceived as something, that other people wanting to try Akka.NET might face. All the concerns described here may be only part of my incomplete knowledge.*
<!--more-->

### Asynchronous everything
To be true, Akka.NET provides a way to use familiar request-response messaging model via the `Ask` method, but in general, it's [not the way to go](http://bartoszsypytkowski.com/dont-ask-tell-2/). I decided to avoid it, even though it appeared to be the ~~simplest~~ easiest way to use Akka inside ASP.Net Web API framework. I somehow got myself into thinking that the request-response message is viral as it forces all the actors we `Ask` to also synchronously communicate with all the other they need some data from. I'm however not 100% sure if there's a way to break this viral chain or not.

So, I'm with the asynchronous model - the `Tell` method in Akka's vocabulary. Almost all of my actors are state machines. The [guarantees of message delivery order](http://getakka.net/docs/concepts/message-delivery-reliability) to an actor aren't particularly strong. It may happen even in a local only actor system (and it does, I've seen it on the running system) that messages sent from two different actors to a third one will be delivered out of the order of sending.

This means that before an actor will send some message back to us (an asynchronous response) many other messages might be processed by it in the meantime, effectively modifying its state and thus influencing our anticipated response. Well, the same may happen in the threading model when some threads modify the internal state of an object. Isn't actor model advertised to solve this kind of problems?

Well, the actor model promise is that an actor processes one message time. It ensures that the internal state is always consistent, in a low-level meaning. There are no race conditions - again, in a low-level meaning. But on the high level, the level of our application's logic it's programmer's responsibility to deal with these issues. 

It was also a big catch for me, a programmer coming with a synchronous, OO influenced mindset, that even though an actor works through its messages serially it's easy to get into trouble. The main misunderstanding was caused by not being able to break with deeply rooted thinking that a request - response pair of messages is like a method call in OO world and at the same time confusing it with this "serial" processing stuff understood in a wrong way.

The conclusion is that the programmer must always assume (unless he can prove it won't happen) that any kind of message can come to an actor at any time. And it's his responsibility to handle that. In my case simply `Stash`ing message for later (when the actor will switch to a different state) was enough in most cases.

### Designing actors
Speaking about the design, I also have a few other observations. The first one is that Akka.NET works great with the top down approach. When implementing an actor (to be more specific, a `ReceiveActor`) I would first write a method representing its main or initial state. This boils down to define which types of messages can an actor receive, and describe with a few words how it shall react to it.

This description naturally makes a name of the message handling method:
<a name="sample1">
~~~~ csharp
    public WaiterActor()
    {
        Become(Ready);   
    }

    private void Ready()
    {
        Receive<CheckPleaseMsg>(msg => 
                ConfirmAndTellCashierActorToPrepareTheBill(msg));

        Receive<CheckReadyMsg>(msg => 
                AddYourNoteAndSendToTableActor(msg));

        Receive<MealOrderBeginningMsg>(msg => BecomeNoting(msg));
        
        Receive<MealOrderCompleteMsg>(msg => 
                BecomeReadyAndPassOrderToChefActor());
    } 
~~~~

</a>
*Note that I should be able to use method group syntax above, but for some reason, the compiler told me it cannot decide on which method to use, even though there was only a single one.*

I usually don't make method names that verbose and descriptive. In my opinion, it obfuscates the code instead of improving the readability, but in this case, I find it to be a good approach. I would place this set of methods at the very top of my class so that any reader of this code would find it right away and hopefully would not need reading implementation details.
These descriptive names also imply the existence of some other actors and states (the "Become..." fragments) within this actor. So I would create stubs for those next.

It is worth noting, that these - most important from the design perspective methods - are (and I believe they should be) private. This observation will be useful when I'll get to discuss language support.

By nature, when designing, we use some well known patterns. Akka.NET has already some well-known idiomatic patterns, like the Circuit Breaker, Router (which is so often used that it's provided as a special kind of actors in Akka), or the PubSub. Yes, I know those are concepts that exist outside of actor model as well, but they have some nice well-established way of implementing in Akka.NET - so I call them patterns.
I want to focus on one of such patterns which solves the problem of [long-running actions](https://petabridge.com/blog/top-7-akkadotnet-stumbling-blocks/#4-executing-long-running-actions-inside-an-actors-receive-method). 
The thing is, this one involves writing a lot of boilerplate code. I've read that Akka.NET now supports `async` and `await` which could be used instead, but I didn't try it. There were some warnings related to this which method I simply didn't get right away and had not much time to experiment with. 
I was using this pattern when dealing with input/output operations such as downloading some data from an external source or writing stuff to the database. It was the database when I realized that the code of my db actor became too long and too complex - mainly because of this boilerplate. Thankfully, the Single Responsibility Principle not only relates to object oriented programming - I think it applies to programming in general. Complex and long code of an actor is clearly a violation of this principle. So I ended up splitting my database actor into two - one that does domain- to db- model translation - which is a long-running action and also uses the pattern, and the second one that does just the database operations. After that, it was way better. 
To be honest, still it wasn't perfect - I came to the conclusion that there could be a new specialized actor for just executing this pattern. It should, of course, be maximally generic to not have to reimplement it every time a different task is to be executed. I haven't yet implemented it, though. 

### Actors are weakly typed.
To be fair - personally, I'm all on the strong typing side. And I don't mean strong typing like C# or Java do. More like Haskell of F# do. Hence I feel somehow crippled when I have to work with Javascript (just a personal opinion). It's funny since weak typing may be seen as more freedom rather than a limitation.

Akka.NET uses POCOs as it's messages. Sure it typecheks them. I used some message model hierarchy in my application which involved interfaces as well so I could match some kinds of incoming messages in a nice readable way inside my `ReceiveActor` implementations:

~~~~ csharp
    Receive<ITaskFailure>(msg => failureHandlerActor.Tell(msg));
~~~~

All this is great but what I would expect from an actor model implementation, is for the compiler to be able to typecheck whether I send some types of messages to an actor which does not handle them. 
The issue is, the basic interface that is used to work with actors is `IActorRef` which, does not provide a way to tell which messages the recipient can process. If an actor receives some message that it doesn't know how to handle it'll put it in a special place. In my design, seeing a message there means a bug. I believe the ability for the compiler to prevent this would be of great value.

I believe this can be implemented in some way, which would probably mean creating some types over the standard Akka ones like `IActorRef`. In reality, it would also mean simply creating a higher level framework working on top of Akka. Doable - I think, but not done yet. Right now it's simply something a person wanting to use Akka.NET needs to be aware of.

### Language support
The original Akka framework is mostly used in Scala language which is a hybrid (both OO and functional) language that compiles to JVM.
The .NET version of it can be used in C#, F# and probably VB.NET as well. I've heard good thing about the F# API of it but didn't even try that. The C# version uses many features of the language in some smart ways but all the time I was under the impression that the language actually doesn't fit the API exposed by Akka.

Remember the [example above](#sample1), which I announced will be discussed here. It's a part of an actor that really deserves to be emphasized by the language. But in C# it's just a private method. It shouldn't be public because we don't want anyone outside to call it. But it would be great if external users of our actor could somehow know what messages are being handled in a particular state. Not talking about unrevealing the internal implementation details here, more of giving some meta-information of what the actor does. In the end, that are the messages that form an actor's true interface.

The verbose C# syntax also doesn't work well with the message passing semantics. I can  think about some well-thought set of built-in operators for sending messages in a couple of ways. 
Even more cumbersome is actor creation. It deserves good language construct, as the currently used combination of this thing called `Props` with constructors is barely readable and I saw in certain articles how much confusion it often makes.

The messages themselves are a concept that is not well supported by C#. A message has to be immutable. The language, however, doesn't have immutability support built-in. So nothing stops us from trying to exploit POCOs to do some hacky magic stuff mutating the messages as they get passed along.

I could probably find more examples to prove my statement, but all this boils down to the conclusion, that the actor model is something that would benefit from its own language constructs. C# is an OO first language, and while Akka.NET developers did a great job with using it, it simply doesn't fit perfectly right now.

I never tried to use an actor-model alternative that comes from Microsoft - The Orleans framework. I know they took some other approach and integrated it way more with the standard object model supported by C#. I wonder how that one works in terms of language fit.

### The final conclusion
After reading all this You may get an impression that I didn't like working with Akka. It is far from the truth. I really liked it and I plan to use it again whenever I'll be in the need of doing something that needs to employ parallelism or distributed execution (or both). When I started developing my piece of software I did not intend to use Akka at all. At one point, during planning how would I implement my requirements I realized that doing this with threads and tasks I would end up creating my own poor implementation of actor model approach. That was when I decided to use an existing one. 
And now it works great, I don't even want to think how much harder would it be to achieve the same results trying to do it the old way!

This somehow reminds me of this [famous quote from Robert Virding](http://rvirding.blogspot.com/2008/01/virdings-first-rule-of-programming.html):

> "Any sufficiently complicated concurrent program in another language contains an ad hoc informally-specified bug-ridden slow implementation of half of Erlang."


Thank You for reading! That's all.
