// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.
#r "Newtonsoft.Json.dll"
#load "ProvidedTypes.fs"
#load "SimpleProvider.fs"
#load "Library1.fs"
open System

open BTypeProvider.SimpleObjectProvider.Provided
open BTypeProvider.TypeProvider.JsonProvided

// Define your library scripting code here
let thing = MyType()
let thingInnerState = thing.InnerState
let thing2 = MyType("Some other text")
let thing2InnerState = thing2.InnerState
;;
