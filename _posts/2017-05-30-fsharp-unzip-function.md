---
layout: post
title: "Splitting data structures through each element (F#)"
tags: [F#, unzip]
description: ""
keywords: fsharp, unzip
published: false
comments: true
excerpt_separator: <!--more-->
---
F# standard library for sequences (the "enumerables" in F#'s nomenclature) has this very nice and simple [`zip`](https://msdn.microsoft.com/visualfsharpdocs/conceptual/seq.zip%5b%27t1%2c%27t2%5d-function-%5bfsharp%5d) function that combines two collections, by simply creating collection of pairs of items.
What I'd like discuss today is a function that does the opposite: deconstructing an collection into two based on some arbitrary selectors.
<!--more-->

To give some better example, we want a function that can take a list like this

~~~~ fsharp
[
    {Name = "Normandy"; AppearsIn = "Mass Effect"}
    {Name = "Enterprise"; AppearsIn = "Star Trek"}
    {Name = "Atlantis"; AppearsIn = "Real life :-)"}
    {Name = "Rocinante"; AppearsIn = "The expanse"}
    {Name = "Serenity"; AppearsIn = "Firefly"} 
]
~~~~

and return two lists:
~~~~ fsharp
["Normandy"; "Rocinante"; "Enterprise"; "Atlantis"; "Serenity"]

["Mass Effect"; "The expanse"; "Star Trek"; "Real life :-)"; "Firefly"]
~~~~

In the above case the selectors I mentions would be `spaceship -> spaceship.Name` and `spaceship -> spaceship.AppearsIn`.

### The purpose
I have to admit, this kind of function seems to have very limited usage. It happened only once that I needed something like this - but when I started to implement it, I just found it to be a good function to exercise with.

Almost always it seems to be a better idea to transform our input list using the standard `map` function and only at the last point, after applying all the transformations to do the unzipping. And this last step only if we really need to have two separate collections - which is rarely the case (at least, in my experience).

So, why to write about it? As mentioned, I found it to be a great exercise. Moreover it's a good study of design considerations.

### The type
The explicitly typed header of our function looks like this:

~~~~ fsharp
let unzip<'a, 'r1, 'r2>
        (selector1:'a->'r1)
        (selector2:'a->'r2) 
        (items:'a seq)
    :'r1 seq * 'r2 seq
~~~~

Please note the return value which is `'r1 seq * 'r2 seq`. We said we want our function to return two collections. Clearly, a function can only return one thing, so that's why the output of the function is a tuple of two collections.

In some later versions we will start using F#'s native list type instead of sequence, just to gain some better readability by saving few lines of converting the types.


