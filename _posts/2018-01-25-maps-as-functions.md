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
We all know the concept of a function. To each value in the input domain it assigns exactly one output in the output domain. In programming languages as well as in math we mostly deal with functions expressed as formulas, but it is not the only way to describe some function. While it's usually the most convenient one, sometimes it's easier to write a function in form of a table. In context of programming we may think about such table as a `Map` or `Dictionary` type. In this post we'll try to explore this idea and look how far we can get when expressing functions as F# maps.
<!--more-->

*All the code samples from this text can be found [here](https://github.com/blainne/blainne.github.io/blob/master/code_samples/fsharp-tableFunctions.fs).*

Let's consider simple square function. It takes a number and returns that number squared. We usually write it using the `f(x) = x*x` notation. This is "formula notation". It tells us how to compute the output from the input.
The "table notation" would be something of this form:

|Input|Output|
|-----|------|
|1.5  |2.25  |
|2    |4     |
|3    |9     |

We see right away that it's impossible to write down such mapping for all the numbers.

This is our first serious limitation: we can only express functions where the input type has finite number of values. Although `int32` type satisfies this, it is still highly impractical to define such big tables. 
What else can we say about it? As opposed to formulas, we don't know how to compute (calculate) the output and we don't need to - it's given right away. There are some important implications:

 1 If we use a proper data structure for the table, we can have the result in constant time.
 2 There's nothing like generic function here.
 3 We can express partial functions.
 4 It's not possible to define an impure function.

Let's discuss these bullets a bit more.

First point means that if we're able to express a computationally expensive function to it's table representation we're trading CPU time for memory usage. In fact, there exists  a well known technique which bases on this tradeoff: memoization. Memoized function is basically building a table representation of itself in memory. The second time You call a it with an argument You used before it'll, go to that table and give You the results in instant time. The more values we memoize, the more memory we take.

About the generics we can think like this: to define a table function we need to write down all the possible inputs and outputs. But if we do this we're already fixed on input and output types. If we'd like the function to support some other type as well we'd have to define second version of this function.

The third is a bit more tricky. A partial function is one which doesn't handle all possible input values of a type. Generally it's something bad, but even [haskell has some] (https://wiki.haskell.org/Partial_functions). On the other hand it may be, that they're more about strong typing than anything else. Let's think about that. If a function - let's say: integer division - doesn't handle all the possible input values it means that the set of integers is not the true input domain of this function. The integers-without-zero are. Since it would be inconvenient to create a new type `IntegersWithoutZero` and converting the value from `int` and back every time we want to divide a number, programming languages usually push that duty to programmer to check for zeroes when dividing.
When expressing functions as tables, it's much more likely to skip the input values we're sure we'll never use - since otherwise we'd have to explicitly write them down.

Fourth implication is almost intuitive. Since there's no true execution or computation happening - there's no place for any additional behavior, so we can't have any side effects. Once a table function is defined it'll always return the same result for the same input - it's just a relation.

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

That produces a typical, formula expressed function. But we can keep the same relation in... a `Map`:
~~~~ ocaml
// Map<CelestialBody, Set<Inhabitants>>
let ``Get inhabitants`` =
    Map[
        Earth, Set[Humans;Robots]
        Mars, Set[Robots]
        Europa, Set[Aliens]
    ]
~~~~

*Note: For this post I decided to have the naming convention where "table functions" are named with normal sentences containing punctuation and spaces wrapped with ``` `` ``` (double backticks). F# allows for that and I think, in this case it's a pleasingly weird way to distinguish standard and table functions.*

What's the advantage of implementing it this way?
One is that we can easily serialize the entire function and transfer it over the network. We also have a standard data structure which we can manipulate just as we would normally do. We'll try one such manipulation later in this post.

We have a map, we also need some small facility to be able to "run" our map - function. That's nothing else but just getting a value of given key. We shall note that find will throw if key is not in the map. You may think about this behavior as an equivalent of throwing `ArgumentException`.

~~~~ ocaml
// Map<'a,'b> -> 'a -> 'b
let run fm arg = Map.find arg fm

//returns set [Robots]
run ``Get inhabitants`` Mars
~~~~

### Composition

If we treat maps as functions, we should be able to compose them. For normal functions there exists the `>>` operator (and its counterpart working in opposite direction, but we'll skip that). For table functions we need to give up on trying to define the operator itself as a table function - because of the inability to have proper generics. Instead we will use a normal function - `Map.map`.

~~~~ ocaml
// Map<'a,'b> -> Map<'b,'c> -> Map<'a,'c>
let fmCompose fm1 fm2 =
    fm1
    |> Map.map (fun _ v -> run fm2 v)

let (>-) = fmCompose
~~~~

The signature suggests it's right. If we consider `Map<'a,'b>` to be equivalent of function `a -> b` then the type signature is similar to the one of the `>>` operator (`('a -> 'b) -> ('b -> 'c) -> 'a -> 'c`. The slight difference is related to currying, which we will explore later in this article.

Let's try to compose some maps now. We will of course need to define one more function with type matching the ``` ``Get inhabitants`` ``` to be able to compose. To have some nice additional twist lets also define the table version of `not` operator.

~~~~ ocaml
// Map<Set<Inhabitants>,bool>
let ``Can they co-exist?`` =
    Map[
        Set[Aliens;Humans;Robots], false
        Set[Humans; Robots], true
        Set[Robots; Aliens], true
        Set[Aliens; Humans], false
        Set[Aliens], true
        Set[Robots], true
        Set[Humans], true
    ]

// Map<bool,bool>
let ``Negate answer`` =
    Map[
        true, false
        false, true
    ]
~~~~

Now we compose the three table functions.

~~~~ ocaml
// Map<CelestialBody,bool>
let ``Is there inhabitant conflict?`` =
    ``Get inhabitants`` 
    >- ``Can they co-exist?``
    >- ``Negate answer``
~~~~

What's nice, the F# interactive prints the resulting map right away so we don't even need to "run" the map to see what happened:

~~~~ ocaml
val ( Is there inhabitant conflict? ) : Map<CelestialBody,bool> =
  map [(Earth, false); (Mars, false); (Europa, false)]
~~~~

### Higher order functions

Let's now consider how we could express one of the kinds of functions which are very important for programmers. As we know, [higher order function](https://en.wikipedia.org/wiki/Higher-order_function) is one which can take some function as a parameter or return it (or both at the same time)

Creating a table function which would return another function is relatively simple - we need to create a `Map<'a, 'Map<'b'c>>`. It's the equivalent of `'a -> ('b -> 'c)` function.

Let's try to write a function which will answer a question whether given inhabitants visited given celestial body. The type should be equivalent of `Inhabitants -> (CelesialBody -> bool)`, so we need a `Map<Inhabitants, Map<CelestialBody, bool>>`. We got three kinds of inhabitants so our outer map should provide three relations (unless we decide to only make a partial function). The three are:

~~~~ ocaml
let ``Was a human there?`` =
    Map[
        Mars, false
        Earth, true
        Europa, false
    ]

let ``Was a robot there?`` =
    Map[
        Mars, true
        Earth, true
        Europa, false
    ]

//don't take it as a fact - it's made up
let ``Was an alien there?`` =
    Map[
        Mars, true
        Earth, true
        Europa, true
    ]
~~~~

Great, now we can finally show our first higher order map:

~~~~ ocaml
//Map<Inhabitants,Map<CelestialBody,bool>>
let ``Was an existence form there?``=
    Map[
        Humans, ``Was a human there?``
        Aliens, ``Was an alien there?``
        Robots, ``Was a robot there?``
    ]
~~~~


##### A small interlude: currying
If we think about that, the presented function has a very interesting structure. It takes an input parameter and then returns the result which is also a function, so it allows us to pass second parameter to it. We have now a two-parameter function! We just need yet another bit of utility to use it as we would do with normal two-parameter function:

~~~~ ocaml
let run2 fm arg1 arg2 =
   run (run fm arg1) arg2

run2 ``Was an existence form there?`` Humans Mars
~~~~

In fact, in F# all multi-parameter normal functions can be considered higher order in this way. A (normal) function `a -> b -> c` can always be treated as a function, which given a parameter of type `a` returns a function `b -> c`.

All this boils down to an observation, that table functions are naturally [curried](https://fsharpforfunandprofit.com/posts/currying/).

##### Higher order functions continued
We still need to implement support for functions which take other functions as an input. The issue here is that more often than not the total number of possible input functions of given type (e.g. `DateTime -> int`, how many different functions can we have) is beyond what gets practical to support in terms of table functions (remember we got to write all of them as tables as well). And if we don't handle the entire possible input space, our function is partial.

If we consider functions mapping some types with very small amount values, like `bool -> bool`, there are only few of them possible, so we could seriously think about implementing our equivalent of (`(bool -> bool) -> something`). Here however, we're going to define a partial function.

For our example let's create a table function which will take a `Map<CelestialBody, bool>` as a first parameter and a tuple of celestial bodies as a second parameter (we already know how to create two-parameter functions) and will return a tuple of answers. The equivalent normal function could look like this:

~~~~ ocaml 
let tupleMap f (t1,t2) = (f t1, f t2)
~~~~

Of course, as we discussed earlier we will not be able to make our function generic in any way, but it sort of shows what we want to do.

~~~~ ocaml
//Map<Map<CelestialBody,bool>, Map<(CelestialBody * CelestialBody),(bool * bool)>>
let ``Give answer for a tuple:`` =
    Map[
       ``Was a robot there?``, 
        Map[(Mars,Earth),(true, true); (Earth, Europa), (true, true)];
       
       ``Was a human there?``, 
       Map[(Mars,Earth),(false, true); (Earth, Europa), (true, false)];
    ]
~~~~

Let's focus on the type first. It's a map (that is: a table function) which relates another two maps. So we can say it takes a table function as a parameter and returns another table function as a result. We've seen this trick with returning a map in previous section and we already we can treat it as a way to allow passing second argument.

Let's run it (please note how below code reads fluently):

~~~~ ocaml
// returns (true, false)
run2 ``Give answer for a tuple:`` ``Was a human there?`` (Earth, Europa)

// returns (true, true)
run2 ``Give answer for a tuple:`` ``Was a robot there?`` (Mars, Earth)

// throws KeyNotFoundException
run2 ``Give answer for a tuple:`` ``Was a human there?`` (Earth, Mars)

// throws KeyNotFoundException
run2 ``Give answer for a tuple:`` ``Was an alien there?`` (Earth, Europa)
~~~~

The first two examples confirm that our function is working fine. The last two need some explanation.
In the third example we pass a tuple which we don't support as our second parameter (the key of returned map). This means that we're in fact returning a partial function. The last example takes a first parameter which we don't handle at all. It means that the entire ``` ``Give answer for a tuple:`` ``` is a partial function.

Let the conclusion for this section be that we can still fallback to partial functions when it's impractical to handle the entire possible input space. We just need to make sure we never pass an illegal input - as the compiler won't warn us.
The fact that tables involve no computation at all make the concept of higher order table functions rather not useful, since we need to precompute everything during implementation.

### Modifying table functions

There's one more interesting thing we can do with table functions. Since our functions are maps, we can use available map operations to add new or alter existing entries. Let's consider the following operation:

~~~~ ocaml
let extend original ext =
    ext
    |> Map.fold 
            (fun acc k v -> Map.add k v acc) 
            original
~~~~

The `Map.add` function will return a map containing new entry if it doesn't exist in the original map, or otherwise with the entry replaced with new value. Of course, in F# the `Map` type is immutable, so we rather end up having new function based on an existing one than really modifying it. If we think about it we could potentially define something similar for "normal" functions, but it probably wouldn't be that simple.

Let's see how it works:

~~~~ ocaml

let ``What are the satellites?`` =
    Map[
        Earth, ["Moon"]
        Mars, ["Phobos"]
    ]

let ``What are the satellites (improved)?`` = 
    extend
        ``What are the satellites?`` 
        (Map[Europa,[]; Mars, ["Phobos"; "Deimos"]])
~~~~

First we define a function that enumerates the moons of a celestial body. Some time later we notice that we forgot about one of the Mars' moons and also to make the function total, we should really handle situation when someone passes in Europa. The output of running the above code in F# interactive is:

~~~~ ocaml
val ( What are the satellites? ) : Map<CelestialBody,string list> =
  map [(Earth, ["Moon"]); (Mars, ["Phobos"])]

val ( What are the satellites (improved)? ) : Map<CelestialBody,string list> =
  map [(Earth, ["Moon"]); (Mars, ["Phobos"; "Deimos"]); (Europa, [])]
~~~~

Works as expected, we were able to extend our table function with new knowledge. In a similar way we could implement removing some keys from a map.

The final thing to notice is that fact that we could implement our table functions using a mutable data structure, like the standard .NET's `Dictionary<k,v>` type. Our extend function would then have the property of truly modifying the functions. Now that would be insane!

### One unknown thing

One very important thing we can do with "normal" functions is recursion. It may seem intuitive, that expressing recursion with table functions is not possible. The reasoning could be that a function calling itself involves the computation step. The recursive function also has some return value and table functions simply provide this final value as an output. There's no place for a recursive call...
However, I have some doubts about this. I am yet to grok the [Y combinator](https://stackoverflow.com/questions/93526/what-is-a-y-combinator) and some other stuff related to the recursion so with my current knowledge it would be to much of a speculation to state that it's not doable.

*If this post resonates with You in any way, please leave a comment. I'd love to have some feedback.*