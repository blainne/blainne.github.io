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
    match tree with
    | Leaf -> acc
    | Node (v, left, right) -> 
        let leftAcc = fold left acc fNode
        fold right (fNode leftAcc v) fNode

let rec cataBiDir tree (rootAcc, leafAcc) (fNodeDown, fNodeUp) =
    match tree with
    | Leaf -> leafAcc
    | Node(v, left, right) ->
            let recur t = 
                cataBiDir t (fNodeDown v rootAcc, leafAcc) (fNodeDown, fNodeUp) 
            fNodeUp v (recur left) (recur right) rootAcc


let tree = Node(1, Node(2, Node(3,Leaf,Leaf), Node(6,Leaf,Leaf)), Node(9,Leaf,Node(11, Leaf, Leaf)))

