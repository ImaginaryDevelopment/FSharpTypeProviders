// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System
open BTypeProvider.TypeProvider.Provided
type thisOne = MavnnProvider<"C:\projects\FSharpTypeProviders\BTypeProvider\Sample.json">

[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    let myJoin = thisOne.Join("A join!", Guid.NewGuid(), "some config")
    printfn "%A config=%A" myJoin myJoin.Config
    
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code
