open System
open System.Reflection
open System.Xml
open System.Globalization

open NUnit.Engine

type TestResult = Unknown | Passed | Failed

[<EntryPoint>]
let main argv =
    let GetAttribute (node: XmlNode) (name: string): string option =
        match node with
        | null -> None
        | elem -> match elem.Attributes.[name] with
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

    let GetSuiteResult (testResults: TestResult seq) = 
        let GetResultScore (result: TestResult): int =
            match result with
                | Passed  -> 0
                | Unknown -> 1
                | Failed  -> 2
        if Seq.isEmpty testResults then Unknown else Seq.maxBy GetResultScore testResults

    let OnTestCase(testCase: XmlNode): TestResult =

        GetDescription testCase
        |> Option.orElse  (GetAttribute testCase "name")
        |> Option.defaultValue ""
        |> printfn "\n<IT::>%s"

        testCase.SelectSingleNode "output"
        |> Option.ofObj
        |> Option.iter (fun node -> printfn "%s" node.InnerText)

        let testCaseResult =
            match GetAttribute testCase "result" with
            | Some("Passed") ->
                printfn "\n<PASSED::>Test Passed"
                Passed
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
                Failed
            | _ -> Unknown
        WriteCompletedIn testCase
        testCaseResult

    let rec OnTestSuiteTestFixture(testFixture: XmlNode): TestResult =
        GetDescription testFixture
        |> Option.orElse  (GetAttribute testFixture "name")
        |> Option.defaultValue ""
        |> printfn "\n<DESCRIBE::>%s"

        let suiteResult =
            testFixture.ChildNodes
            |> Seq.cast<XmlNode> |> Seq.toList
            |> List.map (fun child ->
                match child.Name with
                | "test-suite" ->
                    match GetAttribute child "type" with
                    | Some("ParameterizedMethod" | "GenericMethod") -> OnTestSuiteTestFixture(child)
                    | _ -> Passed
                | "test-case" ->  OnTestCase(child)
                | _ -> Passed )
            |> GetSuiteResult
        WriteCompletedIn(testFixture)
        suiteResult

    let rec OnTestSuiteTestSuite(testSuite: XmlNode): TestResult =

        GetDescription testSuite
        |> Option.orElse  (GetAttribute testSuite "name")
        |> Option.defaultValue ""
        |> printfn "\n<DESCRIBE::>%s"

        let suiteResult = 
            testSuite.SelectNodes "test-suite"
            |> Seq.cast<XmlElement> |> Seq.toList
            |> List.map (fun child ->
                match GetAttribute child "type" with
                | Some("TestFixture") -> OnTestSuiteTestFixture(child)
                | _ -> OnTestSuiteTestSuite(child) )
            |> GetSuiteResult

        WriteCompletedIn(testSuite)

        suiteResult


    let OnTestSuiteAssembly(testSuite: XmlNode): TestResult =        
        testSuite.SelectNodes "test-suite"
        |> Seq.cast<XmlElement> |> Seq.toList
        |> List.map (fun child ->
            match GetAttribute child "type" with
            | Some("TestFixture") -> OnTestSuiteTestFixture(child)
            | _ -> OnTestSuiteTestSuite(child) )
        |> GetSuiteResult
        

    let reportRun (reportNode: XmlNode): TestResult =
        reportNode.SelectSingleNode "test-suite[@type = 'Assembly']"
        |> Option.ofObj
        |> Option.map OnTestSuiteAssembly
        |> Option.defaultValue Unknown

    let testpkg = new TestPackage (Assembly.GetExecutingAssembly().Location)
    let engine = new TestEngine()
    use runner = engine.GetRunner(testpkg)
    let reportNode = runner.Run(null, TestFilter.Empty)
    if reportRun(reportNode) = Passed then 0 else 1
