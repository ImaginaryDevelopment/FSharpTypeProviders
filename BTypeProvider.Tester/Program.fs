﻿// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    let binputs = BTypeProvider.TypeProvider.JsonProvided.Join.Inputs()
    printfn "%A with members %A" binputs binputs.Input1
    
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code
