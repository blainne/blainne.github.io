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

Almost always it seems to be a better idea to transform our input list using the standard `map` function and at the last point, after applying all the transformations, do the unzipping. And this last step only if we really need to have two separate collections - which is rarely the case (at least, in my experience).

So, why to write a blog post about it? As mentioned, I found it to be a great exercise. Moreover it's a good study of design considerations.

### The type
The explicitly typed header of our function looks like this:

~~~~ fsharp
let unzip<'a, 'r1, 'r2>
        (selector1:'a->'r1)
        (selector2:'a->'r2) 
        (items:'a seq)
    :'r1 seq * 'r2 seq
~~~~

Please note the return value which is `'r1 seq * 'r2 seq`. We said we want our function to return two collections. Clearly, a function can only return one thing, so that's why the output is a tuple of two collections.

In some of the further examples we will start using F#'s native list type instead of sequence, mainly to use some of it's unique properties. In general this doesn't change too much in the concept itself.

### Approach one - an easy one.
The idea is like this: when we transform (project) a sequence of things into a sequence of another things we usually use the `map` function. When we want to transform into two sequences why not to use `map` twice?

~~~~ fsharp
let unzip<'a, 'r1, 'r2>
        (selector1:'a->'r1)
        (selector2:'a->'r2) 
        (items:'a seq)
    :'r1 seq * 'r2 seq =
    
    let s1 = items |> Seq.map selector1
    let s2 = items |> Seq.map selector2

    (s1, s2)
~~~~

This function is so simple, it could even be written as a one-liner. We just want to be a bit verbose, since this is for learning purposes. More: we don't really need the types to be explicit. F#'s type inference works great here, so from now on, I'll skip the type annotations.

This first naive version of unzip has a huge drawback: it iterates over the sequence twice. Let's try to fix it.

### Approach 2 - the basic fold
Folds are very powerful functions. You can easily implement a `map` just using some member of the family of folds. Let's just try to use one of the most basic ones which is just `Seq.fold`. 

~~~~ fsharp
let unzip2 select1 select2 items =

    let add item seq  = 
        seq |> Seq.append (Seq.init 1 ( fun _ -> item ))

    let folder (seq1, seq2) item = 
        (seq1 |> add (select1 item), 
         seq2 |> add (select2 item))
        
    items
    |> Seq.fold folder (Seq.empty, Seq.empty) 
~~~~

Here we start with an initial accumulator being just two empty sequences. In each recursive step fold will invoke the `folder` function which creates a new accumulator value being the result of appending results of both selectors to previous accumulator value. Nothing particularly fancy, just building two collections as we're traversing the input.

This code doesn't have the drawback of our first approach, so it doesn't go twice through the input. 

### Approach 3 - lets try lists
Lists in F# are one of the most common collection types. Definitely, our unzipping function should also be available for them. Let's try to create something similar to `unzip2` that would work with F# lists:

~~~~ fsharp
let tMap (fn1,fn2) (item1,item2) = (fn1 item1, fn2 item2)

let unzip3 select1 select2 items =

    let folder (lst1, lst2) item = 
        (select1 item)::lst1, (select2 item)::lst2
        
    items
    |> Seq.fold folder ([], []) 
    |> tMap (List.rev, List.rev) 
~~~~

