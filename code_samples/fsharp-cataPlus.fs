module CataPlus 

type Tree<'a> =
 | Leaf
 | Node of 'a * Tree<'a> * Tree<'a>

let rec cata tree leafVal fNode =
    let recur t = cata t leafVal fNode 
    match tree with
    | Leaf -> leafVal
    | Node(v, left, right) ->
            fNode v (recur left) (recur right)

let rec fold tree acc fNode = 
    let recur tree acc = fold tree acc fNode

    match tree with
    | Leaf -> acc
    | Node (v, left, right) -> 
        let leftAcc = recur left acc
        let newAcc = fNode leftAcc v
        recur right newAcc

let rec cataPlus tree (rootAcc, leafAcc) (fNodeDown, fNodeUp) =
    match tree with
    | Leaf -> leafAcc
    | Node(v, left, right) ->
            let recur t = 
                cataPlus t (fNodeDown rootAcc v, leafAcc) (fNodeDown, fNodeUp) 
            fNodeUp v (recur left) (recur right) rootAcc


module Samples =    
    let sampleTree = 
        Node(1, 
            Node(2, 
                Node(3,Leaf,Leaf), 
                Node(6,Leaf,Leaf)), 
            Node(9,
                Leaf,
                Node(11, Leaf, Leaf)))

    [<Measure>]type tanks; 

    type TravelSegment = 
        {
            Fuel: int<tanks>;
            Destination: string;
        }

    let travelDecisions = 
        Node({Fuel= 0<tanks>; Destination= "Warsaw"}, 
            Node({Fuel= 5<tanks>; Destination= "Paris"},
                Node({Fuel= 3<tanks>; Destination= "Barcelona"}, Leaf, Leaf),
                Node({Fuel= 2<tanks>; Destination= "Zurich"} ,Leaf,Leaf)), 
            Node({Fuel= 2<tanks>; Destination= "Budapest"},
                Node({Fuel= 4<tanks>; Destination= "Athens"}, Leaf, Leaf),
                Node({Fuel= 1<tanks>;Destination= "Vienna"}, Leaf, Leaf)))

    let inline updateFuelLevel fuelLeft segment= 
        fuelLeft - segment.Fuel

    let decideLongerSubRoute segment lLongest rLongest fuelLeft =
        let longerSubRoute = 
            if(List.length lLongest > List.length rLongest)
            then lLongest
            else rLongest 
        
        if fuelLeft <= 0<tanks> 
        then []
        else segment::longerSubRoute

    let mostStopsTravelPlan =
        cataPlus 
            travelDecisions 
            (6<tanks>, [])
            (updateFuelLevel, decideLongerSubRoute)

               

