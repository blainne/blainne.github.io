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

To give some better example, we want a function that can take a collection like this

~~~~ fsharp
[
    {Name = "Normandy"; AppearsIn = "Mass Effect"}
    {Name = "Enterprise"; AppearsIn = "Star Trek"}
    {Name = "Atlantis"; AppearsIn = "Real life :-)"}
    {Name = "Rocinante"; AppearsIn = "The expanse"}
    {Name = "Serenity"; AppearsIn = "Firefly"} 
]
~~~~

and return two collections:
~~~~ fsharp
["Normandy"; "Rocinante"; "Enterprise"; "Atlantis"; "Serenity"]

["Mass Effect"; "The expanse"; "Star Trek"; "Real life :-)"; "Firefly"]
~~~~

In the above case the selectors I mentions would be `spaceship -> spaceship.Name` and `spaceship -> spaceship.AppearsIn`.

### The purpose
I have to admit, this kind of function seems to have very limited usage. It happened only once that I needed something like this - but when I started to implement it, I just found it to be a good function to exercise with.

Almost always it seems to be a better idea to transform our input list using the standard `map` function and at the last point, after applying all the transformations, do the unzipping. And this last step only if we really need to have two separate collections - which is rarely the case (at least, in my experience).

So, why to write a blog post about it? As mentioned, I found it to be a great code & design exercise. I spent quite a lot of time playing with this, some things forced me to learn new things, so I'm just sharing.

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

This function is so simple, it could even be written as a one-liner. We're a bit verbose, since this is for learning purposes. More: we don't really need the types to be explicit. F#'s type inference works great here, so from now on, I'll skip the type annotations.

This first naive version of unzip has a huge drawback: it iterates over the sequence twice. Let's try to fix it.

### Approach two - the basic fold
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

### Approach three - lets try lists
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

Note, the `tMap` function which is just a quick helper that can map a tuple of two elements using two functions accordingly. We could use a lambda instead of a named function, but I believe it improves readability to have it like it is right now.

The `unzip3` function, while works fine, has this final list reversing, which just looks bad. Of course, here, with `fold`, it's necessary since we're prepending (using cons operator: `::`) our accumulator with new element in every step. Remember, in previous example we were appending, but trying to do it with native F# lists would be a bad idea in terms of performance.

There is however a very simple way to get rid of this final reversing step.

### Approach four - foldBack to the rescue
We can simply use `foldBack` instead of `fold`. It effectively iterates the list starting from the end, which will nicely work with our prepending operation. There is no more need to reverse the output collections as they're being built in the right order all the time here.

~~~~ fsharp
let unzip4 select1 select2 items =

    let folder item (lst1, lst2)= 
        (select1 item)::lst1, (select2 item)::lst2
        
    List.foldBack folder items ([], []) 
~~~~

Now this is pretty damn simple piece of code, isn't it?

### Bonus - the trees
Sequences, lists, etc are collections that organize the data in some linear order. There are however some other possibilities to do it. One of those, that is common in programming are trees. 

Turns out we can have our unzip function working also with trees. First, let's define an example tree structure we'll work with.

~~~~ fsharp
type Tree<'a> =
    | Leaf of 'a
    | Node of 'a * Tree<'a> list 
~~~~

Now we'll also going to need something that will work similar to our `foldback` function in case of lists. 

As to why we need something like `foldback` and not just `fold` I suggest to read [short](https://sidburn.github.io/blog/2016/05/28/catamorphisms) or [long](http://fsharpforfunandprofit.com/series/recursive-types-and-folds.html) article (or both). Enough to say here, that we'll be building trees during our function just like we've been building lists. And our immutable `Tree` type needs to be built bottom up.

In fact, we'll use a much simpler folding function, `cata` (again I refer to the articles if this is something new). For our purpose it does exactly the same, `foldback` would do.

~~~~ fsharp
let rec cata fLeaf fNode item=
    let recur = cata fLeaf fNode
    match item with
    | Leaf(a) -> fLeaf(a)
    | Node(a, subTrees) -> fNode(a, subTrees |> List.map recur )  
~~~~

Once we have that, our `unzip` for `Tree` type can be implemented like below:

~~~~ fsharp
let unzipTree<'a, 'r1, 'r2>
        (select1:'a->'r1)
        (select2:'a->'r2)             
        (tree: Tree<'a>)
    : Tree<'r1> * Tree<'r2> =

    let unzipLeaf a= 
        (Leaf(select1 a), Leaf(select2 a))
    
    let unzipNode (a,subtrees) =
        (Node(select1 a, subtrees |> List.map fst),
         Node(select2 a, subtrees |> List.map snd))

    tree |> cata unzipLeaf unzipNode
~~~~

If we compare it to our previous code example, the code looks very similar 