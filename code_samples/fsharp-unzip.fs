module Unzipping
//double iteration - bad
let unzip<'a, 'r1, 'r2>
        (selector1:'a->'r1)
        (selector2:'a->'r2) 
        (items:'a seq)
    :'r1 seq * 'r2 seq =
    
    let s1 = items |> Seq.map selector1
    let s2 = items |> Seq.map selector2

    (s1, s2)

let unzip2 select1 select2 items =

    let add item seq  = 
        seq |> Seq.append (Seq.init 1 ( fun _ -> item ))

    let folder (seq1, seq2) item = 
        (seq1 |> add (select1 item), 
         seq2 |> add (select2 item))
        
    items
    |> Seq.fold folder (Seq.empty, Seq.empty) 


//fixes ineffective append from previous
//but forces us to reverse the lists which still may be as ineffective as double iteration

//introduce tuple map:
let tMap (fn1,fn2) (item1,item2) = (fn1 item1, fn2 item2)

let unzip3 select1 select2 items =

    let folder (lst1, lst2) item = 
        (select1 item)::lst1, (select2 item)::lst2
        
    items
    |> Seq.fold folder ([], []) 
    |> tMap (List.rev, List.rev) //tMap could be one arg function in this case but cannot be because of lack of HKTs
    |> tMap (Seq.ofList, Seq.ofList)
//foldback

//working on lists only so we can get rid of 
//tuple mapping
let unzip4 select1 select2 items =

    let folder item (lst1, lst2)= 
        (select1 item)::lst1, (select2 item)::lst2
        
    List.foldBack folder items ([], []) 


//mutable lists would solve the issue of ineffective append
//but it's .NET specific implementation (is it worth mentioning?)
type MutList<'t> = System.Collections.Generic.List<'t>
let toSeq<'t> l = l :> seq<'t>
let unzip5 select1 select2 items =
    let folder (netLst1 : MutList<'r1>, netLst2 : MutList<'r2>) item = 
        netLst1.Add(select1 item)
        netLst2.Add(select2 item)
        (netLst1, netLst2)
    
    items
    |> Seq.fold folder (new MutList<'r1>(), new MutList<'r2>()) 
    |> tMap (toSeq<'r1>, toSeq<'r2>)


//performance considerations
//append on sequence is associative
//can fold be parallelized

module Tree =
    type Tree<'a> =
        | Leaf of 'a
        | Node of 'a * Tree<'a> list

    let rec cata fLeaf fNode item=
        let recur = cata fLeaf fNode
        match item with
        | Leaf(a) -> fLeaf(a)
        | Node(a, subTrees) -> fNode(a, subTrees |> List.map recur )  
        
    
    let unzipTree<'a, 'r1, 'r2>
            (select1:'a->'r1)
            (select2:'a->'r2)             
            (tree: Tree<'a>)
        : Tree<'r1> * Tree<'r2> =

        let unzipLeaf a= 
            (Leaf(select1 a),
             Leaf(select2 a))
        
        let unzipNode (a,subtrees) =
            (Node(select1 a, subtrees |> List.map fst),
             Node(select2 a, subtrees |> List.map snd))

        tree |> cata unzipLeaf unzipNode

module Tests =
    open Tree

    type Spaceship = 
        {Name: string; AppearsIn: string}

    let Normandy = {Name = "Normandy"; AppearsIn = "Mass Effect"}
    let Enterprise = {Name = "Enterprise"; AppearsIn = "Star Trek"}
    let Atlantis = {Name = "Atlantis"; AppearsIn = "Real life :-)"}
    let Roci = {Name = "Rocinante"; AppearsIn = "The expanse"}
    let Serenity = {Name = "Serenity"; AppearsIn = "Firefly"}     

    let knownShips = 
        [ Normandy; Roci; Enterprise; Atlantis; Serenity ]
    
    let commandStructure =
        
        Node(Normandy,
            [
               Node(Enterprise,
                    [Leaf(Atlantis); Leaf(Roci)]); 
               Leaf(Serenity)
            ])
    
    let getName ship = ship.Name
    let getApp ship = ship.AppearsIn
    

    knownShips |> unzip getName getApp |> printfn "%A" 
    knownShips |> unzip2 getName getApp |> printfn "%A" 
    knownShips |> unzip3 getName getApp |> printfn "%A" 
    knownShips |> unzip4 getName getApp |> printfn "%A" 
    knownShips |> unzip5 getName getApp |> printfn "%A" 

    commandStructure |> unzipTree getName getApp |> printfn "%A"
