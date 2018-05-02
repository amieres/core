module GenSingleFw

#r "System.Xml.Linq"
open System
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open System.Xml.XPath

let (+/) x y = Path.Combine(x, y)
let xn s = XName.Get s

type TargetFramework =
    | Net461
    | Net46
    | NetStandard20
    | NetCoreApp20

    override this.ToString() =
        match this with
        | Net461 -> "net461"
        | Net46 -> "net46"
        | NetStandard20 -> "netstandard2.0"
        | NetCoreApp20 -> "netcoreapp2.0"

let parseTargetFrameworks (attrValue: string) : TargetFramework[] =
    attrValue.Split(';')
    |> Array.map (function
        | "net461" -> Net461
        | "net46" -> Net46
        | "netstandard2.0" -> NetStandard20
        | "netcoreapp2.0" -> NetCoreApp20
        | s -> failwithf "Unknown target framework: %s" s
    )

let copyFileIfExistsAndIsDifferent (srcFile: string) (dstFile: string) =
    let dstFile =
        if Directory.Exists(dstFile) then
            dstFile +/ Path.GetFileName(srcFile)
        else dstFile
    if File.Exists(srcFile) then
        let content = File.ReadAllBytes(srcFile)
        let shouldWrite =
            not (File.Exists(dstFile)) ||
            let oldContent = File.ReadAllBytes(dstFile)
            content <> oldContent
        let fileName = Path.GetFileName(srcFile)
        let dstDir = Path.GetDirectoryName(dstFile)
        if shouldWrite then
            printfn "  Copying %s to %s" fileName dstDir
            File.WriteAllBytes(dstFile, content)
        else
            ()
            // printfn "  Not copying %s to %s -- existing file is identical" fileName dstDir

let copyAndTransformProjects (srcDir: string) (dstDir: string) (transformTargetFrameworks: string -> TargetFramework[] -> option<TargetFramework>) =
    for f in Directory.GetFiles(srcDir, "*.*proj", SearchOption.AllDirectories) do
        let origFullDir = Path.GetDirectoryName(f)
        let relDir = origFullDir.[srcDir.Length..].Trim([|Path.DirectorySeparatorChar|])
        let dstFullDir = dstDir +/ relDir
        let fn = Path.GetFileName(f)
        Directory.CreateDirectory(dstFullDir) |> ignore
        copyFileIfExistsAndIsDifferent (origFullDir +/ "paket.references") dstFullDir
        if not (f.Contains("SPA")) then
            copyFileIfExistsAndIsDifferent (origFullDir +/ "wsconfig.json") dstFullDir
        let doc = XDocument.Load(f)

        // Fix target framework
        match doc.XPathSelectElement("/Project/PropertyGroup/TargetFrameworks") with
        | null -> ()
        | tfe ->
            match tfe.Value |> parseTargetFrameworks |> transformTargetFrameworks fn with
            | None -> ()
            | Some fw ->
                tfe.Name <- xn"TargetFramework"
                tfe.Value <- fw |> string

        // Fix compile/embed/etc file names
        doc.XPathSelectElements("/Project/ItemGroup/*[self::Compile or self::Content or self::None or self::EmbeddedResource]")
        |> Seq.iter (fun e ->
            let name = e.Attribute(xn"Include").Value
            if name <> @"Scripts\*.*" then
                e.SetAttributeValue(xn"Include", origFullDir +/ name)
        )

        doc.XPathSelectElements("/Project/Import[contains(@Project, 'msbuild')]")
        |> Seq.iter (fun e ->
            let name = e.Attribute(xn"Project").Value
            e.SetAttributeValue(xn"Project", name.Replace("msbuild", @"..\msbuild"))
        )

        let dstFile = dstFullDir +/ fn
        let dstTmpFile = dstFile + ".tmp"
        doc.Save(dstTmpFile)
        copyFileIfExistsAndIsDifferent dstTmpFile dstFile
        File.Delete(dstTmpFile)

let copyShallowDir (srcDir: string) (dstDir: string) =
    Directory.CreateDirectory(dstDir) |> ignore
    for f in Directory.GetFiles(srcDir) do
        copyFileIfExistsAndIsDifferent f dstDir
        File.Copy(f, dstDir +/ Path.GetFileName(f), true)

let srcDir = Path.GetDirectoryName(__SOURCE_DIRECTORY__)

let doCopyAll dstDir transformTargetFrameworks =
    copyAndTransformProjects (srcDir +/ "src") (dstDir +/ "src") transformTargetFrameworks
    copyAndTransformProjects (srcDir +/ "tests") (dstDir +/ "tests") transformTargetFrameworks
    copyShallowDir (srcDir +/ ".paket") (dstDir +/ ".paket")
    copyFileIfExistsAndIsDifferent "global.json" dstDir
    for f in ["WebSharper.sln"; "WebSharper.Compiler.sln"; "paket.dependencies"; "paket.lock"] do
        copyFileIfExistsAndIsDifferent f dstDir
    printfn "Copied into %s." dstDir

let gen() =
    doCopyAll "netfx" <| fun fn fws ->
        if fws |> Array.contains Net461 then Some Net461
        elif fws |> Array.contains Net46 then Some Net46
        else Some Net461
    doCopyAll "netcore" <| fun fn fws ->
        if fws |> Array.contains NetCoreApp20 then Some NetCoreApp20
        elif fws |> Array.contains NetStandard20 then Some NetStandard20
        else Some NetStandard20

do gen()
