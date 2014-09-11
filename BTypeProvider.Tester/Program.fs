// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System
open BTypeProvider
open BTypeProvider.TypeProvider.Provided
type thisOne = MavnnProvider<"C:\projects\FSharpTypeProviders\BTypeProvider\Sample.json">
[<Literal>]
let connectionString = @"Data Source= FOO;Initial Catalog=DatabaseName;Integrated Security=true"
type ProgProvider = SqlProgrammabilityProvider<connectionString>
[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    let myJoin = thisOne.Join("A join!", Guid.NewGuid(), "some config")
    printfn "%A config=%A" myJoin myJoin.Config
    let provider = ProgProvider ()
    
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code
