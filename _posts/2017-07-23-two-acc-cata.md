---
layout: post
title: "Splitting data structures through each element (F#)"
tags: [F#, cata, tree]
description: ""
keywords: fsharp, tree, catamorphism, cata
published: false
comments: true
excerpt_separator: <!--more-->
---
Most articles providing introduction to catamorphisms I've read present concepts of a `cata` function and a `foldl` ("left" fold) function and explain how they differ using a binary tree example.
One the most important differences is the input to the function at each recursive step. While `cata` will receive results of reducing all current subtrees, fold will only see a single value accumulated across nodes traversed so far.

What we'll see today is a function that in some way combines those two so that at each step it can use reduced subtrees and a value accumulated going deeper from the root.
<!--more-->
*If You don't know the concept of catamorphisms I encourage You to read [short](https://sidburn.github.io/blog/2016/05/28/catamorphisms) or [long](http://fsharpforfunandprofit.com/series/recursive-types-and-folds.html) article (or both)*
*All the code samples from this text can be found [here](https://github.com/blainne/blainne.github.io/blob/master/code_samples/fsharp-cataPlus.fs).*

Now we can start our little research. Let's see an example tree type we'll be working with:

~~~~ ocaml
type Tree<'a> =
 | Leaf
 | Node of 'a * Tree<'a> * Tree<'a>
~~~~

###`Cata` function
Let's discuss "standard", simple, non tail-recursive `cata` function.

~~~~ ocaml
//type: Tree<'a> -> 'b -> ('a -> 'b -> 'b -> 'b) -> 'b
let rec cata tree leafVal fNode =
    let recur t = cata t leafVal fNode 
    match tree with
    | Leaf -> leafVal
    | Node(v, left, right) ->
            fNode v (recur left) (recur right)
~~~~

The recursion in this function will first go deep to the leaves of the tree. There it'll stop going deeper and just return `leafVal`. This is where building the final value for the tree starts in the `cata` function. On the way back to the root, in each node, the `fNode` function combines results of reducing two subtrees and a value stored in that node.


The image below presents this. The `leafVal` is 0, and the `fNode` function is just adding values of current node, left subtree and right subtree. Current node original value is marked red. The first diagram presents the input tree.

![Cata example](/images/cataExample-cataPlus.png)

Note that while the `fNode` function was chosen as a simple example, in an advanced usage You could use in Your logic the fact, that You always see three separate values as an input to the function. 

###`Fold` function

Time for a fold:
~~~~ ocaml
//type: Tree<'a> -> 'b -> ('b -> 'a -> 'b) -> 'b
let rec fold tree acc fNode = 
    let recur tree acc = fold tree acc fNode

    match tree with
    | Leaf -> acc
    | Node (v, left, right) -> 
        let leftAcc = recur left acc
        let newAcc = fNode leftAcc v
        recur right newAcc
~~~~

Once again, we look for the simplicity. We could have a `fold` version that uses some nice functional ways to achieve tail recursion, but the point we need to get here is different. What we need to focus on is the fact that our fNode function only uses the single acc argument and passes it to the recursive call.

It'll start with an initial accumulator value and pass it deeper to calculate the left subree. Once it has the reduced value for left subtree (`leftAcc`) it'll use it as the input to the fNode function to calculate a new accumulator (`newAcc`). The latter will be then passed as an input to calculate the right subtree. As before, we can see this depicted on the image below:

![Cata example](/images/foldExample-cataPlus.png)

*On a side note: the `fold` function presented here traverses the tree with an [in-order method](https://en.wikipedia.org/wiki/Tree_traversal). I couldn't find any proof but it seems intuitive that, unlike `cata`, the `fold` function has the traverse order fixed in a specific implementation.*



1. Start with code of cata and fold
2. Discuss accumulators seen (with pictures!)
3. Describe the use case (possibly mention hackerrank swap trees problem, decision tree with costs)
4. Introduce function
5. Summary - mention tail recursive version, and something more there
