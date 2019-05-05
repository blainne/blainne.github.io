---
layout: post
title: "Building computations in C#"
tags: [C#, functions, combinators]
description: ""
keywords: csharp, functions, function composition, lazy evaluation
published: false
comments: true
excerpt_separator: <!--more-->
---
I was writing lots of code dealing with a SQL database recently. Nothing really fancy, just ordinary business logic which sometimes needs to retrieve something, then persist something else based on what it read. The code ended up being divided into nice separate modules, each having its own responsibility.
The only issue I had with it was how all the places were polluted with instances of SqlConnections and SqlTransactions and the standard appeareance of `using` statements following them. "Does this really have to look like that?" - I asked myself. As You might have guessed already, the answer is "no". 
<!--more-->
The modularization turned out to be somehow responsible for this mess. Each module had it's share of database code which it used to do it's part of business logic separately. 


*All the code samples from this text can be found [here](https://github.com/blainne/blainne.github.io/blob/master/code_samples/building_computations.cs).*



*If this post resonates with You in any way, please leave a comment. I'd love to have some feedback.*