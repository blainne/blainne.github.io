type CelestialBody =
    | Earth | Mars | Europa 

type Inhabitants =
    | Humans | Robots | Aliens 

let run fm arg = Map.find arg fm 

let run2 fm arg1 arg2 =
   run (run fm arg1) arg2

let GetInhabitants =
    function
    | Earth -> Set[Humans;Robots]
    | Mars -> Set[Robots]
    | Europa -> Set[Aliens]
    

let ``Get inhabitants`` =
    Map[
        Earth, Set[Humans;Robots]
        Mars, Set[Robots]
        Europa, Set[Aliens]
    ]

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

let ``Celestial body itself`` =
    Map[
        Mars,Mars
        Earth,Earth
        Europa,Europa
    ]

//two possible functions for filtering
let ``Is it a planet?`` =
    Map[
        Mars, true
        Earth, true
        Europa, false
    ]

let ``Negate answer`` = 
    Map[
        true, false
        false, true
    ]

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

//don't take it as a fact
let ``Was an alien there?`` =
    Map[
        Mars, true
        Earth, true
        Europa, true
    ]

let ``Was an existence form there?``=
    Map[
        Humans, ``Was a human there?``
        Aliens, ``Was an alien there?``
        Robots, ``Was a robot there?``
        ]

run2 ``Was an existence form there?`` Humans Mars

let fmCompose fm1 fm2 = 
    fm1
    |> Map.map (fun _ v -> run fm2 v)

let (>-) = fmCompose

let ``Is there inhabitant conflict?`` =
    ``Get inhabitants`` 
    >- ``Can they co-exist?``
    >- ``Negate answer``

run ``Get inhabitants`` Mars
run ``Is there inhabitant conflict?`` Mars

let tupleMap f (t1,t2) =
    (f t1, f t2)
let ``Give answer for a tuple:`` =
    Map[
       ``Was a robot there?``, 
        Map[(Mars,Earth),(true, true); (Earth, Europa), (true, true)];
       
       ``Was a human there?``, 
       Map[(Mars,Earth),(false, true); (Earth, Europa), (true, false)];
    ]

run2 ``Give answer for a tuple:`` ``Was a human there?`` (Earth, Europa)
run2 ``Give answer for a tuple:`` ``Was a robot there?`` (Mars, Earth)

//throws
//run2 ``Give answer for a tuple:`` ``Was a human there?`` (Earth, Mars)
//throws
//run2 ``Give answer for a tuple:`` ``Was an alien there?`` (Earth, Europa)

let extend original ext =
    ext
    |> Map.fold 
            (fun acc k v -> Map.add k v acc) 
            original

let ``What are the satellites?`` =
    Map[
        Earth, ["Moon"]
        Mars, ["Phobos"]
    ]

let ``What are the satellites (improved)?`` = 
    extend 
        ``What are the satellites?`` 
        (Map[Europa,[]; Mars, ["Phobos"; "Deimos"]])

