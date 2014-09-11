module BTypeProvider.TypeProvider
open System
open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Newtonsoft.Json

type private Id() = 
    member val UniqueId = Guid() with get, set
    member val Name = "" with get, set
type private Port() = 
    member val Id = Id() with get, set
    member val Type = "" with get, set
type private Node () = 
    member val Id = Id() with get,set
    member val Ports = Collections.Generic.List<Port>() with get,set
let private nodes = 
    System.Diagnostics.Trace.WriteLine(sprintf "Trace: Working from %s" Environment.CurrentDirectory)
    System.Diagnostics.Debug.WriteLine(sprintf "Debug: Working from %s" Environment.CurrentDirectory)
    printfn "Printfn %s" Environment.CurrentDirectory
    JsonConvert.DeserializeObject<seq<Node>>(IO.File.ReadAllText("Sample.json"))
    |> Seq.map (fun n -> n.Id.UniqueId.ToString(), n)
    |> Map.ofSeq
let private GetNode id = nodes.[id]
let private ports = 
    nodes
    |> Map.toSeq
    |> Seq.map (fun (_, node) -> node.Ports)
    |> Seq.concat
    |> Seq.map (fun p -> p.Id.UniqueId.ToString(), p)
    |> Map.ofSeq
let private GetPort id = ports.[id]

type private nodeInstance = { Node : Node; InstanceId : Id; Config : string}
module private NodeInstance = 
    let create node name guid config = 
        {Node = node; InstanceId = Id(Name = name, UniqueId = guid); Config = config}
type private InputPort = | InputPort of Port
type private OutputPort = | OutputPort of Port

[<TypeProvider>]
type JsonProvider(config: TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces ()

    let ns = "BTypeProvider.TypeProvider.Provided"
    let asm = Assembly.GetExecutingAssembly()

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
        let ctor = ProvidedConstructor (
                        [ 
                            ProvidedParameter("Name", typeof<string>)
                            ProvidedParameter("UniqueId", typeof<Guid>)
                            ProvidedParameter("Config", typeof<string>)
                        ],
                        InvokeCode = fun [name;unique;config] -> <@@ NodeInstance.create (GetNode id) (%%name:string) (%%unique:Guid) (%%config:string) @@>)
        nodeType.AddMember ctor
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
        nodeType.AddMembers [inputPorts;outputPorts]
        nodeType
    let createTypes () = 
        nodes |> Map.map createNodeType |> Map.toList |> List.map (fun (k,v) -> v)
    do
        this.AddNamespace(ns, createTypes())
[<TypeProvider>]
type BusinessObjectTypeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces ()
    let ns = "BTypeProvider.TypeProvider.Provided"
    let asm = Assembly.GetExecutingAssembly()
    
    let createTypes () =
        let myType = ProvidedTypeDefinition(asm, ns, "MyType", Some typeof<obj>)
        let myProp = ProvidedProperty("MyProperty", typeof<string>, IsStatic = true,
                                        GetterCode = (fun args -> <@@ "Hello world" @@>))
        myType.AddMember myProp
        let ctor = ProvidedConstructor([], InvokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember ctor
        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], 
                                    InvokeCode = fun args -> <@@ (%%(args.[0]):string):> obj @@>)
        myType.AddMember ctor2
        let innerState = ProvidedProperty("InnerState", typeof<string>, 
                                    GetterCode = fun args -> <@@ (%%(args.[0]) :> obj) :?> string @@>)
        myType.AddMember innerState
        [myType]

    do
        this.AddNamespace(ns, createTypes())

[<assembly:TypeProviderAssembly>]
do ()