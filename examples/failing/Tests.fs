module OddOrEvenTests

open OddOrEvenKata
open Preloaded

open NUnit.Framework

[<Test>]
let TestZero_TopLevelTest () =
    Assert.AreEqual(Answer.Even, oddOrEven 0)

[<TestFixture>]
type FixedTest() =
    [<Test>]
    member this.TestOne() =
        Assert.AreEqual(Answer.Odd, oddOrEven 1)

    [<Test>]
    member this.TestFail_NoCustomMessage() =
        Assert.AreEqual(Answer.Even, oddOrEven 42)

    [<Test>]
    member this.TestFail_CustomMessage() =
        Assert.AreEqual(Answer.Even, oddOrEven 42, "Incorrect answer for n={0}", 42)

    [<Test>]
    member this.TestCrash() =
        let s = null:string
        printf "Name: %s" (s.ToUpper())
        Assert.AreEqual(Answer.Even, oddOrEven 42, "Incorrect answer for n={0}", 42)

    [<Test>]
    member this.TestPrint() =
        printf "Value: %d" 42
        Assert.AreEqual(Answer.Even, oddOrEven 42, "Incorrect answer for n={0}", 42)

    [<Test>]
    member this.TestNegativeOdd ([<Values(-1, -3, -1001)>] n ) =
        Assert.AreEqual(Answer.Odd, oddOrEven n)

    [<TestCase(-2)>]
    [<TestCase(-42)>]
    [<TestCase(-1024)>]
    member this.TestNegativeEven n =
        Assert.AreEqual(Answer.Even, oddOrEven n)


[<TestFixture>]
type RandomTests() =

    static let rnd = System.Random( )

    [<Test>]
    [<TestCaseSource("GenerateEvenRandom")>]
    member this.EvenRandomTests n =
        Assert.AreEqual(Answer.Even, oddOrEven n)


    static member private GenerateEvenRandom() =
        List.init 10 (fun _ -> rnd.Next(1000) * 2)

    [<Test>]
    [<TestCaseSource("GenerateOddRandom")>]
    member this.OddRandomTests n =
        Assert.AreEqual(Answer.Odd, oddOrEven n)


    static member private GenerateOddRandom() =
        List.init 10 (fun _ -> rnd.Next(1000) * 2 + 1)

    [<Test>]
    [<TestCaseSource("GenerateRandomCases")>]
    member this.FullRandomTests n expected =
        Assert.AreEqual(expected, oddOrEven n, "Incorrect answer for n={0}", n)


    static member private GenerateRandomCases() =
        let oddOrEvenRef n = match n % 2 with
                                | 0 -> Answer.Even
                                | _ -> Answer.Odd

        let nums = List.init 100 (fun _ -> rnd.Next(-1000, 1000))

        let makeTestData n =
            let data = TestCaseData(n, oddOrEvenRef n)
            data.TestName <- sprintf "Test n=%d" n
            data

        List.map makeTestData nums
