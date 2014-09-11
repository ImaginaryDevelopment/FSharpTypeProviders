module BTypeProvider.TypeProvider

open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open System.Reflection

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