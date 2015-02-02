namespace IntelliFactory.WebSharper.InterfaceGenerator.Tests

open IntelliFactory.WebSharper

module Definition =
    open IntelliFactory.WebSharper.InterfaceGenerator

    let JustX =
        Pattern.Config "JustX" {
            Required = [ "x", T<int> ]
            Optional = []
        }

    let WIGtest =
        Class "WIGtest"
        |+> Static [
            "ArgsFuncIn" => (T<int> * T<int> ^-> T<int>)?add ^-> T<int>    
            "ArgsFuncOut" => T<unit> ^-> (T<int> * T<int> ^-> T<int>)
            Generic - fun a -> "GetGetThis" => T<unit> ^-> (a -* T<unit> ^-> a)            
            "FuncInWithThis" => (JustX -* T<unit> ^-> T<string>) ^-> T<string>
            "ArgFuncInWithThis" => (JustX -* T<int> ^-> T<string>) ^-> T<string>
            "ArgsFuncInWithThis" => (JustX -* T<int> * T<int> ^-> T<string>) ^-> T<string>
            "TupledFuncInWithThis" => (JustX -* (T<int> * T<int>).Parameter ^-> T<string>) ^-> T<string>
        ]

    let WIGtestGeneric =
        Generic - fun a b ->
            Class "WIGtestGeneric"
            |+> Instance [
                "NonGenericMethod" => a * b ^-> T<unit>
                Generic - fun c d -> "GenericMethod" => a * b * c * d ^-> T<unit>
                Constructor T<unit>
            ]

    let Assembly =
        Assembly [
            Namespace "IntelliFactory.WebSharper.InterfaceGenerator.Tests" [
                 JustX
                 WIGtest
                 WIGtestGeneric
                 Resource "WIGTestJs" "WIGtest.js" |> AssemblyWide
            ]
        ]

open IntelliFactory.WebSharper.InterfaceGenerator

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()