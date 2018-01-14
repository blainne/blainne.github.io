---
layout: post
title: "Maps as functions (F#)"
tags: [F#, functions, maps]
description: ""
keywords: fsharp, functions, function composition
published: false
comments: true
excerpt_separator: <!--more-->
---
We all know the concept of a function. To each value in the input domain it assigns exactly one output in the output domain. In programming languages as well as in math classes we mostly deal with functions expressed as formulas, but it is not the only way to describe some function. While it's usually the most convenient one, sometimes it's easier to write a function in form of a table. In context of programming we may think about this table as a `Map` or `Dictionary` type. In this post we'll try to explore this idea and look how far we can get when expressing functions as F# maps.
<!--more-->

Let's consider simple square function. It takes a number and returns that number squared. We usually write it using the 
`f(x) = x*x` notation. This is what I call "formula notation". It tells us how to compute the output from the input.
The "table notation" would be something of this form:

|Input|Output|
|-----|------|
|1.5  |2.25  |
|2    |4     |
|3    |9     |

We see right away that it's impossible to write down such mapping for all the numbers.

This is our first serious limitation: we can only express functions where the input type has finite number of values. Although `int32` type satisfies this, it is still highly impractical to define such big tables. 
What else can we say about it? As opposed to formulas, we don't know how to compute (calculate) the output and we don't need to - it's given right away or it's missing.
This has some important implications:

 1 If we use a proper data structure for the table, we can have the result in constant time.
 2 There's nothing like generic function here.
 3 We can express partial functions.

Let's discuss these points a bit more.
First point means that if we're able to express a computationally expensive function to it's table representation we're trading CPU time for memory usage. Actually, we even have a well known technique which bases on this tradeoff: memoization. Memoized function is basically building a table representation of itself in memory. The second time You call a memoized function with an argument You used before it'll, go to that table and give You the results in instant time. 

The second point we can think of like this: to define a table function we need to write down all the possible inputs and outputs. But if we do this we're already fixed on input and output types. If we'd like the function to support some other type as well we'd have to define second version of this function.

The third is a bit more tricky. A partial function is one which doesn't handle all possible input values of a type. Generally it's something wrong but even [haskell has some] (https://wiki.haskell.org/Partial_functions). On the other hand it seems that they're more about strong typing than anything else. Think about that. If a function, let's say integer division, doesn't handle all the possible input values it means that the set of integers is not the true input domain of this function. The integers without zero are. Since it would be inconvenient to create a new type `IntegersWithoutZero` and converting the value when needed every time we want to divide a number, programming languages usually push that duty to programmer to check for zeroes when dividing.
When expressing functions as tables, it's much more likely to skip the input values we're sure we'll never use - since otherwise we'd have to explicitly write them down. If You keep a table function for some computationally expensive fancy cryptography stuff We'll never cover the entire input domain in the table - We'll write some code to check for that. 

Well, enough discussion let's go to some code. We need some types to work on: 

~~~~ ocaml
type CelestialBody =
    | Earth | Mars | Europa 

type Inhabitants =
    | Humans | Robots | Aliens
~~~~

Our first function will be to get inhabitants of given celestial body. The standard F# way would probably be something like this:

~~~~ ocaml
// CelestialBody -> Set<Inhabitants>
let GetInhabitants =
    function
    | Earth -> Set[Humans;Robots]
    | Mars -> Set[Robots]
    | Europa -> Set[Aliens]
~~~~

That produces a normal formula expressed function. But we can keep this mapping in... a `Map`:
~~~~ ocaml
// Map<CelestialBody, Set<Inhabitants>>
let ``Get inhabitants`` =
    Map[
        Earth, Set[Humans;Robots]
        Mars, Set[Robots]
        Europa, Set[Aliens]
    ]
~~~~ 

We also need some small facility to be able to "run" our map. 
~~~~ ocaml
// Map<'a,'b> -> 'a -> 'b
let run fm arg = Map.find arg fm
~~~~
