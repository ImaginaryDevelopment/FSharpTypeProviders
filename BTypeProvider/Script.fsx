// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.
#load "ProvidedTypes.fs"
#load "Library1.fs"
open BTypeProvider.TypeProvider.Provided

// Define your library scripting code here
let thing = MyType()
let thingInnerState = thing.InnerState
let thing2 = MyType("Some other text")
let thing2InnerState = thing2.InnerState
;;
