﻿module BTypeProvider.TypeProvider

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
        myType.AddMember(myProp)
        [myType]

    do
        this.AddNamespace(ns, createTypes())

[<assembly:TypeProviderAssembly>]
do ()