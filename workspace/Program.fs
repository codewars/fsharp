open System
open NUnit.Engine
open System.Reflection
open System.Xml
open System.Globalization

[<EntryPoint>]
let main argv =

    let GetAttribute (node: XmlNode) (name: string): string option =
        match node with
        | null -> None
        | elem -> match node.Attributes.[name] with
                  | null -> None
                  | a -> Some(a.Value)

    let GetDescription (node: XmlNode) =
        let description = node.SelectSingleNode("properties/property[@name = 'Description']");
        GetAttribute description "value"

    let WriteCompletedIn (node: XmlNode) =
        GetAttribute node "duration"
        |> Option.map (fun d -> Double.Parse(d, CultureInfo.InvariantCulture) * 1000.0)  // seconds to ms
        |> Option.map (fun d -> d.ToString "0.0000")
        |> Option.defaultValue ""
        |> printfn "\n<COMPLETEDIN::>%s"

    let escapeLF(s: string): string = s.Replace(Environment.NewLine, "<:LF:>")

    let OnTestCase(testCase: XmlNode) =

        GetDescription testCase
        |> Option.orElse  (GetAttribute testCase "name")
        |> Option.defaultValue ""
        |> printfn "\n<IT::>%s"

        testCase.SelectSingleNode "output"
        |> Option.ofObj
        |> Option.iter (fun node -> printfn "%s" node.InnerText)

        match GetAttribute testCase "result" with
        | Some("Passed") -> printfn "\n<PASSED::>Test Passed"
        | Some("Failed") ->
            let label = GetAttribute testCase "label"
            let message = Option.ofObj <| testCase.SelectSingleNode "failure/message"
            match label with
            | Some("Error") ->
                message
                |> Option.map (fun m -> escapeLF(m.InnerText))
                |> Option.defaultValue "Unknown Error"
                |> printfn "\n<ERROR::>%s"

                testCase.SelectSingleNode "failure/stack-trace"
                |> Option.ofObj
                |> Option.iter (fun node -> printfn "\n<LOG::-Stack Trace>%s" node.InnerText)
            | _ ->
                message
                |> Option.map (fun msg -> "<:LF:>" + escapeLF msg.InnerText)
                |> Option.defaultValue ""
                |> printfn "\n<FAILED::>Test Failed%s"
        | _ -> ()
        WriteCompletedIn testCase


    let rec OnTestSuiteTestFixture(testFixture: XmlNode) =

        GetDescription testFixture
        |> Option.orElse  (GetAttribute testFixture "name")
        |> Option.defaultValue ""
        |> printfn "\n<DESCRIBE::>%s"

        for child in testFixture.ChildNodes do
            match child.Name with
            | "test-suite" ->
                match GetAttribute child "type" with
                | Some("ParameterizedMethod" | "GenericMethod") -> OnTestSuiteTestFixture(child)
                | _ -> ()
            | "test-case" ->  OnTestCase(child)
            | _ -> ()
        WriteCompletedIn(testFixture);

    let rec OnTestSuiteTestSuite(testSuite: XmlNode) =

        GetDescription testSuite
        |> Option.orElse  (GetAttribute testSuite "name")
        |> Option.defaultValue ""
        |> printfn "\n<DESCRIBE::>%s"

        for child in testSuite.SelectNodes "test-suite" do
            match GetAttribute child "type" with
            | Some("TestFixture") -> OnTestSuiteTestFixture(child)
            | _ -> OnTestSuiteTestSuite(child)

        WriteCompletedIn(testSuite);


    let OnTestSuiteAssembly(testSuite: XmlNode) =
        for child in testSuite.SelectNodes "test-suite" do
            match GetAttribute child "type" with
            | Some("TestFixture") -> OnTestSuiteTestFixture(child)
            | _ -> OnTestSuiteTestSuite(child)

    let reportRun (reportNode: XmlNode) =
        reportNode.SelectSingleNode "test-suite[@type = 'Assembly']"
        |> Option.ofObj
        |> Option.iter OnTestSuiteAssembly

    let testpkg = new TestPackage (Assembly.GetExecutingAssembly().Location)
    let engine = new TestEngine()
    use runner = engine.GetRunner(testpkg)
    let reportNode = runner.Run(null, TestFilter.Empty)
    // TODO Exit code
    reportRun(reportNode)
    0
