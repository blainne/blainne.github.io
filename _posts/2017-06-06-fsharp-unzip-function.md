---
layout: post
title: "Splitting data structures through each element (F#)"
tags: [F#, unzip]
description: ""
keywords: fsharp, unzip
published: true
comments: true
excerpt_separator: <!--more-->
---
F# standard library for sequences (the "enumerables" in F#'s nomenclature) has this very nice and simple [`zip`](https://msdn.microsoft.com/visualfsharpdocs/conceptual/seq.zip%5b%27t1%2c%27t2%5d-function-%5bfsharp%5d) function that combines two collections, by simply creating a collection of pairs of items.
What I'd like to discuss today is a function that does the opposite: deconstructing a collection into two based on some arbitrary selectors.
<!--more-->

*All the code samples from this text and even more can be found [here](https://github.com/blainne/blainne.github.io/blob/master/code_samples/fsharp-unzip.fs).*

To give some better example, we want a function that can take a collection like this

~~~~ ocaml
[
    {Name = "Normandy"; AppearsIn = "Mass Effect"}
    {Name = "Enterprise"; AppearsIn = "Star Trek"}
    {Name = "Atlantis"; AppearsIn = "Real life :-)"}
    {Name = "Rocinante"; AppearsIn = "The expanse"}
    {Name = "Serenity"; AppearsIn = "Firefly"} 
]
~~~~

and return two collections:
~~~~ ocaml
["Normandy"; "Rocinante"; "Enterprise"; "Atlantis"; "Serenity"]

["Mass Effect"; "The expanse"; "Star Trek"; "Real life :-)"; "Firefly"]
~~~~

In the above case the selectors I mentioned would be `spaceship -> spaceship.Name` and `spaceship -> spaceship.AppearsIn`.

### The purpose
I have to admit, this kind of function seems to have very limited usage. It happened only once that I really needed something like this - but when I started to implement it, I just found it to be a good function to exercise with.

Almost always it seems to be a better idea to transform our input list using the standard `map` function and at the last point, after applying all the transformations, do the unzipping. More, this last step makes sense only if we really need to have two separate collections - which is rarely the case (at least, in my experience).

So, why to write a blog post about it? As mentioned, I found it to be a great code & design exercise. I spent quite a lot of time playing with this, some pieces forced me to learn new things, so I'm just sharing.

### The type
The explicitly typed header of our function looks like here:

~~~~ ocaml
let unzip<'a, 'r1, 'r2>
        (selector1:'a->'r1)
        (selector2:'a->'r2) 
        (items:'a seq)
    :'r1 seq * 'r2 seq
~~~~

Please note the return value, which is `'r1 seq * 'r2 seq`. We said we want our function to return two collections. Clearly, a function can only return one thing, so that's why the output is a tuple of two collections.

In some of the further examples, we will start using F#'s native list type instead of the sequence, mainly to use some of its unique properties. In general, this doesn't change too much in the concept itself.

### Approach one - an easy one.
The idea is like this: when we transform (project) a sequence of things into a sequence of other things we usually use the `map` function. When do we want to transform into two sequences why not to use `map` twice?

~~~~ ocaml
let unzip<'a, 'r1, 'r2>
        (selector1:'a->'r1)
        (selector2:'a->'r2) 
        (items:'a seq)
    :'r1 seq * 'r2 seq =
    
    let s1 = items |> Seq.map selector1
    let s2 = items |> Seq.map selector2

    (s1, s2)
~~~~

This function is so simple, it could even be written as a one-liner. We're a bit verbose since this is for learning purposes. More: we don't really need the types to be explicit. F#'s type inference works great here, so from now on, I'll skip the type annotations.

This first naive version of `unzip` has a huge drawback: it iterates over the sequence twice. Let's try to fix it.

### Approach two - the basic fold
Folds are very powerful functions. You can easily implement a `map` just using some member of the family of folds. Let's try to use one of the most basic ones - `Seq.fold`. 

~~~~ ocaml
let unzip2 select1 select2 items =

    let add item seq  = 
        seq |> Seq.append (Seq.init 1 ( fun _ -> item ))

    let folder (seq1, seq2) item = 
        (seq1 |> add (select1 item), 
         seq2 |> add (select2 item))
        
    items
    |> Seq.fold folder (Seq.empty, Seq.empty) 
~~~~

Here we start with an initial accumulator being simply two empty sequences. In each recursive step, `fold` will invoke the `folder` function which creates a new accumulator value being the result of appending results of both selectors to previous accumulator value. Nothing particularly fancy, just building two collections as we're traversing the input.

This code doesn't have the drawback of our first approach, so it doesn't go twice through the input. 

### Approach three - let's try lists
List in F# is one of the most common collection types. Definitely, our unzipping function should also be available for them. Let's try to create something similar to `unzip2` that would work with F# lists:

~~~~ ocaml
let tMap (fn1,fn2) (item1,item2) = (fn1 item1, fn2 item2)

let unzip3 select1 select2 items =

    let folder (lst1, lst2) item = 
        (select1 item)::lst1, (select2 item)::lst2
        
    items
    |> Seq.fold folder ([], []) 
    |> tMap (List.rev, List.rev) 
~~~~

Note, the `tMap` function which is a quick helper that can map a tuple of two elements using two functions accordingly. We could use a lambda instead of a named function, but I believe it improves readability to have it like it is right now.

The `unzip3` function, while working fine, has final list reversing, which looks bad. Of course, here, with `fold`, it's necessary since we're prepending (using cons operator: `::`) our accumulator with a new element in every step. Remember, in the previous example we were appending, but trying to do it with native F# lists would be a bad idea in terms of performance.

There is, however, a very simple way to get rid of that final `List.rev` step.

### Approach four - foldBack to the rescue
We can simply use `foldBack` instead of `fold`. It effectively iterates the list starting from the end, which will nicely work with our prepending operation. There is no more need to reverse the output collections as they're being built in the right order all the time here.

~~~~ ocaml
let unzip4 select1 select2 items =

    let folder item (lst1, lst2)= 
        (select1 item)::lst1, (select2 item)::lst2
        
    List.foldBack folder items ([], []) 
~~~~

Now this is a pretty damn simple piece of code, isn't it?

### Bonus - the trees
Sequences, lists, etc are collections that organize the data in some linear order. There are, however, some other possibilities to do it. One of those, that is common in programming are trees. 

Turns out we can have a version of our unzip function working with trees. First, let's define an example tree structure we'll work with.

~~~~ ocaml
type Tree<'a> =
    | Leaf of 'a
    | Node of 'a * Tree<'a> list 
~~~~

Now we'll also be going to need something that will work similar to our `foldback` function in case of lists. 

As to why we need something like `foldback` and not just `fold` I suggest to read [short](https://sidburn.github.io/blog/2016/05/28/catamorphisms) or [long](http://fsharpforfunandprofit.com/series/recursive-types-and-folds.html) article (or both). Enough to say here, that we'll be building trees during our function just like we've been building lists. And the immutable `Tree` type needs to be built bottom up.

In fact, we'll use a much simpler folding function, `cata` (again I refer to the articles if this is something new). For our purpose it does exactly the same, `foldback` would do.

~~~~ ocaml
let rec cata fLeaf fNode item=
    let recur = cata fLeaf fNode
    match item with
    | Leaf(a) -> fLeaf(a)
    | Node(a, subTrees) -> fNode(a, subTrees |> List.map recur )  
~~~~

Once we have that, our `unzip` for `Tree` type can be implemented like below:

~~~~ ocaml
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

If we compare it to our previous example, the code looks very similar. We got two functions for two cases of a tree element instead of one for a sequence, but besides this, the structure, and even the internals of those two functions follow the same structure.

This time, let's look at some example of using it:

~~~~ ocaml
type Spaceship = 
    {Name: string; AppearsIn: string}

let Normandy = {Name = "Normandy"; AppearsIn = "Mass Effect"}
let Enterprise = {Name = "Enterprise"; AppearsIn = "Star Trek"}
let Atlantis = {Name = "Atlantis"; AppearsIn = "Real life :-)"}
let Roci = {Name = "Rocinante"; AppearsIn = "The expanse"}
let Serenity = {Name = "Serenity"; AppearsIn = "Firefly"}     

let commandStructure =
    
    Node(Normandy,
        [
            Node(Enterprise,
                [Leaf(Atlantis); Leaf(Roci)]); 
            Leaf(Serenity)
        ])

let getName ship = ship.Name
let getApp ship = ship.AppearsIn

commandStructure 
|> unzipTree getName getApp 
|> printfn "%A"
~~~~

And the output is as expected - two trees:

~~~~ fsharp
(Node
   ("Normandy",
    [Node ("Enterprise",[Leaf "Atlantis"; Leaf "Rocinante"]); Leaf "Serenity"]),
 Node
   ("Mass Effect",
    [Node ("Star Trek",[Leaf "Real life :-)"; Leaf "The expanse"]);
     Leaf "Firefly"]))
~~~~

### Afterword
During the course of this post, we've gone through a bunch of different approaches to the same problem. The goal was to play with it and try to get some solutions that would represent different perspectives of looking at the task.

There are two big areas worth further research:
1. Would be to measure and discuss the performance of certain approaches. There are some questions related to it. Can the function be easily parallelized? For what size of input data does a particular approach work best? Can it efficiently work with infinite collections (sequences in particular)?

2. The observation that some of the proposed implementations were quite similar - like the version with `foldback` for lists and `cata` for trees - leads to the conclusion that this kind of function could be made much more generic. Could we have just a single unzip function that would work with all the data structures? I guess it could be easily done in Haskell. Could we do something like this in F#? If yes, then to what extent?

Having kind of considerations, for me personally, is an inherent part of designing a code solution. Without that it ends up just blindly following the "happy path" which leads to problems more often than not.