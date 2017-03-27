---
layout: post
title: "Experience of Akka.NET, part 2"
tags: [Akka.NET]
description: ""
keywords: akka.net
published: false
comments: true
excerpt_separator: <!--more-->
---
In the [previous part](https://blainne.github.io/2017/03/21/akka-impressions/) I presented my conclusions of unit testing Akka.NET. The next big thing when starting to work with actor model based approach is how it's omnipresent asynchronous communication requires different ways of designing and resoning about the code.
<!--more-->

### Asynchronous everything
To be true, Akka.NET provides a way to use familiar request-response messaging model via the `Ask` method, but in general it's [not the way to go](http://bartoszsypytkowski.com/dont-ask-tell-2/). I decided to avoid it, even though it appeared to be the ~~simplest~~ easiest approach to use Akka inside AS.PNet Web API framework. I somehow got myself into thinking that the request-response message is viral as it forces all the actors we `Ask` to also synchronously communicate with all other actors they need some data from. I'm however not 100% sure if there's a way to break this or not.

So, I'm with the asynchronous model - the `Tell` method in Akka's vocabulary. Almost all of my actors are state machines. Some of them use the states to serialize IO operations like updating a DB table or downloading a big chunk of data from an external web resource. The others simply model "lifecycle" states of some domain entities they represent (e.g. a )



