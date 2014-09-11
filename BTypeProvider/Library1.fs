module BTypeProvider.TypeProvider
open System
open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Newtonsoft.Json

type Id () =
    member val UniqueId = Guid() with get, set
    member val Name = "" with get, set

let private embeddedId (id : Id) =
    let guid = sprintf "%A" (id.UniqueId)
    let name = id.Name
    <@ Id(UniqueId = Guid(guid), Name = name) @>

type Port () =
    member val Id = Id() with get, set
    member val Type = "" with get, set

let private embeddedPort (port : Port) =
    let idExpr = embeddedId port.Id
    let type' = port.Type
    <@ Port(Id = %idExpr, Type = type') @>

type Node () =
    member val Id = Id() with get, set
    member val Ports = Collections.Generic.List<Port>() with get, set

type InputPort = | InputPort of Port
type OutputPort = | OutputPort of Port

type nodeInstance =
    {
        Node : Node
        InstanceId : Id
        Config : string
    }

module NodeInstance =
    let create node name guid config =
        { Node = node; InstanceId = Id(Name = name, UniqueId = guid); Config = config }    

[<TypeProvider>]
type MavnnProvider(config: TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces ()

    let ns = "BTypeProvider.TypeProvider.Provided"
    let asm = Assembly.GetExecutingAssembly()
    let mavnnProvider = ProvidedTypeDefinition(asm,ns, "MavnnProvider", Some typeof<obj>)

    let parameters = [ProvidedStaticParameter("PathToJson", typeof<string>)]

    do mavnnProvider.DefineStaticParameters(parameters, fun typeName args ->
        let pathToJson = args.[0] :?> string

        let provider = ProvidedTypeDefinition (typeName, Some typeof<obj>, HideObjectMethods = true)
        let nodes = 
            System.Diagnostics.Trace.WriteLine(sprintf "Trace: Working from %s" Environment.CurrentDirectory)
            System.Diagnostics.Debug.WriteLine(sprintf "Debug: Working from %s" Environment.CurrentDirectory)
            printfn "Printfn %s" Environment.CurrentDirectory
            JsonConvert.DeserializeObject<seq<Node>>(IO.File.ReadAllText(pathToJson))
                |> Seq.map (fun n -> n.Id.UniqueId.ToString(), n)
                |> Map.ofSeq
        let GetNode id = nodes.[id]

        let ports =
            nodes
            |> Map.toSeq
            |> Seq.map (fun (_, node) -> node.Ports)
            |> Seq.concat
            |> Seq.map (fun p -> p.Id.UniqueId.ToString(), p)
            |> Map.ofSeq

        let GetPort id = ports.[id]
        let addInputPort (inputs: ProvidedTypeDefinition) (port : Port) =
            let port = ProvidedProperty (
                        port.Id.Name,
                        typeof<InputPort>,
                        GetterCode = fun args -> 
                            let id = port.Id.UniqueId.ToString()
                            <@@ GetPort id |> InputPort @@>)
            inputs.AddMember port
        let addOutputPort (outputs : ProvidedTypeDefinition) (port : Port) =
            let port = ProvidedProperty(
                        port.Id.Name,
                        typeof<OutputPort>,
                        GetterCode = fun args -> 
                            let id = port.Id.UniqueId.ToString()
                            <@@ GetPort id |> OutputPort @@>
            )
            outputs.AddMember port
        let addPorts inputs outputs (portList: Port seq) =
            portList
            |> Seq.iter (fun port ->
                match port.Type with
                | "input" -> addInputPort inputs port
                | "output" -> addOutputPort outputs port
                | _ -> failwithf "Unknown port type for port %s/%s" port.Id.Name (port.Id.UniqueId.ToString())
                )
        
        let createNodeType id (node : Node) =
            let nodeType = ProvidedTypeDefinition(asm, ns, node.Id.Name, Some typeof<nodeInstance>)

            let addInputOutput () = 
            
                let outputs = ProvidedTypeDefinition("Outputs", Some typeof<obj>)
                let outputCtor = ProvidedConstructor([], InvokeCode = fun args -> <@@ obj() @@>)
                outputs.AddMember outputCtor
                outputs.HideObjectMethods <- true

                let inputs = ProvidedTypeDefinition("Inputs", Some typeof<obj>)
                let inputCtor = ProvidedConstructor([], InvokeCode = fun args -> <@@ obj() @@>)
                inputs.AddMember inputCtor
                inputs.HideObjectMethods <- true
                addPorts inputs outputs node.Ports

                nodeType.AddMembers [inputs;outputs]

                let outputPorts = ProvidedProperty("OutputPorts", outputs, [],
                                    GetterCode = fun args -> <@@ obj() @@>)
                let inputPorts = ProvidedProperty("InputPorts", inputs, [],
                                    GetterCode = fun args -> <@@ obj() @@>)
                [inputPorts;outputPorts]

            nodeType.AddMembersDelayed addInputOutput
            provider.AddMember nodeType

        let createTypes () = 
            nodes |> Map.map createNodeType |> Map.toList |> List.map (fun (k,v) -> v)
        createTypes() |> ignore
        provider
        )
    do
        this.AddNamespace(ns, [mavnnProvider])
[<assembly:TypeProviderAssembly>]
do ()